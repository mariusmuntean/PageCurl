using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Maui.Controls;
using PageCurl.Core;
using PageCurl.Maui;
using ReaderApp;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using UIKit;
using static PageCurl.Apple.Tests.NativeTestSupport;

namespace PageCurl.Apple.Tests;

internal static class ReaderExampleTests
{
    private static readonly FieldInfo Snapshotter = typeof(PageSpreadView).GetField("_snapshotter", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public static async Task RunAsync(Window window, List<string> results)
    {
        var page = new ReaderPage();
        var book = page.FindByName<PageSpreadView>("Book");
        var counter = page.FindByName<Label>("PageCounter");
        var previous = page.FindByName<Button>("PreviousButton");
        var next = page.FindByName<Button>("NextButton");
        window.Width = 1000;
        window.Height = 800;
        window.Page = page;
        await WaitUntilAsync(() => book.Handler is not null && book.Width > 0);
        Require(book.LeftContent is not null && book.RightContent is not null, "Reader initial spread content missing.");
        await WaitUntilAsync(() => next.IsEnabled);
        Require(counter.Text == "1–2 / 5" && !previous.IsEnabled, "Reader initial boundaries/counter differ.");
        Require(!book.TryTurnBackward(), "Reader accepted a backward command at the first spread.");
        var slots = new[] { book.LeftContent, book.RightContent, book.NextLeftContent, book.NextRightContent };
        Require(slots.Distinct().Count() == slots.Length, "Reader reuses a view in multiple occupied slots.");
        results.Add("PASS reader initial readiness, distinct views and first boundary");
        using (var baseline = new UILabel { Text = "Reader accessibility probe" })
            results.Add($"READER_ACCESSIBILITY baseline_element={baseline.IsAccessibilityElement} baseline_label={baseline.AccessibilityLabel ?? "<null>"}");
        foreach (var content in new[] { book.LeftContent, book.RightContent }.OfType<Grid>())
        {
            var text = (VerticalStackLayout)content.Children.OfType<ScrollView>().Single().Content;
            foreach (var label in text.Children.OfType<Label>())
            {
                var native = (UIView)label.Handler!.PlatformView!;
                Require(native.IsAccessibilityElement && native.AccessibilityLabel == label.Text,
                    $"Reader page text has no native accessible label: text={label.Text}, type={native.GetType().Name}, element={native.IsAccessibilityElement}, label={native.AccessibilityLabel ?? "<null>"}.");
                for (var parent = native.Superview; parent is not null; parent = parent.Superview)
                    Require(!parent.IsAccessibilityElement && !parent.AccessibilityElementsHidden
                        && !parent.Hidden && parent.Alpha > 0, "Reader container hides its accessible page text.");
            }
        }
        results.Add("PASS reader native accessibility exposes current page text");

        Click(page, "NextButton");
        Require(book.Overlay.Controller.Snapshot.State == PageTurnState.Settling
            && !next.IsEnabled && !previous.IsEnabled && counter.Text == "1–2 / 5", "Reader advanced before completion or left commands enabled.");
        Click(page, "NextButton");
        await WaitUntilAsync(() => next.IsEnabled && previous.IsEnabled);
        Require(counter.Text == "3–4 / 5", "Rapid reader commands skipped a spread.");
        Require(PageNumber(book.NextLeftContent) == "5" && book.NextRightContent is Grid blank && blank.Children.Count == 0,
            "Reader did not prepare the odd last page and blank underneath destination.");
        Click(page, "NextButton");
        await WaitUntilAsync(() => previous.IsEnabled && book.Overlay.Controller.Snapshot.State == PageTurnState.Idle);
        Require(counter.Text == "5 / 5" && PageNumber(book.LeftContent) == "5" && book.RightContent is null
            && !next.IsEnabled && !book.TryTurnForward(), "Reader final spread/boundary differs.");
        Click(page, "PreviousButton");
        await WaitUntilAsync(() => next.IsEnabled && previous.IsEnabled);
        Click(page, "PreviousButton");
        await WaitUntilAsync(() => next.IsEnabled && !previous.IsEnabled);
        Require(counter.Text == "1–2 / 5", "Reader backward journey did not restore its first spread.");
        results.Add("PASS reader animated journey, rapid input, odd last page and both boundaries");

        book.FlickEnabled = false;
        await DragAsync(book, .2f);
        Require(counter.Text == "1–2 / 5", "Reader rollback advanced the model.");
        await DragAsync(book, .8f);
        await WaitUntilAsync(() => next.IsEnabled && previous.IsEnabled);
        Require(counter.Text == "3–4 / 5", "Reader gesture did not share command completion.");
        results.Add("PASS reader gesture commit and rollback use the shared completion path");

        Click(page, "NextButton");
        ((IWindow)window).Stopped();
        Require(book.Overlay.Controller.Snapshot.State == PageTurnState.Idle && counter.Text == "3–4 / 5"
            && !next.IsEnabled && !previous.IsEnabled, "Reader stop failed to cancel before hiding.");
        ((IWindow)window).Resumed();
        await WaitUntilAsync(() => next.IsEnabled && previous.IsEnabled);
        Click(page, "NextButton");
        window.Page = new ContentPage();
        ((IWindow)window).Stopped();
        ((IWindow)window).Resumed();
        await Task.Delay(40);
        Require(!book.IsVisible && counter.Text == "3–4 / 5" && !next.IsEnabled,
            "Hidden reader resumed or committed after leaving.");
        window.Page = page;
        await WaitUntilAsync(() => next.IsEnabled);
        results.Add("PASS reader window stop, resume, leaving and re-entry");

        var original = Snapshotter.GetValue(book);
        var failure = new ControlledSnapshotter();
        Snapshotter.SetValue(book, failure);
        try
        {
            failure.Fail = true;
            await book.RefreshSnapshotAsync();
            var error = page.FindByName<VerticalStackLayout>("CaptureError");
            var retry = page.FindByName<Button>("RetryButton");
            Require(error.IsVisible && counter.Text == "3–4 / 5" && !next.IsEnabled, "Reader failed capture changed navigation or hid its error.");
            failure.Fail = false;
            failure.Gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Click(page, "RetryButton");
            await WaitUntilAsync(() => failure.Pending > 0);
            var calls = failure.Calls;
            Click(page, "RetryButton");
            Require(!retry.IsEnabled && failure.Calls == calls, "Reader retry overlapped an unfinished capture.");
            failure.Gate.SetResult();
            await WaitUntilAsync(() => retry.IsEnabled && next.IsEnabled);
            Require(!error.IsVisible && counter.Text == "3–4 / 5", "Reader retry did not recover its current spread.");
            results.Add("PASS reader capture failure and serialized retry retain navigation");

            failure.Fail = true;
            failure.Gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
            // Hold the old await continuation until the resumed retry has begun.
            var ui = SynchronizationContext.Current!;
            var delayed = new DeferredContext(ui);
            SynchronizationContext.SetSynchronizationContext(delayed);
            try { Click(page, "RetryButton"); }
            finally { SynchronizationContext.SetSynchronizationContext(ui); }
            await WaitUntilAsync(() => failure.Pending > 0);
            var oldGate = failure.Gate;
            ((IWindow)window).Stopped();
            ((IWindow)window).Resumed();
            failure.Fail = false;
            failure.Gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Click(page, "RetryButton");
            delayed.Release();
            Require(!retry.IsEnabled && !error.IsVisible && counter.Text == "3–4 / 5",
                "Old retry completion changed its resumed activation's pending retry.");
            calls = failure.Calls;
            oldGate.SetResult();
            await WaitUntilAsync(() => failure.Calls > calls && failure.Pending > 0);
            Require(!retry.IsEnabled && !error.IsVisible,
                "Stale capture failure changed the new activation before recovery.");
            failure.Gate.SetResult();
            await WaitUntilAsync(() => retry.IsEnabled && next.IsEnabled);
            Require(!error.IsVisible, "Reader stale retry affected its resumed activation.");
            results.Add("PASS reader retry across stop/resume recovers without stale state");
        }
        finally
        {
            failure.Gate?.TrySetResult();
            Snapshotter.SetValue(book, original);
        }
        // Constrain the root layout; Catalyst Window setters do not resize the desktop window.
        var layout = (Grid)page.Content;
        var commands = layout.Children.OfType<HorizontalStackLayout>().Single();
        layout.HorizontalOptions = LayoutOptions.Start;
        layout.VerticalOptions = LayoutOptions.Start;
        layout.HeightRequest = 750;
        foreach (var width in new[] { 600, 390, 320 })
        {
            layout.WidthRequest = width;
            await WaitUntilAsync(() => book.Width <= width && next.IsEnabled);
            Require(next.Width >= 44 && next.Height >= 44 && commands.Bounds.Bottom <= layout.Height,
                "Compact reader lost usable navigation.");
            foreach (var content in new[] { book.LeftContent, book.RightContent, book.PreviousLeftContent,
                book.PreviousRightContent, book.NextLeftContent }.OfType<Grid>())
            {
                var scroll = content.Children.OfType<ScrollView>().Single();
                Require(scroll.Height >= 44 && scroll.Bounds.Bottom <= content.Height,
                    $"Reader text is not reachable at {width}.");
                var labels = content.Children.OfType<Label>().Concat(((VerticalStackLayout)scroll.Content).Children.OfType<Label>());
                foreach (var label in labels)
                {
                    var desired = ((IView)label).Measure(label.Width, double.PositiveInfinity);
                    Require(label.Width > 0 && desired.Height <= label.Height + 1,
                        $"Reader text clipped at {width}: desired={desired.Height:F1}, allocated={label.Height:F1}, text={label.Text}");
                }
            }
        }
        results.Add("PASS reader compact and phone-width text layout");
        var right = (Grid)book.RightContent!;
        var textScroll = right.Children.OfType<ScrollView>().Single();
        await textScroll.ScrollToAsync(0, textScroll.Content.Height, animated: false);
        await WaitUntilAsync(() => textScroll.ScrollY > 0 && next.IsEnabled);
        using var actual = await new PageContentSnapshotter().CaptureAsync(right, default);
        if (Environment.GetEnvironmentVariable("PAGECURL_TEST_OUTPUT") is { } output)
        {
            using var png = actual.Encode(SKEncodedImageFormat.Png, 100);
            using var file = File.Create(Path.ChangeExtension(output, ".reader-page.png"));
            png.SaveTo(file);
        }
        Require(book.TryTurnForward(), "Reader could not turn after scrolling.");
        using var expectedPixels = SKBitmap.FromImage(actual);
        using var turningPixels = SKBitmap.FromImage(book.Overlay.FrontImage!);
        Require(expectedPixels.Width == turningPixels.Width && expectedPixels.Height == turningPixels.Height
            && expectedPixels.GetPixelSpan().SequenceEqual(turningPixels.GetPixelSpan()),
            "Reader turn used text from before scrolling.");
        book.CancelInteraction();
        Require(counter.Text == "3–4 / 5", "Cancelled reader scroll probe changed navigation.");
        results.Add("PASS reader scrolled text matches its native turn snapshot");
    }

    private static string? PageNumber(View? page) => (page as Grid)?.Children.OfType<Label>().Last().Text;
    private static void Click(ReaderPage page, string name) => ((IButtonController)page.FindByName<Button>(name)).SendClicked();

    private static async Task DragAsync(PageSpreadView book, float progress)
    {
        var frame = (Grid)book.Children.Single();
        var zone = frame.Children.OfType<SKCanvasView>().Single(view => Equals(view.BindingContext, PageTurnDirection.Forward));
        var scale = zone.CanvasSize.Width > 0 ? zone.CanvasSize.Width / zone.Width : 1;
        var grab = new SKPoint((float)((zone.Width - 1) * scale), (float)(zone.Height * .5 * scale));
        var pulled = new SKPoint(grab.X - (float)(frame.Width * progress * scale), grab.Y);
        var touch = typeof(PageSpreadView).GetMethod("OnActivationTouch", BindingFlags.Instance | BindingFlags.NonPublic)!;
        void Send(SKTouchAction action, SKPoint point) => touch.Invoke(book,
            [zone, new SKTouchEventArgs(91, action, point, action != SKTouchAction.Released)]);
        Send(SKTouchAction.Pressed, grab);
        Require(book.Overlay.Controller.Snapshot.State == PageTurnState.Dragging, "Reader gesture did not acquire a sheet.");
        Send(SKTouchAction.Moved, pulled);
        Send(SKTouchAction.Released, pulled);
        await WaitUntilAsync(() => book.Overlay.Controller.Snapshot.State == PageTurnState.Idle);
    }

    private sealed class DeferredContext(SynchronizationContext ui) : SynchronizationContext
    {
        private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _pending = new();
        private volatile bool _released;
        public override void Post(SendOrPostCallback callback, object? state)
        {
            if (_released) ui.Post(callback, state);
            else _pending.Enqueue((callback, state));
        }
        public void Release()
        {
            _released = true;
            while (_pending.TryDequeue(out var work)) work.Callback(work.State);
        }
    }

    private sealed class ControlledSnapshotter : IPageContentSnapshotter
    {
        private readonly PageContentSnapshotter _inner = new();
        public bool Fail;
        public TaskCompletionSource? Gate;
        public int Calls;
        public int Pending;
        public async Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken)
        {
            Calls++;
            Pending++;
            var fail = Fail;
            try
            {
                if (Gate is { } gate) await gate.Task;
                if (fail) throw new InvalidOperationException("Reader capture regression probe.");
                return await _inner.CaptureAsync(content, cancellationToken);
            }
            finally { Pending--; }
        }
    }
}
