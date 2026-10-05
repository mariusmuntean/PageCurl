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
        window.Width = 1728;
        window.Height = 1000;
        window.Page = page;
        await Task.Delay(500);
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
        results.Add($"SPREAD_CAPTURE count={captureTimings.Count} total_ms={captureTimings.TotalMilliseconds:F1}");
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
        page.FindByName<Switch>("ReduceMotionSwitch").IsToggled = true;
        foreach (var scene in new[] { "Single", "Spread", "Content", "Backside" })
        {
            Invoke(page, "OnSceneClicked", new Button { CommandParameter = scene }, EventArgs.Empty);
            await Task.Delay(300);
            if (scene == "Content") await page.FindByName<PageCurlView>("ContentPageView").RefreshSnapshotAsync();
            if (scene == "Backside") await page.FindByName<PageCurlView>("BacksidePageView").RefreshSnapshotAsync();
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
    }

    private static async Task Explicit_refresh_preserves_failure_and_scene_status(MainPage page, List<string> results)
    {
        ((IButtonController)page.FindByName<Button>("ContentSceneButton")).SendClicked();
        var content = page.FindByName<PageCurlView>("ContentPageView");
        await content.RefreshSnapshotAsync();
        var field = typeof(PageCurlView).GetField("_snapshotter", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var original = field.GetValue(content);
        var refresh = page.FindByName<Button>("RefreshSnapshotButton");
        var status = page.FindByName<Label>("StatusLabel");
        var previous = content.Renderer.FrontImage;
        try
        {
            field.SetValue(content, new FailingSnapshotter());
            ((IButtonController)refresh).SendClicked();
            await WaitUntilAsync(() => refresh.IsEnabled);
            results.Add(status.Text == "Using last snapshot" && ReferenceEquals(previous, content.Renderer.FrontImage)
                ? "PASS explicit refresh reports failure and retains last frame"
                : $"FAIL explicit refresh hid capture failure: {status.Text}");

            await page.RefreshContentAsync("Live content updated", deferUntilLayout: true);
            results.Add(status.Text == "Using last snapshot"
                ? "PASS live refresh reports capture failure honestly"
                : $"FAIL live refresh hid capture failure: {status.Text}");

            var queuedLiveRefresh = page.RefreshContentAsync("Live content updated", deferUntilLayout: true);
            ((IButtonController)page.FindByName<Button>("SingleSceneButton")).SendClicked();
            ((IButtonController)page.FindByName<Button>("ContentSceneButton")).SendClicked();
            await queuedLiveRefresh;
            results.Add(status.Text == "Ready"
                ? "PASS queued live refresh ignores scene re-entry"
                : $"FAIL queued live refresh changed new scene status: {status.Text}");

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
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var timeout = Stopwatch.StartNew();
        while (!condition() && timeout.Elapsed < TimeSpan.FromSeconds(3)) await Task.Delay(10);
        Require(condition(), "Timed out waiting for the UI operation.");
    }

    private sealed class FailingSnapshotter : IPageContentSnapshotter
    {
        public Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken) =>
            Task.FromException<SKImage>(new InvalidOperationException("Capture unavailable in regression fixture."));
    }

    private sealed class SuspendedSnapshotter : IPageContentSnapshotter
    {
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<SKImage> _capture = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task Started => _started.Task;
        public Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken)
        {
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
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class TimedSnapshotter : IPageContentSnapshotter
    {
        private readonly PageContentSnapshotter _inner = new();
        public int Count { get; private set; }
        public double TotalMilliseconds { get; private set; }
        public async Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken)
        {
            var watch = Stopwatch.StartNew();
            try { return await _inner.CaptureAsync(content, cancellationToken); }
            finally { Count++; TotalMilliseconds += watch.Elapsed.TotalMilliseconds; }
        }
    }
}
