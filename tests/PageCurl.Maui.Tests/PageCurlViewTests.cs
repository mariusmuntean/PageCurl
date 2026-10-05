using Microsoft.Maui.Controls;
using PageCurl.Core;
using SkiaSharp;
using System.Reflection;

namespace PageCurl.Maui.Tests;

internal static class PageCurlViewTests
{
    public static async Task RunAllAsync()
    {
        await Superseded_capture_does_not_report_a_stale_failure();
        await Hidden_capture_cannot_publish_after_visibility_returns();
        await Capture_failure_does_not_clear_the_active_sheet_back();
        await Active_curl_defers_snapshot_replacement();
        await Front_and_back_content_are_captured_independently();
        await Failed_refresh_keeps_the_last_good_snapshot();
        await Final_cancelled_capture_propagates_and_disposes_returned_image();
        Caller_images_survive_replacement_cancellation_and_teardown();
        Cancellation_finishes_before_a_view_is_hidden();
        Renderer_uses_the_stage_instead_of_the_page_frame();
        First_programmatic_turn_uses_the_live_page_frame();
        Interaction_properties_forward_to_the_renderer();
        Owned_release_and_cancel_remain_handled();
        Activation_layer_preserves_live_content_input();
        Flat_content_hides_renderer_outside_turns();
        Handler_teardown_restores_live_content();
    }

    private static async Task Superseded_capture_does_not_report_a_stale_failure()
    {
        var snapshotter = new SuspendedSnapshotter();
        var view = new PageCurlView(snapshotter) { FrontContent = new Grid() };
        var failures = 0;
        view.SnapshotFailed += (_, _) => failures++;
        var old = new TaskCompletionSource<SKImage>();
        snapshotter.Pending = old;
        var obsolete = view.RefreshSnapshotAsync();
        snapshotter.Pending = null;
        await view.RefreshSnapshotAsync();
        old.SetException(new InvalidOperationException("obsolete failure"));
        await TestAssert.ThrowsAsync<OperationCanceledException>(() => obsolete);
        TestAssert.Equal(0, failures);
        TestAssert.True(view.Renderer.FrontImage!.Handle != IntPtr.Zero);
    }

