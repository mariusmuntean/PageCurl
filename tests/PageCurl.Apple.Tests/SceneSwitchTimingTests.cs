using System.Diagnostics;
using System.Reflection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using PageCurl.Core;
using PageCurl.Demo;
using PageCurl.Maui;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using static PageCurl.Apple.Tests.NativeTestSupport;

namespace PageCurl.Apple.Tests;

internal static class SceneSwitchTimingTests
{
    public static async Task RunAsync(Window window, List<string> results)
    {
        var page = new MainPage();
        var captures = new ObservedSnapshotter();
        var spread = page.FindByName<PageSpreadView>("SpreadPageView");
        var content = page.FindByName<PageCurlView>("ContentPageView");
        typeof(PageSpreadView).GetField("_snapshotter", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(spread, captures);
        typeof(PageCurlView).GetField("_snapshotter", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(content, captures);
        window.Width = 1728;
        window.Height = 1000;
        window.Page = page;
        await WaitUntilAsync(() => page.FindByName<PageTurnView>("SinglePageView").LastContentViewport.IsValid);

        foreach (var delay in new[] { 0, 8, 16, 24, 40, 80 })
        {
            Click(page, "SpreadSceneButton");
            await WaitUntilAsync(() => spread.LeftContent?.Handler is not null && spread.LeftContent.Width > 0);
            await spread.RefreshSnapshotAsync();
            await Task.Delay(80);
            await WaitUntilAsync(() => page.FindByName<Button>("NextButton").IsEnabled);
            Click(page, "NextButton");
            Require(spread.Overlay.Controller.Snapshot.State == PageTurnState.Settling, "Pending-capture probe did not start a turn.");
            // Layout can invalidate a live page while the overlay is moving.
            // Exercise the actual layout callback, keeping the content unchanged.
            typeof(PageSpreadView).GetMethod("OnContentLayoutChanged", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(spread, [spread.RightContent, EventArgs.Empty]);
            var count = captures.Count;
            var measurement = await QueueClickAsync(page, captures, "SingleSceneButton", delay);
            results.Add($"QUEUED_SWITCH Spread->Single delay_ms={delay} queue_ms={measurement.Queue:F1} handler_ms={measurement.Handler:F1} next_dispatch_ms={measurement.Dispatch:F1} first_paint_ms={measurement.Paint:F1} handler_captures={measurement.HandlerCaptures}");
            results.Add(measurement.HandlerCaptures == 0
                ? $"PASS pending spread cancellation avoids outgoing native capture ({delay})"
                : $"FAIL pending spread cancellation rasterized outgoing content ({delay})");
            Require(measurement.Queue < 150 && measurement.Handler < 150 && measurement.Dispatch < 250 && measurement.Paint < 250,
                $"Queued spread switch exceeded its response budget ({delay}).");
            Require(spread.Overlay.Controller.Snapshot.State == PageTurnState.Idle, "Queued switch did not cancel the spread.");
            await Task.Delay(30);
            results.Add(captures.Count == count
                ? $"PASS hidden spread discards pending automatic capture ({delay})"
                : $"FAIL hidden spread performed obsolete automatic capture ({delay})");
        }

        await Queued_switch_after_rendered_drag(page, captures, results);
        await Queued_switch_during_spread_capture(page, captures, results);

        Click(page, "ContentSceneButton");
        Click(page, "InteractiveContentButton");
        await WaitUntilAsync(() => content.FrontContent?.Handler is not null && content.FrontContent.Width > 0);
        await content.RefreshSnapshotAsync();
        await Task.Delay(80);
        var live = (ContentView)content.FrontContent!;
        var add = ((Grid)live.Content).Children.OfType<Button>().Single();
        var label = (Label)((Grid)live.Content).Children[2];
        var before = int.Parse(label.Text);
        var countBefore = captures.Count;
        var captureStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        captures.NextCaptureStarted = () => captureStarted.TrySetResult();
        // Queue from a worker as soon as rasterization begins. Starting a
        // stopwatch after a UI-context await would hide this queued interval.
        var queued = QueueClickAsync(page, captures, "SingleSceneButton", 0, captureStarted.Task);
        for (var click = 0; click < 5; click++) ((IButtonController)add).SendClicked();
        var burst = await queued;
        results.Add($"QUEUED_LIVE_SWITCH clicks=5 queue_ms={burst.Queue:F1} handler_ms={burst.Handler:F1} next_dispatch_ms={burst.Dispatch:F1} first_paint_ms={burst.Paint:F1} source_captures={captures.Count - countBefore}");
        Require(captures.LastStarted <= burst.Requested && burst.Requested < captures.LastFinished,
            "The live switch request did not overlap native rasterization.");
        Require(int.Parse(label.Text) == before + 5, "Coalescing dropped a live counter input.");
        results.Add(captures.Count - countBefore == 1
            ? "PASS live counter burst captures the latest content once"
            : $"FAIL live counter burst performed {captures.Count - countBefore} native captures");
        results.Add(burst.Queue < 150 && burst.Handler < 150 && burst.Dispatch < 250 && burst.Paint < 250
            ? "PASS queued scene input stays responsive during live refresh"
            : $"FAIL queued scene input exceeded its live refresh response budget: queue={burst.Queue:F1} handler={burst.Handler:F1} dispatch={burst.Dispatch:F1} paint={burst.Paint:F1}");

        // Copy the automatic result before re-entry can replace its owned image.
        // Compare against an explicit native capture of the latest live content.
        using var actualPixels = SKBitmap.FromImage(content.Renderer.FrontImage!);
        Click(page, "ContentSceneButton");
        await content.RefreshSnapshotAsync();
        using var expected = await new PageContentSnapshotter().CaptureAsync(live, default);
        using var expectedPixels = SKBitmap.FromImage(expected);
        Require(actualPixels.Width == expectedPixels.Width && actualPixels.Height == expectedPixels.Height,
            "Live refresh changed native snapshot resolution.");
        for (var py = 0; py < actualPixels.Height; py++)
        for (var px = 0; px < actualPixels.Width; px++)
            Require(actualPixels.GetPixel(px, py) == expectedPixels.GetPixel(px, py), "Latest live counter snapshot pixels differ.");
        results.Add("PASS coalesced live refresh retains current native pixels and resolution");
    }

    private static async Task<ClickTiming> QueueClickAsync(MainPage page, ObservedSnapshotter captures,
        string buttonName, int delay, Task? signal = null)
    {
        var dispatcher = page.Dispatcher;
        var clicked = new TaskCompletionSource<ClickTiming>(TaskCreationOptions.RunContinuationsAsynchronously);
        var single = page.FindByName<PageTurnView>("SinglePageView");
        var painted = new TaskCompletionSource<long>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<SKPaintGLSurfaceEventArgs> onPaint = (_, _) =>
        {
            if (single.IsVisible && single.LastContentViewport.IsValid && single.Controller.Snapshot.State == PageTurnState.Idle)
                painted.TrySetResult(Stopwatch.GetTimestamp());
        };
        if (buttonName == "SingleSceneButton") single.PaintSurface += onPaint;
        try
        {
            await Task.Run(async () =>
            {
                if (signal is not null) await signal.ConfigureAwait(false);
                if (delay > 0) await Task.Delay(delay).ConfigureAwait(false);
                var requested = Stopwatch.GetTimestamp();
                if (!dispatcher.Dispatch(() =>
                {
                    try
                    {
                        var entered = Stopwatch.GetTimestamp();
                        var count = captures.Count;
                        Click(page, buttonName);
                        var returned = Stopwatch.GetTimestamp();
                        var duringHandler = captures.Count - count;
                        if (!dispatcher.Dispatch(() => clicked.TrySetResult(new(
                            Milliseconds(requested, entered), Milliseconds(entered, returned),
                            Milliseconds(requested, Stopwatch.GetTimestamp()), duringHandler, requested, double.NaN))))
                            clicked.TrySetException(new InvalidOperationException("Follow-up dispatch was rejected."));
                    }
                    catch (Exception exception) { clicked.TrySetException(exception); }
                })) clicked.TrySetException(new InvalidOperationException("Scene dispatch was rejected."));
            }).WaitAsync(UiTimeout);
            var timing = await clicked.Task.WaitAsync(UiTimeout);
            return buttonName == "SingleSceneButton"
                ? timing with { Paint = Milliseconds(timing.Requested, await painted.Task.WaitAsync(UiTimeout)) }
                : timing;
        }
        finally { single.PaintSurface -= onPaint; }
    }

    private static async Task Queued_switch_after_rendered_drag(MainPage page, ObservedSnapshotter captures, List<string> results)
    {
        var single = page.FindByName<PageTurnView>("SinglePageView");
        foreach (var delay in new[] { 0, 8, 16, 24, 40, 80 })
        foreach (var destination in new[] { "Spread", "Content", "Backside" })
        {
            Click(page, "SingleSceneButton");
            await WaitUntilAsync(() => single.LastContentViewport.IsValid && page.FindByName<Button>("NextButton").IsEnabled);
            var viewport = single.LastContentViewport;
            var grab = new SKPoint(viewport.Origin.X + viewport.Size.X - 1, viewport.Origin.Y + viewport.Size.Y * .5f);
            var pulled = grab;
            SendTouch(single, SKTouchAction.Pressed, grab);
            Require(single.Controller.Snapshot.State == PageTurnState.Dragging, "Queued drag probe did not acquire a page.");
            for (var frame = 0; frame < 3; frame++)
            {
                var painted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                EventHandler<SKPaintGLSurfaceEventArgs> onPaint = (_, _) => painted.TrySetResult();
                single.PaintSurface += onPaint;
                try
                {
                    pulled = new SKPoint(grab.X - viewport.Size.X * (.2f + frame * .15f), grab.Y + viewport.Size.Y * .1f);
                    SendTouch(single, SKTouchAction.Moved, pulled);
                    await painted.Task.WaitAsync(UiTimeout);
                }
                finally { single.PaintSurface -= onPaint; }
            }
            var released = Stopwatch.GetTimestamp();
            SendTouch(single, SKTouchAction.Released, pulled);
            var measurement = await QueueClickAsync(page, captures, destination + "SceneButton", delay);
            Require(single.Controller.Snapshot.State == PageTurnState.Idle, "A queued scene switch left the outgoing drag settling.");
            Require(measurement.Queue < 150 && measurement.Handler < 150 && measurement.Dispatch < 250,
                $"Queued Single->{destination} switch exceeded its response budget ({delay}).");
            await WaitUntilAsync(() => page.FindByName<Button>("NextButton").IsEnabled
                && (destination == "Spread" || SnapshotReady(page.FindByName<PageCurlView>($"{destination}PageView"))));
            var ready = Milliseconds(measurement.Requested, Stopwatch.GetTimestamp());
            results.Add($"QUEUED_DRAG_SWITCH Single->{destination} delay_ms={delay} release_to_request_ms={Milliseconds(released, measurement.Requested):F1} queue_ms={measurement.Queue:F1} handler_ms={measurement.Handler:F1} next_dispatch_ms={measurement.Dispatch:F1} navigation_ready_ms={ready:F1}");
        }
        results.Add("PASS queued scene switches stay responsive after rendered Single drags");
    }

    private static async Task Queued_switch_during_spread_capture(MainPage page, ObservedSnapshotter captures, List<string> results)
    {
        Click(page, "SingleSceneButton");
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        captures.NextCaptureStarted = () => started.TrySetResult();
        var count = captures.Count;
        var queued = QueueClickAsync(page, captures, "SingleSceneButton", 0, started.Task);
        Click(page, "SpreadSceneButton");
        var timing = await queued;
        var total = captures.Count - count;
        results.Add($"QUEUED_CAPTURE_SWITCH Spread->Single queue_ms={timing.Queue:F1} handler_ms={timing.Handler:F1} next_dispatch_ms={timing.Dispatch:F1} first_paint_ms={timing.Paint:F1} source_captures={total}");
        Require(total == 1, "Queued scene input waited behind multiple outgoing spread captures.");
        Require(captures.LastStarted <= timing.Requested && timing.Requested < captures.LastFinished,
            "The spread switch request did not overlap native rasterization.");
        Require(timing.Queue < 150 && timing.Handler < 150 && timing.Dispatch < 250 && timing.Paint < 250,
            "Queued scene input exceeded its spread capture response budget.");
        results.Add("PASS queued scene input interrupts spread capture between native slots");
    }

    private static void SendTouch(PageTurnView view, SKTouchAction action, SKPoint point) =>
        typeof(PageTurnView).GetMethod("OnTouch", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)!
            .Invoke(view, [view, new SKTouchEventArgs(1, action, point, action != SKTouchAction.Released)]);

    private static double Milliseconds(long start, long end) => (end - start) * 1000d / Stopwatch.Frequency;
    private readonly record struct ClickTiming(double Queue, double Handler, double Dispatch, int HandlerCaptures, long Requested, double Paint);

    private sealed class ObservedSnapshotter : IPageContentSnapshotter
    {
        private readonly PageContentSnapshotter _inner = new();
        public int Count { get; private set; }
        public long LastStarted { get; private set; }
        public long LastFinished { get; private set; }
        public Action? NextCaptureStarted { get; set; }
        public Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken)
        {
            var notify = NextCaptureStarted;
            NextCaptureStarted = null;
            LastStarted = Stopwatch.GetTimestamp();
            notify?.Invoke();
            try { return _inner.CaptureAsync(content, cancellationToken); }
            finally { LastFinished = Stopwatch.GetTimestamp(); Count++; }
        }
    }
}
