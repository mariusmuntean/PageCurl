using Stopwatch = System.Diagnostics.Stopwatch;
using System.Reflection;
using Microsoft.Maui.Controls;
using PageCurl.Core;
using PageCurl.Demo;
using PageCurl.Maui;
using SkiaSharp.Views.Maui.Controls;
using UIKit;
using CoreGraphics;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using static PageCurl.Apple.Tests.NativeTestSupport;

namespace PageCurl.Apple.Tests;

internal static class DemoRegressionTests
{
    public static async Task RunAsync(Window window, List<string> results)
    {
        var watch = Stopwatch.StartNew();
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        using (var pages = DemoPageSet.Create())
        {
            results.Add($"RESOURCE_CREATE elapsed_ms={watch.Elapsed.TotalMilliseconds:F1} managed_bytes={GC.GetAllocatedBytesForCurrentThread() - allocated}");
            Require(pages.Pages.Count == 8 && pages.Pages[6].Width == 1800, "Generated pages changed.");
        }
        var page = new MainPage();
        var captureTimings = new TimedSnapshotter();
        typeof(PageSpreadView).GetField("_snapshotter", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(page.FindByName<PageSpreadView>("SpreadPageView"), captureTimings);
        foreach (var name in new[] { "ContentPageView", "BacksidePageView" })
            typeof(PageCurlView).GetField("_snapshotter", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(page.FindByName<PageCurlView>(name), captureTimings);
        window.Width = 1728;
        window.Height = 1000;
        window.Page = page;
        await Task.Delay(500);
        await Check_theme_contrast(page, results);
        Invoke(page, "OnSceneClicked", new Button { CommandParameter = "Backside" }, EventArgs.Empty);
        await Task.Delay(250);
        var backside = page.FindByName<PageCurlView>("BacksidePageView");
        results.Add(SemanticProperties.GetDescription(backside.BackContent)?.StartsWith("Page 2.") == true
            ? "PASS page 01 back is page 02" : "FAIL page 01 back is unrelated content");
        var pageFrame = (Grid)backside.Children[0];
        // Hidden renderers are not arranged; inspect the active curl surface.
        backside.Renderer.IsVisible = true;
        await Task.Delay(100);
        results.Add(ReferenceEquals(backside.Renderer.Parent, backside)
            && Math.Abs(backside.Renderer.Width - backside.Width) < 1
            && backside.Renderer.Width > pageFrame.Width
            ? "PASS curl surface spans stage" : "FAIL curl surface confined to page");
        backside.CancelInteraction();
        Invoke(page, "OnSceneClicked", new Button { CommandParameter = "Single" }, EventArgs.Empty);
        var single = page.FindByName<PageTurnView>("SinglePageView");
        await Task.Delay(100);
        single.PerformanceDiagnosticsEnabled = true;
        single.ResetPerformanceSnapshot();
        var viewport = single.LastContentViewport;
        var grab = new SKPoint(viewport.Origin.X + viewport.Size.X - 1, viewport.Origin.Y + viewport.Size.Y * .5f);
        SendTouch(single, SKTouchAction.Pressed, grab);
        for (var frame = 0; frame < 5; frame++)
        {
            var painted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            EventHandler<SKPaintGLSurfaceEventArgs> callback = (_, _) => painted.TrySetResult();
            single.PaintSurface += callback;
            try
            {
                SendTouch(single, SKTouchAction.Moved,
                    new SKPoint(grab.X - viewport.Size.X * (.2f + frame * .1f), grab.Y + viewport.Size.Y * .1f));
                await painted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            }
            finally { single.PaintSurface -= callback; }
        }
        var frameMetrics = single.PerformanceSnapshot;
        results.Add($"SINGLE_PAINT frames={frameMetrics.FrameCount} mean_ms={frameMetrics.AverageFrameMilliseconds:F1} last_ms={frameMetrics.LastFrameMilliseconds:F1}");
        // Release and switch in the same event-loop turn. Waiting for the
        // settle animation would hide the reported regression.
        SendTouch(single, SKTouchAction.Released, grab);
        watch.Restart();
        ((IButtonController)page.FindByName<Button>("SpreadSceneButton")).SendClicked();
        var clickMilliseconds = watch.Elapsed.TotalMilliseconds;
        await Task.Delay(1);
        results.Add($"IMMEDIATE_SWITCH handler_ms={clickMilliseconds:F1} dispatcher_ms={watch.Elapsed.TotalMilliseconds:F1}");
        results.Add($"SNAPSHOT_CAPTURE count={captureTimings.Count} total_ms={captureTimings.TotalMilliseconds:F1}");
        results.Add(clickMilliseconds < 150 && watch.Elapsed.TotalMilliseconds < 250
            ? "PASS immediate switch stays responsive" : "FAIL immediate switch blocks UI thread");
        results.Add(single.Controller.Snapshot.State == PageTurnState.Idle
            ? "PASS scene switch cancels immediately" : "FAIL hidden scene keeps settling");
        var before = page.FindByName<Label>("PageCounterLabel").Text;
        Invoke(page, "OnPageTurnCompleted", single, new PageTurnCompletedEventArgs(PageTurnDirection.Forward, true));
        results.Add(before == page.FindByName<Label>("PageCounterLabel").Text
            ? "PASS inactive completion ignored" : "FAIL inactive completion advances spread");
        await Task.Delay(300);
        var spreadReady = page.FindByName<PageSpreadView>("SpreadPageView");
        results.Add(spreadReady.CanTurnForward ? "PASS spread ready after Single drag" : "FAIL spread blocked after Single drag");
        var spreadFrame = (Grid)spreadReady.Children[0];
        var zone = spreadFrame.Children.OfType<SKCanvasView>().Single(v => Equals(v.BindingContext, PageTurnDirection.Forward));
        var nativeZone = (UIView)zone.Handler!.PlatformView!;
        var nativeWindow = nativeZone.Window!;
        var point = nativeZone.ConvertPointToView(new CGPoint(nativeZone.Bounds.Width / 2, nativeZone.Bounds.Height / 2), nativeWindow);
        var hit = nativeWindow.HitTest(point, null);
        results.Add($"ZONE logical={zone.Bounds} native={nativeZone.Frame} center={point} hit={hit?.GetType().Name} enabled={nativeZone.UserInteractionEnabled}");
        results.Add(hit?.Handle == nativeZone.Handle ? "PASS spread outer edge receives input" : "FAIL spread edge input intercepted");
        ((IButtonController)page.FindByName<Button>("SingleSceneButton")).SendClicked();
        await Idle_cancellation_does_not_render(single, results);
        Backside_scene_reuses_unchanged_content(page, results);
        await Rapid_scene_switches(page, results, captureTimings);
        page.FindByName<Switch>("ReduceMotionSwitch").IsToggled = true;
        foreach (var scene in new[] { "Single", "Spread", "Content", "Backside" })
        {
            Invoke(page, "OnSceneClicked", new Button { CommandParameter = scene }, EventArgs.Empty);
            await Task.Delay(300);
            if (scene == "Content") await page.FindByName<PageCurlView>("ContentPageView").RefreshSnapshotAsync();
            if (scene == "Backside") await page.FindByName<PageCurlView>("BacksidePageView").RefreshSnapshotAsync();
            await WaitUntilAsync(() => page.FindByName<Button>("NextButton").IsEnabled);
            Invoke(page, "RequestTurn", PageTurnDirection.Forward);
            var status = page.FindByName<Label>("StatusLabel").Text;
            results.Add(status == "Turn complete" ? $"PASS {scene} immediate status" : $"FAIL {scene} immediate status: {status}");
            var readyWatch = Stopwatch.StartNew();
            while (!page.FindByName<Button>("NextButton").IsEnabled && readyWatch.Elapsed < TimeSpan.FromSeconds(2))
                await Task.Delay(10);
            results.Add(page.FindByName<Button>("NextButton").IsEnabled
                ? $"PASS {scene} next available" : $"FAIL {scene} next disabled");
        }
        await Explicit_refresh_preserves_failure_and_scene_status(page, results);
        Invoke(page, "OnContentKindClicked", new Button { CommandParameter = "Interactive" }, EventArgs.Empty);
        var live = (ContentView)typeof(MainPage).GetField("_interactivePage", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(page)!;
        await WaitUntilAsync(() => live.Handler is not null && live.Width > 0 && live.Height > 0);
        await page.FindByName<PageCurlView>("ContentPageView").RefreshSnapshotAsync();
        ((IView)live).Measure(250, 333);
        ((IView)live).Arrange(new Rect(0, 0, 250, 333));
        var liveGrid = (Grid)live.Content;
        liveGrid.CrossPlatformMeasure(250 - live.Padding.HorizontalThickness, 333 - live.Padding.VerticalThickness);
        liveGrid.CrossPlatformArrange(new Rect(live.Padding.Left, live.Padding.Top,
            250 - live.Padding.HorizontalThickness, 333 - live.Padding.VerticalThickness));
        var counter = (Label)liveGrid.Children[2];
        var liveButton = (Button)liveGrid.Children[3];
        results.Add(counter.Height >= counter.FontSize && liveButton.Bounds.Bottom <= live.Height
            ? "PASS compact live content keeps counter and button visible"
            : $"FAIL compact live content clipped: counter={counter.Height:F1} font={counter.FontSize:F1} button={liveButton.Bounds.Bottom:F1} page={live.Height:F1}");
        // Queue a refresh, disappear before its yield resumes, then verify
        // it cannot update the old page or recapture detached content.
        Invoke(page, "OnInteractiveContentChanged", null, EventArgs.Empty);
        Invoke(page, "OnDisappearing");
        var spread = page.FindByName<PageSpreadView>("SpreadPageView");
        var cleared = spread.LeftContent is null && spread.RightContent is null
            && spread.PreviousLeftContent is null && spread.NextRightContent is null
            && spread.PreviousRightContent is null && spread.NextLeftContent is null;
        results.Add(cleared ? "PASS all six spread slots detached" : "FAIL spread sheet backs retained");
        var statusBefore = page.FindByName<Label>("StatusLabel").Text;
        await Task.Delay(250);
        results.Add(statusBefore == page.FindByName<Label>("StatusLabel").Text
            ? "PASS queued callback ignores disappearance" : "FAIL callback updated disappeared page");
        Invoke(page, "OnAppearing");
        await Task.Delay(300);
        Invoke(page, "OnSceneClicked", new Button { CommandParameter = "Single" }, EventArgs.Empty);
        var recreated = page.FindByName<PageTurnView>("SinglePageView").FrontImage;
        results.Add(recreated is not null && recreated.Handle != IntPtr.Zero
            ? "PASS resources recreated after return" : "FAIL resources missing after return");
        page.FindByName<Switch>("ReduceMotionSwitch").IsToggled = false;
        Invoke(page, "RequestTurn", PageTurnDirection.Forward);
        results.Add(page.FindByName<Label>("StatusLabel").Text == "Turning"
            ? "PASS animated command starts" : "FAIL animated status");
        await Task.Delay(1500);
        results.Add(page.FindByName<Label>("StatusLabel").Text == "Turn complete"
            ? "PASS animated completion" : "FAIL animated completion");
        Check_compact_options(page, results);
        await Unarranged_spread_refresh_can_be_cancelled_or_disconnected(page, results);
        await Navigation_journeys_cover_every_scene_in_both_layouts(page, results);
    }

    private static async Task Unarranged_spread_refresh_can_be_cancelled_or_disconnected(MainPage page, List<string> results)
    {
        var stage = page.FindByName<Grid>("StageHost");
        foreach (var cancel in new[] { true, false })
        {
            var probe = new PageSpreadView { IsVisible = false };
            using var cancellation = new CancellationTokenSource();
            var failures = 0;
            probe.SnapshotFailed += (_, _) => failures++;
            stage.Children.Add(probe);
            try
            {
                await WaitUntilAsync(() => probe.Handler is not null);
                // Hidden, freshly assigned content has no arranged native bounds.
                probe.LeftContent = new Grid();
                var refresh = probe.RefreshSnapshotAsync(cancellation.Token);
                Require(!refresh.IsCompleted && failures == 0, "Unarranged content was captured instead of waiting for layout.");
                if (cancel)
                {
                    cancellation.Cancel();
                    try
                    {
                        await refresh.WaitAsync(TimeSpan.FromSeconds(3));
                        throw new InvalidOperationException("Layout wait ignored caller cancellation.");
                    }
                    catch (OperationCanceledException) { }
                }
                else
                {
                    stage.Children.Remove(probe);
                    probe.Handler = null;
                    await refresh.WaitAsync(TimeSpan.FromSeconds(3));
                }
                Require(failures == 0 && !probe.CanTurnForward && !probe.CanTurnBackward,
                    "Aborted layout wait published failure or navigation readiness.");
                results.Add($"PASS unarranged spread refresh {(cancel ? "cancellation" : "disconnection")} releases layout wait");
            }
            finally
            {
                if (ReferenceEquals(probe.Parent, stage)) stage.Children.Remove(probe);
                probe.Handler = null;
            }
        }
    }

    private static async Task Navigation_journeys_cover_every_scene_in_both_layouts(MainPage page, List<string> results)
    {
        var originalBounds = page.Bounds;
        var motion = page.FindByName<Switch>("ReduceMotionSwitch");
        var wasReduced = motion.IsToggled;
        motion.IsToggled = true;
        try
        {
            foreach (var compact in new[] { false, true })
            {
                if (compact)
                {
                    ((IView)page).Measure(600, 750);
                    ((IView)page).Arrange(new Rect(0, 0, 600, 750));
                }
                foreach (var scene in new[] { "Single", "Spread", "Content", "Backside" })
                {
                    ((IButtonController)page.FindByName<Button>((compact ? "Mobile" : "") + scene + "SceneButton")).SendClicked();
                    var previous = page.FindByName<Button>(compact ? "MobilePreviousButton" : "PreviousButton");
                    var next = page.FindByName<Button>(compact ? "MobileNextButton" : "NextButton");
                    var counter = page.FindByName<Label>("PageCounterLabel");
                    var spread = page.FindByName<PageSpreadView>("SpreadPageView");
                    var counters = scene == "Spread"
                        ? new[] { "01–02 / 08", "03–04 / 08", "05–06 / 08", "07–08 / 08" }
                        : new[] { "01 / 08", "02 / 08", "03 / 08", "04 / 08", "05 / 08", "06 / 08", "07 / 08", "08 / 08" };
                    async Task Ready()
                    {
                        if (scene == "Spread")
                            await WaitUntilAsync(() => (counter.Text == counters[0] || spread.CanTurnBackward)
                                && (counter.Text == counters[^1] || spread.CanTurnForward));
                        else if (scene is "Content" or "Backside")
                            await WaitUntilAsync(() => SnapshotReady(page.FindByName<PageCurlView>($"{scene}PageView")));
                    }
                    await Ready();
                    // Earlier suites leave different page indices. Normalize through real commands.
                    for (var step = 0; previous.IsEnabled && step < 8; step++)
                    {
                        ((IButtonController)previous).SendClicked();
                        await Ready();
                    }
                    Require(!previous.IsEnabled && next.IsEnabled, $"{scene}: first boundary buttons");
                    Require(counter.Text == counters[0], $"{scene}: first counter was {counter.Text}");
                    for (var step = 1; step < counters.Length; step++)
                    {
                        Require(next.IsEnabled, $"{scene}: next unavailable at step {step}");
                        ((IButtonController)next).SendClicked();
                        await Ready();
                        Require(counter.Text == counters[step], $"{scene}: forward step {step} was {counter.Text}");
                        Require(page.FindByName<Label>("StatusLabel").Text == "Turn complete", $"{scene}: forward completion status {page.FindByName<Label>("StatusLabel").Text}; {page.FindByName<Label>("DetailLabel").Text}");
                    }
                    Require(!next.IsEnabled && previous.IsEnabled, $"{scene}: last boundary buttons");
                    for (var step = counters.Length - 2; step >= 0; step--)
                    {
                        Require(previous.IsEnabled, $"{scene}: previous unavailable at step {step}");
                        ((IButtonController)previous).SendClicked();
                        await Ready();
                        Require(counter.Text == counters[step], $"{scene}: backward step {step} was {counter.Text}");
                        Require(page.FindByName<Label>("StatusLabel").Text == "Turn complete", $"{scene}: backward completion status");
                    }
                    Require(!previous.IsEnabled && next.IsEnabled, $"{scene}: restored boundary buttons");
                    Require(page.FindByName<Button>("PreviousButton").IsEnabled == page.FindByName<Button>("MobilePreviousButton").IsEnabled
                        && page.FindByName<Button>("NextButton").IsEnabled == page.FindByName<Button>("MobileNextButton").IsEnabled,
                        $"{scene}: compact and wide navigation disagree");
                    results.Add($"PASS {scene} {(compact ? "compact" : "wide")} full forward/backward journey and boundaries");
                    motion.IsToggled = false;
                    foreach (var forward in new[] { true, false })
                    {
                        await WaitUntilAsync(() => (forward ? next : previous).IsEnabled);
                        ((IButtonController)(forward ? next : previous)).SendClicked();
                        Require(page.FindByName<Label>("StatusLabel").Text == "Turning"
                            && !previous.IsEnabled && !next.IsEnabled,
                            $"{scene}: animated turn did not lock commands: forward={forward}, status={page.FindByName<Label>("StatusLabel").Text}, previous={previous.IsEnabled}, next={next.IsEnabled}");
                        var expected = forward ? counters[1] : counters[0];
                        await WaitUntilAsync(() => counter.Text == expected
                            && page.FindByName<Label>("StatusLabel").Text == "Turn complete");
                        await Ready();
                    }
                    Require(!previous.IsEnabled && next.IsEnabled, $"{scene}: animated return boundary");
                    results.Add($"PASS {scene} {(compact ? "compact" : "wide")} animated forward/backward completion");
                    motion.IsToggled = true;
                }
            }
        }
        finally
        {
            motion.IsToggled = wasReduced;
            ((IView)page).Arrange(originalBounds);
        }
    }

    private static void Check_compact_options(MainPage page, List<string> results)
    {
        var originalBounds = page.Bounds;
        ((IView)page).Measure(600, 750);
        ((IView)page).Arrange(new Rect(0, 0, 600, 750));
        Require(page.Width == 600 && page.FindByName<Grid>("MobileFooter").IsVisible,
            "Compact layout must expose mobile controls.");
        var overlay = page.FindByName<Grid>("MobileOptionsOverlay");
        var stage = page.FindByName<Grid>("StageHost");
        var options = page.FindByName<ScrollView>("OptionsScroll");
        Require(!page.FindByName<Border>("InspectorPanel").IsVisible && !overlay.IsVisible,
            "Compact layout must start with options closed.");
        var footer = page.FindByName<Grid>("MobileFooter");
        var optionsButton = ((Grid)footer.Children[1]).Children.OfType<Button>().Last();
        ((IButtonController)optionsButton).SendClicked();
        Require(overlay.IsVisible && stage.InputTransparent
            && ReferenceEquals(options.Parent, page.FindByName<Grid>("MobileOptionsContent")),
            "Compact options must open and prevent page interaction.");
        ((IButtonController)page.FindByName<Button>("CloseOptionsButton")).SendClicked();
        Require(!overlay.IsVisible && !stage.InputTransparent, "Closing options must restore page interaction.");
        results.Add("PASS compact options open and close at a 600x750 page layout");
        ((IView)page).Arrange(originalBounds);
        Require(ReferenceEquals(options.Parent, page.FindByName<Grid>("InspectorContent")) && !overlay.IsVisible,
            "Wide layout must restore the inspector and close the compact overlay.");
        results.Add("PASS returning to wide layout restores option controls");
    }

    private static async Task Check_theme_contrast(MainPage page, List<string> results)
    {
        var app = Application.Current!;
        var original = app.UserAppTheme;
        static double Luminance(Color color)
        {
            static double Linear(double value) => value <= .04045 ? value / 12.92 : Math.Pow((value + .055) / 1.055, 2.4);
            return .2126 * Linear(color.Red) + .7152 * Linear(color.Green) + .0722 * Linear(color.Blue);
        }
        try
        {
            foreach (var theme in new[] { AppTheme.Light, AppTheme.Dark })
            {
                app.UserAppTheme = theme;
                await Task.Delay(100);
                var label = page.FindByName<Label>("OptionsTitleLabel");
                var eyebrow = (Label)((VerticalStackLayout)label.Parent).Children[0];
                var panel = page.FindByName<Border>("InspectorPanel");
                var foreground = Luminance(eyebrow.TextColor);
                var background = Luminance(panel.BackgroundColor);
                var ratio = (Math.Max(foreground, background) + .05) / (Math.Min(foreground, background) + .05);
                results.Add(ratio >= 4.5 ? $"PASS {theme} option text contrast {ratio:F2}:1"
                    : $"FAIL {theme} option text contrast {ratio:F2}:1");
            }
        }
        finally { app.UserAppTheme = original; }
    }

    private static async Task Explicit_refresh_preserves_failure_and_scene_status(MainPage page, List<string> results)
    {
        Click(page, "ContentSceneButton");
        Click(page, "InteractiveContentButton");
        var content = page.FindByName<PageCurlView>("ContentPageView");
        await WaitUntilAsync(() => content.FrontContent?.Handler is not null
            && content.FrontContent.Width > 0 && content.FrontContent.Height > 0
            && !AutomaticRefreshQueued(content));
        var live = (ContentView)content.FrontContent!;
        await content.RefreshSnapshotAsync();
        var field = typeof(PageCurlView).GetField("_snapshotter", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var original = field.GetValue(content);
        var refresh = page.FindByName<Button>("RefreshSnapshotButton");
        var status = page.FindByName<Label>("StatusLabel");
        var previous = content.Renderer.FrontImage;
        try
        {
            HoldAutomaticQueue(content);
            field.SetValue(content, new FailingSnapshotter());
            ((IButtonController)refresh).SendClicked();
            await WaitUntilAsync(() => refresh.IsEnabled);
            results.Add(status.Text == "Using last snapshot" && ReferenceEquals(previous, content.Renderer.FrontImage)
                ? "PASS explicit refresh reports failure and retains last frame"
                : $"FAIL explicit refresh hid capture failure: {status.Text}");

            AddOne(live);
            await InteractiveTask(page).WaitAsync(UiTimeout);
            results.Add(status.Text == "Using last snapshot" && ReferenceEquals(previous, content.Renderer.FrontImage)
                ? "PASS live refresh reports capture failure honestly"
                : $"FAIL live refresh hid capture failure: {status.Text}");

            AddOne(live);
            var queuedLiveRefresh = InteractiveTask(page);
            ((IButtonController)page.FindByName<Button>("SingleSceneButton")).SendClicked();
            ((IButtonController)page.FindByName<Button>("ContentSceneButton")).SendClicked();
            await queuedLiveRefresh.WaitAsync(UiTimeout);
            results.Add(status.Text == "Ready"
                ? "PASS queued live refresh ignores scene re-entry"
                : $"FAIL queued live refresh changed new scene status: {status.Text}");

            foreach (var scene in new[] { "Content", "Spread" })
            {
                ((IButtonController)page.FindByName<Button>($"{scene}SceneButton")).SendClicked();
                var control = scene == "Content" ? (View)content : page.FindByName<PageSpreadView>("SpreadPageView");
                var snapshotterField = control.GetType().GetField("_snapshotter", BindingFlags.Instance | BindingFlags.NonPublic)!;
                var savedSnapshotter = snapshotterField.GetValue(control);
                var failure = new DelayedFailureSnapshotter();
                snapshotterField.SetValue(control, failure);
                try
                {
                    var capture = scene == "Content" ? content.RefreshSnapshotAsync()
                        : ((PageSpreadView)control).RefreshSnapshotAsync();
                    await failure.Started;
                    ((IButtonController)page.FindByName<Button>("SingleSceneButton")).SendClicked();
                    ((IButtonController)page.FindByName<Button>($"{scene}SceneButton")).SendClicked();
                    failure.Fail();
                    try { await capture; } catch (OperationCanceledException) { }
                    results.Add(status.Text == "Ready"
                        ? $"PASS {scene} delayed failure ignores scene re-entry"
                        : $"FAIL {scene} delayed failure changed new scene status: {status.Text}");
                }
                finally { snapshotterField.SetValue(control, savedSnapshotter); }
            }
            ((IButtonController)page.FindByName<Button>("ContentSceneButton")).SendClicked();

            var delayed = new SuspendedSnapshotter();
            field.SetValue(content, delayed);
            ((IButtonController)refresh).SendClicked();
            await delayed.Started;
            ((IButtonController)page.FindByName<Button>("SingleSceneButton")).SendClicked();
            ((IButtonController)page.FindByName<Button>("NextButton")).SendClicked();
            var activeStatus = status.Text;
            using var surface = SKSurface.Create(new SKImageInfo(4, 4));
            delayed.Finish(surface.Snapshot());
            await WaitUntilAsync(() => refresh.IsEnabled);
            results.Add(status.Text == activeStatus
                ? "PASS suspended refresh cannot overwrite another scene"
                : $"FAIL suspended refresh changed scene status: {activeStatus} -> {status.Text}");

            delayed = new SuspendedSnapshotter();
            field.SetValue(content, delayed);
            ((IButtonController)page.FindByName<Button>("ContentSceneButton")).SendClicked();
            ((IButtonController)refresh).SendClicked();
            await delayed.Started;
            ((IButtonController)page.FindByName<Button>("SingleSceneButton")).SendClicked();
            ((IButtonController)page.FindByName<Button>("ContentSceneButton")).SendClicked();
            activeStatus = status.Text;
            delayed.Finish(surface.Snapshot());
            await WaitUntilAsync(() => refresh.IsEnabled);
            results.Add(status.Text == activeStatus
                ? "PASS suspended refresh cannot overwrite a re-entered scene"
                : $"FAIL suspended refresh changed re-entered scene status: {activeStatus} -> {status.Text}");
        }
        finally
        {
            field.SetValue(content, original);
            HoldAutomaticQueue(content, false);
        }
    }

    private static async Task Idle_cancellation_does_not_render(PageTurnView renderer, List<string> results)
    {
        await Task.Delay(100);
        renderer.PerformanceDiagnosticsEnabled = true;
        renderer.ResetPerformanceSnapshot();
        for (var i = 0; i < 10; i++) renderer.CancelInteraction();
        await Task.Delay(100);
        results.Add(renderer.PerformanceSnapshot.FrameCount == 0
            ? "PASS idle cancellation does not request redundant paints"
            : $"FAIL idle cancellation requested {renderer.PerformanceSnapshot.FrameCount} redundant paints");
    }

    private static void Backside_scene_reuses_unchanged_content(MainPage page, List<string> results)
    {
        ((IButtonController)page.FindByName<Button>("BacksideSceneButton")).SendClicked();
        var backside = page.FindByName<PageCurlView>("BacksidePageView");
        var front = backside.FrontContent;
        var back = backside.BackContent;
        ((IButtonController)page.FindByName<Button>("SingleSceneButton")).SendClicked();
        ((IButtonController)page.FindByName<Button>("BacksideSceneButton")).SendClicked();
        results.Add(ReferenceEquals(front, backside.FrontContent) && ReferenceEquals(back, backside.BackContent)
            ? "PASS scene re-entry retains unchanged Backside content"
            : "FAIL scene re-entry rebuilds unchanged Backside content");
    }

    private static async Task Rapid_scene_switches(MainPage page, List<string> results, TimedSnapshotter captures)
    {
        string[] scenes = ["Single", "Spread", "Content", "Backside"];
        page.FindByName<Switch>("ReduceMotionSwitch").IsToggled = false;
        foreach (var delay in new[] { 0, 16, 80, -1 })
        {
            for (var index = 0; index < scenes.Length; index++)
            {
                var source = scenes[index];
                var destination = scenes[(index + 1) % scenes.Length];
                ((IButtonController)page.FindByName<Button>($"{source}SceneButton")).SendClicked();
                if (source is "Content" or "Backside")
                    await WaitUntilAsync(() => SnapshotReady(page.FindByName<PageCurlView>($"{source}PageView")));
                var next = page.FindByName<Button>("NextButton");
                var previous = page.FindByName<Button>("PreviousButton");
                await WaitUntilAsync(() => next.IsEnabled || previous.IsEnabled);
                var renderer = source switch
                {
                    "Single" => page.FindByName<PageTurnView>("SinglePageView"),
                    "Spread" => page.FindByName<PageSpreadView>("SpreadPageView").Overlay,
                    _ => page.FindByName<PageCurlView>($"{source}PageView").Renderer,
                };
                ((IButtonController)(next.IsEnabled ? next : previous)).SendClicked();
                Require(renderer.Controller.Snapshot.State == PageTurnState.Settling,
                    $"{source} rapid-switch probe did not start a turn.");
                if (delay < 0) await WaitUntilAsync(() => renderer.Controller.Snapshot.State == PageTurnState.Idle);
                else if (delay > 0) await Task.Delay(delay);
                var count = captures.Count;
                var captureTime = captures.TotalMilliseconds;
                var watch = Stopwatch.StartNew();
                ((IButtonController)page.FindByName<Button>($"{destination}SceneButton")).SendClicked();
                var handler = watch.Elapsed.TotalMilliseconds;
                await Task.Delay(1);
                var dispatch = watch.Elapsed.TotalMilliseconds;
                results.Add($"RAPID_SWITCH {source}->{destination} delay_ms={delay} handler_ms={handler:F1} dispatcher_ms={dispatch:F1} captures={captures.Count - count} capture_ms={captures.TotalMilliseconds - captureTime:F1}");
                results.Add(captures.Count == count
                    ? $"PASS {source}->{destination} scene dispatch precedes automatic capture ({delay})"
                    : $"FAIL {source}->{destination} automatic capture blocked scene dispatch ({delay})");
                Require(renderer.Controller.Snapshot.State == PageTurnState.Idle,
                    $"{source} kept animating after a rapid switch.");
            }
        }
        results.Add("PASS rapid scene switches cancel every outgoing turn");
    }

    private sealed class FailingSnapshotter : IPageContentSnapshotter
    {
        public Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken) =>
            Task.FromException<SKImage>(new InvalidOperationException("Capture unavailable in regression fixture."));
    }

    private sealed class DelayedFailureSnapshotter : IPageContentSnapshotter
    {
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<SKImage> _failure = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly PageContentSnapshotter _inner = new();
        private bool _first = true;
        public Task Started => _started.Task;
        public Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken)
        {
            if (!_first) return _inner.CaptureAsync(content, cancellationToken);
            _first = false;
            _started.SetResult();
            return _failure.Task;
        }
        public void Fail() => _failure.SetException(new InvalidOperationException("Delayed stale capture failure."));
    }

    private sealed class SuspendedSnapshotter : IPageContentSnapshotter
    {
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<SKImage> _capture = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly PageContentSnapshotter _inner = new();
        private bool _first = true;
        public Task Started => _started.Task;
        public Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken)
        {
            if (!_first) return _inner.CaptureAsync(content, cancellationToken);
            _first = false;
            _started.TrySetResult();
            return _capture.Task;
        }
        public void Finish(SKImage image) => _capture.SetResult(image);
    }

    private static void Invoke(MainPage page, string name, params object?[] args) =>
        typeof(MainPage).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)!.Invoke(page, args);
    private static void SendTouch(PageTurnView view, SKTouchAction action, SKPoint point) =>
        typeof(PageTurnView).GetMethod("OnTouch", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)!
            .Invoke(view, [view, new SKTouchEventArgs(1, action, point, action != SKTouchAction.Released)]);
    private sealed class TimedSnapshotter : IPageContentSnapshotter
    {
        private readonly PageContentSnapshotter _inner = new();
        public int Count { get; private set; }
        public double TotalMilliseconds { get; private set; }
        public async Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken)
        {
            var watch = Stopwatch.StartNew();
            try { return await _inner.CaptureAsync(content, cancellationToken); }
            catch (Exception exception)
            {
                Console.WriteLine($"CAPTURE_FAILURE logical={content.Bounds} native={(content.Handler?.PlatformView as UIView)?.Bounds}: {exception.Message}");
                throw;
            }
            finally { Count++; TotalMilliseconds += watch.Elapsed.TotalMilliseconds; }
        }
    }
}