    private static async Task Hidden_capture_cannot_publish_after_visibility_returns()
    {
        foreach (var fails in new[] { true, false })
        {
            var snapshotter = new SuspendedSnapshotter();
            var view = new PageCurlView(snapshotter) { FrontContent = new Grid() };
            try
            {
                await view.RefreshSnapshotAsync();
                var previous = view.Renderer.FrontImage!;
                var failures = 0;
                view.SnapshotFailed += (_, _) => failures++;
                var pending = new TaskCompletionSource<SKImage>();
                snapshotter.Pending = pending;
                var capture = view.RefreshSnapshotAsync();
                view.IsVisible = false;
                view.IsVisible = true;
                using var surface = SKSurface.Create(new SKImageInfo(4, 4));
                var stale = surface.Snapshot();
                if (fails) { stale.Dispose(); pending.SetException(new InvalidOperationException("hidden failure")); }
                else pending.SetResult(stale);
                await capture;
                TestAssert.Equal(0, failures);
                TestAssert.Same(previous, view.Renderer.FrontImage!);
                TestAssert.Equal(IntPtr.Zero, stale.Handle);
            }
            finally
            {
                typeof(PageCurlView).GetMethod("OnHandlerChanged", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(view, null);
            }
        }
    }

    private static async Task Capture_failure_does_not_clear_the_active_sheet_back()
    {
        var snapshotter = new SuspendedSnapshotter();
        var view = new PageCurlView(snapshotter) { FrontContent = new Grid(), BackContent = new Grid() };
        await view.RefreshSnapshotAsync();
        var back = view.Renderer.BackImage!;
        view.BackContent = null;
        snapshotter.Pending = new TaskCompletionSource<SKImage>();
        var refresh = view.RefreshSnapshotAsync();
        view.Renderer.Controller.BeginDrag(new(400, 250), new(400, 500), PageAnchor.Left);
        snapshotter.Pending.SetException(new InvalidOperationException("capture failed"));
        // The test snapshotter runs continuations inline, so this checks the
        // null back slot immediately after the failed front await resumes.
        TestAssert.False(refresh.IsCompleted, "Capture failure cleared a back still used by the curl.");
        TestAssert.True(back.Handle != IntPtr.Zero);
        view.CancelInteraction();
        await refresh.WaitAsync(TimeSpan.FromSeconds(2));
        TestAssert.True(view.Renderer.BackImage is null);
    }

    private static async Task Active_curl_defers_snapshot_replacement()
    {
        var snapshotter = new FakeSnapshotter();
        foreach (var color in new[] { SKColors.Red, SKColors.Blue, SKColors.Green, SKColors.White })
            snapshotter.EnqueueImage(color);
        var view = new PageCurlView(snapshotter) { FrontContent = new Grid(), BackContent = new Grid() };
        await view.RefreshSnapshotAsync();
        var front = view.Renderer.FrontImage!;
        var back = view.Renderer.BackImage!;
        view.Renderer.Controller.BeginDrag(new(400, 250), new(400, 500), PageAnchor.Left);
        view.Renderer.Controller.UpdateDrag(new(200, 250));
        var refresh = view.RefreshSnapshotAsync();
        TestAssert.False(refresh.IsCompleted, "Refresh replaced the moving sheet.");
        TestAssert.Same(front, view.Renderer.FrontImage!);
        TestAssert.True(front.Handle != IntPtr.Zero && back.Handle != IntPtr.Zero);
        view.CancelInteraction();
        await refresh.WaitAsync(TimeSpan.FromSeconds(2));
        TestAssert.False(ReferenceEquals(front, view.Renderer.FrontImage));
        TestAssert.Equal(IntPtr.Zero, front.Handle);
    }

    private static void Caller_images_survive_replacement_cancellation_and_teardown()
    {
        using var surface = SKSurface.Create(new SKImageInfo(4, 4));
        using var front = surface.Snapshot();
        using var back = surface.Snapshot();
        using var under = surface.Snapshot();
        using var previous = surface.Snapshot();
        using var replacement = surface.Snapshot();
        var view = new PageTurnView
        {
            FrontImage = front, BackImage = back, UnderPageImage = under, PreviousPageImage = previous,
            BacksideMode = PageBacksideMode.CustomContent,
        };
        TestAssert.True(view.TryBeginInteraction(new(399, 250), new(400, 500), PageAnchor.Left));
        view.UpdateInteraction(new(100, 250));
        view.CancelInteraction();
        foreach (var property in new[] { PageTurnView.FrontImageProperty, PageTurnView.BackImageProperty,
            PageTurnView.UnderPageImageProperty, PageTurnView.PreviousPageImageProperty })
            view.SetValue(property, replacement);
        typeof(PageTurnView).GetMethod("OnHandlerChanged", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(view, null);
        foreach (var property in new[] { PageTurnView.FrontImageProperty, PageTurnView.BackImageProperty,
            PageTurnView.UnderPageImageProperty, PageTurnView.PreviousPageImageProperty })
            view.SetValue(property, null);
        foreach (var image in new[] { front, back, under, previous, replacement })
            TestAssert.True(image.Handle != IntPtr.Zero, "The view disposed a caller-owned image.");
    }

    private static void Cancellation_finishes_before_a_view_is_hidden()
    {
        var view = new PageTurnView();
        view.Controller.BeginDrag(new(400, 250), new(400, 500), PageAnchor.Left);
        view.Controller.UpdateDrag(new(100, 250));
        var completions = 0;
        view.PageTurnCompleted += (_, e) => { TestAssert.False(e.Committed); completions++; };
        view.CancelInteraction();
        TestAssert.Equal(PageTurnState.Idle, view.Controller.Snapshot.State);
        TestAssert.Equal(1, completions);
        TestAssert.False(view.Controller.Tick(1));
    }

    private static void Renderer_uses_the_stage_instead_of_the_page_frame()
    {
        var view = new PageCurlView(new FakeSnapshotter());
        TestAssert.Same(view, view.Renderer.Parent);
        TestAssert.Equal(PageContentScale.AspectFit, view.Renderer.ContentScale);
    }

    private static void First_programmatic_turn_uses_the_live_page_frame()
    {
        var view = new PageCurlView(new FakeSnapshotter()) { ReduceMotion = true };
        var frame = (Grid)view.Children[0];
        frame.WidthRequest = 400;
        frame.HeightRequest = 500;
        TestAssert.False(view.Renderer.IsVisible);
        var completed = 0;
        view.PageTurnCompleted += (_, e) => { if (e.Committed) completed++; };
        TestAssert.True(view.TryTurn(PageTurnDirection.Forward), "First command on a live page did not start.");
        TestAssert.Equal(1, completed);
        TestAssert.False(view.Renderer.IsVisible);
    }

    private static void Owned_release_and_cancel_remain_handled()
    {
        foreach (var action in new[] { SkiaSharp.Views.Maui.SKTouchAction.Released, SkiaSharp.Views.Maui.SKTouchAction.Cancelled })
        {
            var view = new PageTurnView { ReduceMotion = true };
            TestAssert.True(view.Controller.BeginDrag(new System.Numerics.Vector2(200, 250),
                new System.Numerics.Vector2(400, 500), PageAnchor.Left));
            var capture = typeof(PageTurnView).GetField("_pointerCapture", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
            capture.GetType().GetMethod("TryCapture")!.Invoke(capture, [42L]);
            // No dispatcher is required: the test drives the controller timer.
            typeof(PageTurnView).GetField("_animationLoopActive", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(view, true);
            var touch = new SkiaSharp.Views.Maui.SKTouchEventArgs(42L, action, new SKPoint(),
                action != SkiaSharp.Views.Maui.SKTouchAction.Released);
            typeof(PageTurnView).GetMethod("OnTouch", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)!
                .Invoke(view, [view, touch]);
            TestAssert.True(touch.Handled, "Owned release/cancel escaped input dispatch.");
            TestAssert.TickUntilIdle(view.Controller);
        }
    }

    private static async Task Front_and_back_content_are_captured_independently()
    {
        var snapshotter = new FakeSnapshotter();
        snapshotter.EnqueueImage(SKColors.Red);
        snapshotter.EnqueueImage(SKColors.Blue);
        var view = new PageCurlView(snapshotter)
        {
            FrontContent = new Grid(),
            BackContent = new Grid(),
        };

        await view.RefreshSnapshotAsync();

        TestAssert.NotNull(view.Renderer.FrontImage);
        TestAssert.NotNull(view.Renderer.BackImage);
        TestAssert.False(ReferenceEquals(view.Renderer.FrontImage, view.Renderer.BackImage));
    }

    private static async Task Failed_refresh_keeps_the_last_good_snapshot()
    {
        var snapshotter = new FakeSnapshotter();
        snapshotter.EnqueueImage(SKColors.Red);
        snapshotter.EnqueueImage(SKColors.Blue);
        var view = new PageCurlView(snapshotter)
        {
            FrontContent = new Grid(),
            BackContent = new Grid(),
        };
        await view.RefreshSnapshotAsync();
        var firstFront = view.Renderer.FrontImage;
        var firstBack = view.Renderer.BackImage;
        PageSnapshotFailedEventArgs? failed = null;
        view.SnapshotFailed += (_, args) => failed = args;
        snapshotter.EnqueueFailure(new InvalidOperationException("front failed"));
        snapshotter.EnqueueImage(SKColors.Green);

        await view.RefreshSnapshotAsync();

        TestAssert.Same(firstFront!, view.Renderer.FrontImage!);
        TestAssert.False(ReferenceEquals(firstBack, view.Renderer.BackImage));
        TestAssert.NotNull(failed);
        TestAssert.False(failed!.IsBackside);
        TestAssert.Equal("front failed", failed.Exception.Message);
    }

    private static async Task Final_cancelled_capture_propagates_and_disposes_returned_image()
    {
        using var cancellation = new CancellationTokenSource();
        var snapshotter = new CancellingSnapshotter(cancellation);
        var view = new PageCurlView(snapshotter)
        {
            BackContent = new Grid(),
        };

        await TestAssert.ThrowsAsync<OperationCanceledException>(() =>
            view.RefreshSnapshotAsync(cancellation.Token));

        TestAssert.NotNull(snapshotter.ReturnedImage);
        TestAssert.Equal(IntPtr.Zero, snapshotter.ReturnedImage!.Handle);
    }

    private static void Interaction_properties_forward_to_the_renderer()
    {
        var view = new PageCurlView(new FakeSnapshotter())
        {
            LockedAnchor = PageAnchor.BottomRight,
            PageChangeThreshold = 0.42f,
            FlickEnabled = false,
            FlickVelocityThreshold = 2.25f,
            PageAspectRatio = 0.68f,
            BacksideMode = PageBacksideMode.SolidColor,
            BacksideDimFactor = 0.7f,
            ReduceMotion = true,
        };

        TestAssert.Equal(PageAnchor.BottomRight, view.Renderer.LockedAnchor);
        TestAssert.Equal(0.42f, view.Renderer.PageChangeThreshold);
        TestAssert.False(view.Renderer.FlickEnabled);
        TestAssert.Equal(2.25f, view.Renderer.FlickVelocityThreshold);
        TestAssert.Equal(PageBacksideMode.SolidColor, view.Renderer.BacksideMode);
        TestAssert.Equal(0.7f, view.Renderer.BacksideDimFactor);
        TestAssert.True(view.Renderer.ReduceMotion);
    }

    private static void Activation_layer_preserves_live_content_input()
    {
        var view = new PageCurlView(new FakeSnapshotter());
        var pageFrame = (Grid)view.Children[0];
        var activationLayer = (Grid)pageFrame.Children[^1];

        TestAssert.True(activationLayer.InputTransparent);
        TestAssert.False(activationLayer.CascadeInputTransparent);
        TestAssert.True(activationLayer.Children.All(child => !child.InputTransparent));
    }

    private static void Flat_content_hides_renderer_outside_turns()
    {
        var view = new PageCurlView(new FakeSnapshotter());

        TestAssert.False(view.Renderer.IsVisible);

        view.Renderer.IsVisible = true;
        view.CancelInteraction();

        TestAssert.False(view.Renderer.IsVisible);
    }

    private static void Handler_teardown_restores_live_content()
    {
        var view = new PageCurlView(new FakeSnapshotter());
        view.Renderer.IsVisible = true;

        InvokeHandlerChanged(view);

        TestAssert.False(view.Renderer.IsVisible);
        var pageFrame = (Grid)view.Children[0];
        var frontHost = (Grid)pageFrame.Children[1];
        TestAssert.True(frontHost.IsVisible);
    }

    private static void InvokeHandlerChanged(PageCurlView view) =>
        typeof(PageCurlView)
            .GetMethod("OnHandlerChanged", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(view, null);

    private sealed class FakeSnapshotter : IPageContentSnapshotter
    {
        private readonly Queue<Func<SKImage>> _captures = new();

        public void EnqueueImage(SKColor color) => _captures.Enqueue(() =>
        {
            using var surface = SKSurface.Create(new SKImageInfo(4, 4));
            surface.Canvas.Clear(color);
            return surface.Snapshot();
        });

        public void EnqueueFailure(Exception exception) =>
            _captures.Enqueue(() => throw exception);

        public Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_captures.Dequeue()());
        }
    }

    private sealed class SuspendedSnapshotter : IPageContentSnapshotter
    {
        public TaskCompletionSource<SKImage>? Pending { get; set; }
        public Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken)
        {
            if (Pending is not null) return Pending.Task;
            using var surface = SKSurface.Create(new SKImageInfo(4, 4));
            surface.Canvas.Clear(SKColors.White);
            return Task.FromResult(surface.Snapshot());
        }
    }

    private sealed class CancellingSnapshotter(CancellationTokenSource cancellation)
        : IPageContentSnapshotter
    {
        public SKImage? ReturnedImage { get; private set; }

        public Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken)
        {
            using var surface = SKSurface.Create(new SKImageInfo(4, 4));
            ReturnedImage = surface.Snapshot();
            cancellation.Cancel();
            return Task.FromResult(ReturnedImage);
        }
    }
}
