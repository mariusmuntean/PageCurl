using Microsoft.Maui.Controls;
using PageCurl.Core;
using SkiaSharp;
using System.Numerics;
using System.Reflection;

namespace PageCurl.Maui.Tests;

internal static class PageSpreadViewTests
{
    public static async Task RunAllAsync()
    {
        await Teardown_rejects_a_turn_requested_by_the_cancellation_handler();
        await Cancellation_handler_can_start_a_replacement_spread_turn();
        await Activation_release_and_cancel_finish_only_the_owned_pointer();
        await Cancelled_queued_request_cannot_bypass_a_running_capture();
        await Deferred_refresh_is_awaitable_and_cancellable();
        await Teardown_and_cancellation_do_not_publish_suspended_captures();
        await Removed_optional_back_uses_the_configured_fallback();
        await Explicit_refresh_recaptures_in_place_changes();
        await Obsolete_capture_does_not_acknowledge_new_content();
        await Active_turn_keeps_all_overlay_images_alive();
        await Refresh_captures_current_and_destination_pages();
        await Destination_content_and_snapshots_control_navigation_availability();
        await Replaced_content_blocks_turns_until_its_snapshot_is_current();
        await Final_cancelled_capture_propagates_and_disposes_returned_image();
        await Refresh_requested_during_a_turn_runs_after_completion();
        await Failed_capture_keeps_the_slot_dirty_and_a_later_refresh_retries_only_it();
        await Refresh_captures_only_slots_with_changed_content();
        await Every_content_slot_blocks_only_its_turn_until_recaptured();
        await Forward_turn_shows_the_adjacent_page_on_the_sheet_back();
        await Backward_turn_shows_the_adjacent_page_on_the_sheet_back();
        Overlay_surface_preserves_the_stationary_page();
        Handler_teardown_restores_both_static_pages();
    }

    private static async Task Cancellation_handler_can_start_a_replacement_spread_turn()
    {
        var spread = await CreateReadySpreadAsync(new CountingSnapshotter());
        var completions = new List<PageTurnCompletedEventArgs>();
        spread.PageTurnCompleted += (_, args) =>
        {
            completions.Add(args);
            if (completions.Count == 1) BeginSettlingTurn(spread, PageTurnDirection.Backward);
        };
        try
        {
            BeginSettlingTurn(spread, PageTurnDirection.Forward);
            spread.CancelInteraction();
            TestAssert.Equal(PageTurnState.Settling, spread.Overlay.Controller.Snapshot.State);
            TestAssert.Equal(1d, spread.Overlay.Opacity);
            TestAssert.NotNull(spread.Overlay.FrontImage, "Cancellation cleared the replacement turn's textures.");
            TestAssert.NotNull(spread.Overlay.PreviousPageImage);
            TestAssert.False(spread.CanTurnForward || spread.CanTurnBackward);
            TestAssert.TickUntilIdle(spread.Overlay.Controller);
            TestAssert.Equal(2, completions.Count);
            TestAssert.False(completions[0].Committed);
            TestAssert.True(completions[1].Committed);
            TestAssert.Equal(PageTurnDirection.Backward, completions[1].Direction);
            TestAssert.Equal(0d, spread.Overlay.Opacity);
            TestAssert.True(spread.CanTurnForward && spread.CanTurnBackward);
        }
        finally { InvokeHandlerChanged(spread); }
    }

    private static async Task Teardown_rejects_a_turn_requested_by_the_cancellation_handler()
    {
        var spread = await CreateReadySpreadAsync(new CountingSnapshotter());
        BeginSettlingTurn(spread, PageTurnDirection.Forward);
        var restarted = false;
        var completions = 0;
        spread.PageTurnCompleted += (_, _) =>
        {
            completions++;
            restarted = (bool)typeof(PageSpreadView).GetMethod("PrepareTurn", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(spread, [PageTurnDirection.Backward])!;
        };
        InvokeHandlerChanged(spread);
        TestAssert.False(restarted, "Cancellation subscriber reused disconnected spread textures.");
        TestAssert.Equal(1, completions);
        TestAssert.Equal(PageTurnState.Idle, spread.Overlay.Controller.Snapshot.State);
        TestAssert.Null(spread.Overlay.FrontImage);
        TestAssert.Null(spread.Overlay.BackImage);
        TestAssert.False(spread.CanTurnForward || spread.CanTurnBackward);
    }

    private static async Task Every_content_slot_blocks_only_its_turn_until_recaptured()
    {
        var slots = new (BindableProperty Property, bool Forward, Func<PageTurnView, SKImage?> TurnImage)[]
        {
            (PageSpreadView.LeftContentProperty, false, view => view.FrontImage),
            (PageSpreadView.RightContentProperty, true, view => view.FrontImage),
            (PageSpreadView.PreviousLeftContentProperty, false, view => view.PreviousPageImage),
            (PageSpreadView.NextRightContentProperty, true, view => view.UnderPageImage),
            (PageSpreadView.PreviousRightContentProperty, false, view => view.BackImage),
            (PageSpreadView.NextLeftContentProperty, true, view => view.BackImage),
        };

        foreach (var (property, forward, turnImage) in slots)
        {
            var snapshotter = new DelayedSnapshotter();
            var spread = CreateSpread(snapshotter);
            spread.PreviousRightContent = new Grid();
            spread.NextLeftContent = new Grid();
            try
            {
                await spread.RefreshSnapshotAsync();
                TestAssert.True(spread.CanTurnForward && spread.CanTurnBackward);
                var oldContent = (View)spread.GetValue(property);
                var host = oldContent.Parent;
                var newContent = new Grid { WidthRequest = 13 };
                spread.SetValue(property, newContent);
                TestAssert.Null(oldContent.Parent);
                TestAssert.True(ReferenceEquals(host, newContent.Parent));
                TestAssert.Equal(!forward, spread.CanTurnForward);
                TestAssert.Equal(forward, spread.CanTurnBackward);

                await DrainAsync(spread);
                TestAssert.Equal(7, snapshotter.Count);
                TestAssert.True(spread.CanTurnForward && spread.CanTurnBackward);
                BeginSettlingTurn(spread, forward ? PageTurnDirection.Forward : PageTurnDirection.Backward);
                TestAssert.Equal(13, turnImage(spread.Overlay)!.Width);
                TestAssert.False(spread.CanTurnForward || spread.CanTurnBackward);
                spread.CancelInteraction();
                TestAssert.True(spread.CanTurnForward && spread.CanTurnBackward);
            }
            finally
            {
                InvokeHandlerChanged(spread);
            }
        }
    }

    private static async Task Activation_release_and_cancel_finish_only_the_owned_pointer()
    {
        foreach (var direction in new[] { PageTurnDirection.Forward, PageTurnDirection.Backward })
        foreach (var action in new[] { SkiaSharp.Views.Maui.SKTouchAction.Released, SkiaSharp.Views.Maui.SKTouchAction.Cancelled })
        {
            var spread = await CreateReadySpreadAsync(new CountingSnapshotter());
            try
            {
                spread.ReduceMotion = true;
                var frame = (Grid)spread.Children[0];
                frame.HorizontalOptions = LayoutOptions.Fill;
                frame.VerticalOptions = LayoutOptions.Fill;
                ((IView)frame).Arrange(new Rect(0, 0, 800, 500));
                var zone = frame.Children.OfType<SkiaSharp.Views.Maui.Controls.SKCanvasView>()
                    .Single(view => Equals(view.BindingContext, direction));
                // Neutral controls have no native layout handler; give the edge its real hit area.
                zone.HorizontalOptions = LayoutOptions.Fill;
                ((IView)zone).Arrange(new Rect(direction == PageTurnDirection.Forward ? 748 : 0, 0, 52, 500));
                var handler = typeof(PageSpreadView).GetMethod("OnActivationTouch", BindingFlags.Instance | BindingFlags.NonPublic)!;
                var completions = 0;
                spread.PageTurnCompleted += (_, args) =>
                {
                    TestAssert.Equal(action == SkiaSharp.Views.Maui.SKTouchAction.Released, args.Committed);
                    completions++;
                };
                void Send(long id, SkiaSharp.Views.Maui.SKTouchAction kind, float x, bool handled)
                {
                    var touch = new SkiaSharp.Views.Maui.SKTouchEventArgs(id, kind, new SKPoint(x, 250),
                        kind != SkiaSharp.Views.Maui.SKTouchAction.Released);
                    handler.Invoke(spread, [zone, touch]);
                    TestAssert.True(handled == touch.Handled, $"{direction} {kind}: handled={touch.Handled}, frame={frame.Bounds}, zone={zone.Bounds}");
                }
                Send(7, SkiaSharp.Views.Maui.SKTouchAction.Pressed,
                    direction == PageTurnDirection.Forward ? 51 : 1, true);
                Send(7, SkiaSharp.Views.Maui.SKTouchAction.Moved,
                    direction == PageTurnDirection.Forward ? -450 : 450, true);
                Send(99, action, 0, false);
                TestAssert.Equal(PageTurnState.Dragging, spread.Overlay.Controller.Snapshot.State);
                Send(7, action, 0, true);
                TestAssert.Equal(PageTurnState.Idle, spread.Overlay.Controller.Snapshot.State);
                TestAssert.Equal(1, completions);
                Send(7, action, 0, false);
                TestAssert.Equal(1, completions);
                TestAssert.True(spread.CanTurnForward && spread.CanTurnBackward);
            }
            finally
            {
                InvokeHandlerChanged(spread);
            }
        }
    }

    private static async Task Cancelled_queued_request_cannot_bypass_a_running_capture()
    {
        var snapshotter = new DelayedSnapshotter();
        var spread = CreateSpread(snapshotter);
        await spread.RefreshSnapshotAsync();
        var pending = snapshotter.DelayNext();
        var running = spread.RefreshSnapshotAsync();
        using var cancellation = new CancellationTokenSource();
        var cancelled = spread.RefreshSnapshotAsync(cancellation.Token);
        cancellation.Cancel();
        await TestAssert.ThrowsAsync<OperationCanceledException>(() => cancelled);
        var queued = DrainAsync(spread);
        TestAssert.Equal(5, snapshotter.Count);
        pending.SetResult(MakeImage(4));
        await Task.WhenAll(running, queued);
        TestAssert.True(spread.CanTurnForward && spread.CanTurnBackward);
    }

    private static async Task Deferred_refresh_is_awaitable_and_cancellable()
    {
        var snapshotter = new CountingSnapshotter();
        var spread = await CreateReadySpreadAsync(snapshotter);
        BeginSettlingTurn(spread, PageTurnDirection.Forward);
        using var cancellation = new CancellationTokenSource();
        var refresh = spread.RefreshSnapshotAsync(cancellation.Token);
        TestAssert.False(refresh.IsCompleted, "Refresh completed before the active turn ended.");
        cancellation.Cancel();
        await TestAssert.ThrowsAsync<OperationCanceledException>(() => refresh);
        TestAssert.Equal(4, snapshotter.CaptureCount);
        var next = spread.RefreshSnapshotAsync();
        TestAssert.TickUntilIdle(spread.Overlay.Controller);
        await next;
        TestAssert.Equal(8, snapshotter.CaptureCount);
        TestAssert.True(spread.CanTurnForward);
    }

    private static async Task Teardown_and_cancellation_do_not_publish_suspended_captures()
    {
        foreach (var teardown in new[] { false, true })
        {
            var snapshotter = new DelayedSnapshotter();
            var spread = CreateSpread(snapshotter);
            await spread.RefreshSnapshotAsync();
            spread.LeftContent = new Grid();
            var pending = snapshotter.DelayNext();
            using var cancellation = new CancellationTokenSource();
            var refresh = (Task)typeof(PageSpreadView)
                .GetMethod("DrainSnapshotRefreshAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(spread, [cancellation.Token])!;
            BeginSettlingTurn(spread, PageTurnDirection.Forward);
            var front = spread.Overlay.FrontImage!;
            if (teardown)
            {
                typeof(PageTurnView).GetMethod("OnHandlerChanged", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(spread.Overlay, null);
                InvokeHandlerChanged(spread);
            }
            else cancellation.Cancel();
            var rejected = MakeImage(11);
            pending.SetResult(rejected);
            await TestAssert.ThrowsAsync<OperationCanceledException>(() => refresh);
            TestAssert.Equal(IntPtr.Zero, rejected.Handle);
            if (teardown)
            {
                TestAssert.Null(spread.Overlay.FrontImage);
                TestAssert.Equal(IntPtr.Zero, front.Handle);
                TestAssert.False(spread.CanTurnForward);
            }
            else
            {
                TestAssert.True(front.Handle != IntPtr.Zero);
                spread.Overlay.Controller.Cancel();
                TestAssert.TickUntilIdle(spread.Overlay.Controller);
                TestAssert.Null(spread.Overlay.FrontImage);
                await DrainAsync(spread);
                TestAssert.True(spread.CanTurnForward && spread.CanTurnBackward);
            }
        }
    }

    private static async Task Removed_optional_back_uses_the_configured_fallback()
    {
        foreach (var direction in new[] { PageTurnDirection.Forward, PageTurnDirection.Backward })
        foreach (var mode in new[] { PageBacksideMode.MirroredFront, PageBacksideMode.SolidColor })
        {
            var snapshotter = new DelayedSnapshotter();
            var spread = CreateSpread(snapshotter);
            spread.BacksideMode = mode;
            spread.NextLeftContent = new Grid();
            spread.PreviousRightContent = new Grid();
            await spread.RefreshSnapshotAsync();
            if (direction == PageTurnDirection.Forward) spread.LeftContent = new Grid();
            else spread.RightContent = new Grid();
            var pending = snapshotter.DelayNext();
            var refresh = DrainAsync(spread);
            if (direction == PageTurnDirection.Forward) spread.NextLeftContent = null;
            else spread.PreviousRightContent = null;
            BeginSettlingTurn(spread, direction);
            TestAssert.Null(spread.Overlay.BackImage);
            TestAssert.Equal(mode, spread.Overlay.BacksideMode);
            pending.SetResult(MakeImage(4));
            TestAssert.TickUntilIdle(spread.Overlay.Controller);
            await refresh;
            if (direction == PageTurnDirection.Forward) spread.NextLeftContent = new Grid();
            else spread.PreviousRightContent = new Grid();
            await DrainAsync(spread);
            BeginSettlingTurn(spread, direction);
            TestAssert.NotNull(spread.Overlay.BackImage);
            TestAssert.Equal(PageBacksideMode.CustomContent, spread.Overlay.BacksideMode);
            TestAssert.TickUntilIdle(spread.Overlay.Controller);
        }
    }

    private static async Task Explicit_refresh_recaptures_in_place_changes()
    {
        var snapshotter = new DelayedSnapshotter();
        var spread = CreateSpread(snapshotter);
        spread.RightContent = new Label { Text = "before" };
        await spread.RefreshSnapshotAsync();
        ((Label)spread.RightContent).Text = "after";
        await spread.RefreshSnapshotAsync();
        TestAssert.Equal(8, snapshotter.Count);
        TestAssert.True(spread.CanTurnForward && spread.CanTurnBackward);
    }

    private static async Task Obsolete_capture_does_not_acknowledge_new_content()
    {
        foreach (var change in new[] { "replace", "resize", "remove" })
        {
            var snapshotter = new DelayedSnapshotter();
            var spread = CreateSpread(snapshotter);
            await spread.RefreshSnapshotAsync();
            spread.RightContent = new Grid { WidthRequest = 11 };
            var pending = snapshotter.DelayNext();
            var oldRefresh = DrainAsync(spread);
            if (change == "replace") spread.RightContent = new Grid { WidthRequest = 22 };
            else if (change == "remove") spread.RightContent = null;
            else
            {
                spread.RightContent!.WidthRequest = 22;
                typeof(PageSpreadView).GetMethod("OnSizeChanged", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(spread, [null, EventArgs.Empty]);
            }
            TestAssert.False(spread.CanTurnForward, "Obsolete content enabled forward navigation.");
            var oldImage = MakeImage(11);
            pending.SetResult(oldImage);
            await oldRefresh;
            TestAssert.Equal(IntPtr.Zero, oldImage.Handle);
            await DrainAsync(spread);
            if (change == "remove") TestAssert.False(spread.CanTurnForward);
            else
            {
                BeginSettlingTurn(spread, PageTurnDirection.Forward);
                TestAssert.Equal(22, spread.Overlay.FrontImage!.Width);
                TestAssert.TickUntilIdle(spread.Overlay.Controller);
            }
        }
    }

    private static async Task Active_turn_keeps_all_overlay_images_alive()
    {
        foreach (var direction in new[] { PageTurnDirection.Forward, PageTurnDirection.Backward })
        {
            var snapshotter = new DelayedSnapshotter();
            var spread = CreateSpread(snapshotter);
            spread.NextLeftContent = new Grid();
            spread.PreviousRightContent = new Grid();
            await spread.RefreshSnapshotAsync();
            // Invalidate only the opposite direction so a turn can start
            // while capture is suspended.
            if (direction == PageTurnDirection.Forward) spread.LeftContent = new Grid();
            else spread.RightContent = new Grid();
            var pending = snapshotter.DelayNext();
            var refresh = DrainAsync(spread);
            BeginSettlingTurn(spread, direction);
            var front = spread.Overlay.FrontImage!;
            var back = spread.Overlay.BackImage!;
            var under = (direction == PageTurnDirection.Forward
                ? spread.Overlay.UnderPageImage : spread.Overlay.PreviousPageImage)!;
            spread.LeftContent = new Grid();
            spread.RightContent = new Grid();
            spread.PreviousLeftContent = new Grid();
            spread.NextRightContent = new Grid();
            spread.PreviousRightContent = new Grid();
            spread.NextLeftContent = new Grid();
            pending.SetResult(MakeImage(4));
            await Task.Yield();
            TestAssert.False(refresh.IsCompleted);
            TestAssert.True(front.Handle != IntPtr.Zero, "The active front snapshot was disposed.");
            TestAssert.True(back.Handle != IntPtr.Zero, "The active back snapshot was disposed.");
            TestAssert.True(under.Handle != IntPtr.Zero, "The active destination snapshot was disposed.");
            TestAssert.Same(front, spread.Overlay.FrontImage!);
            TestAssert.TickUntilIdle(spread.Overlay.Controller);
            await refresh;
            TestAssert.Null(spread.Overlay.FrontImage);
            TestAssert.Equal(IntPtr.Zero, front.Handle);
            TestAssert.Equal(IntPtr.Zero, back.Handle);
            TestAssert.Equal(IntPtr.Zero, under.Handle);
            TestAssert.True(spread.CanTurnForward && spread.CanTurnBackward);
        }
    }

    private static PageSpreadView CreateSpread(IPageContentSnapshotter snapshotter) => new(snapshotter)
    {
        LeftContent = new Grid(), RightContent = new Grid(),
        PreviousLeftContent = new Grid(), NextRightContent = new Grid(),
    };

    private static Task DrainAsync(PageSpreadView spread) =>
        (Task)typeof(PageSpreadView).GetMethod("DrainSnapshotRefreshAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(spread, [CancellationToken.None])!;

    private static SKImage MakeImage(int width)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, 4));
        return surface.Snapshot();
    }

    private sealed class DelayedSnapshotter : IPageContentSnapshotter
    {
        private TaskCompletionSource<SKImage>? _next;
        public int Count { get; private set; }
        public TaskCompletionSource<SKImage> DelayNext() => _next = new();
        public Task<SKImage> CaptureAsync(View view, CancellationToken token)
        {
            Count++;
            if (_next is { } next) { _next = null; return next.Task; }
            return Task.FromResult(MakeImage(view.WidthRequest > 0 ? (int)view.WidthRequest : 4));
        }
    }

    private static async Task Refresh_captures_current_and_destination_pages()
    {
        var snapshotter = new CountingSnapshotter();
        var spread = new PageSpreadView(snapshotter)
        {
            LeftContent = new Grid(),
            RightContent = new Grid(),
            PreviousLeftContent = new Grid(),
            NextRightContent = new Grid(),
        };

        await spread.RefreshSnapshotAsync();

        TestAssert.Equal(4, snapshotter.CaptureCount);
        TestAssert.True(spread.CanTurnForward);
        TestAssert.True(spread.CanTurnBackward);
    }

    private static async Task Destination_content_and_snapshots_control_navigation_availability()
    {
        var spread = new PageSpreadView(new CountingSnapshotter())
        {
            LeftContent = new Grid(),
            RightContent = new Grid(),
        };

        TestAssert.False(spread.CanTurnForward);
        TestAssert.False(spread.CanTurnBackward);

        spread.NextRightContent = new Grid();
        spread.PreviousLeftContent = new Grid();

        TestAssert.False(spread.CanTurnForward);
        TestAssert.False(spread.CanTurnBackward);

        await spread.RefreshSnapshotAsync();

        TestAssert.True(spread.CanTurnForward);
        TestAssert.True(spread.CanTurnBackward);
    }

    private static async Task Replaced_content_blocks_turns_until_its_snapshot_is_current()
    {
        var spread = new PageSpreadView(new CountingSnapshotter())
        {
            LeftContent = new Grid(),
            RightContent = new Grid(),
            PreviousLeftContent = new Grid(),
            NextRightContent = new Grid(),
        };
        await spread.RefreshSnapshotAsync();

        spread.RightContent = new Grid();

        TestAssert.False(spread.CanTurnForward);
        TestAssert.True(spread.CanTurnBackward);

        await spread.RefreshSnapshotAsync();

        TestAssert.True(spread.CanTurnForward);
        TestAssert.True(spread.CanTurnBackward);
    }

    private static async Task Final_cancelled_capture_propagates_and_disposes_returned_image()
    {
        using var cancellation = new CancellationTokenSource();
        var snapshotter = new CancellingSnapshotter(cancellation);
        var spread = new PageSpreadView(snapshotter)
        {
            NextRightContent = new Grid(),
        };

        await TestAssert.ThrowsAsync<OperationCanceledException>(() =>
            spread.RefreshSnapshotAsync(cancellation.Token));

        // Caller cancellation stops waiting before the serialized capture
        // finishes. Check disposal only after that internal work has ended.
        var capture = (Task)typeof(PageSpreadView)
            .GetField("_refreshChain", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(spread)!;
        await TestAssert.ThrowsAsync<OperationCanceledException>(() => capture);

        TestAssert.NotNull(snapshotter.ReturnedImage);
        TestAssert.Equal(IntPtr.Zero, snapshotter.ReturnedImage!.Handle);
    }

    private static async Task Refresh_requested_during_a_turn_runs_after_completion()
    {
        var snapshotter = new CountingSnapshotter();
        var spread = await CreateReadySpreadAsync(snapshotter);
        BeginSettlingTurn(spread, PageTurnDirection.Forward);
        spread.NextRightContent = new Grid();

        var pendingRefresh = spread.RefreshSnapshotAsync();
        TestAssert.False(pendingRefresh.IsCompleted);
        TestAssert.Equal(4, snapshotter.CaptureCount);

        TestAssert.TickUntilIdle(spread.Overlay.Controller);

        await pendingRefresh;
        // Explicit refresh preserves in-place changes across an active turn.
        TestAssert.Equal(8, snapshotter.CaptureCount);
        TestAssert.True(spread.CanTurnForward);
    }

    private static async Task Failed_capture_keeps_the_slot_dirty_and_a_later_refresh_retries_only_it()
    {
        var snapshotter = new SequencedSnapshotter();
        snapshotter.EnqueueImages(4);
        var spread = new PageSpreadView(snapshotter)
        {
            LeftContent = new Grid(),
            RightContent = new Grid(),
            PreviousLeftContent = new Grid(),
            NextRightContent = new Grid(),
        };
        await spread.RefreshSnapshotAsync();

        spread.RightContent = new Grid();
        var staleFailure = snapshotter.EnqueueDelayedFailure();
        var failedRefresh = DrainAsync(spread);
        await staleFailure.Started;
        staleFailure.Fail(new InvalidOperationException("capture failed"));

        await failedRefresh;
        TestAssert.False(spread.CanTurnForward);
        TestAssert.True(spread.CanTurnBackward);

        snapshotter.EnqueueImage();
        await DrainAsync(spread);

        TestAssert.True(spread.CanTurnForward);
        TestAssert.True(spread.CanTurnBackward);
        TestAssert.Equal(6, snapshotter.CaptureCount);
    }

    private static async Task Refresh_captures_only_slots_with_changed_content()
    {
        var snapshotter = new CountingSnapshotter();
        var spread = new PageSpreadView(snapshotter)
        {
            LeftContent = new Grid(),
            RightContent = new Grid(),
            PreviousLeftContent = new Grid(),
            NextRightContent = new Grid(),
            PreviousRightContent = new Grid(),
            NextLeftContent = new Grid(),
        };
        await spread.RefreshSnapshotAsync();
        TestAssert.Equal(6, snapshotter.CaptureCount);

        spread.RightContent = new Grid();
        await Task.WhenAll(DrainAsync(spread), DrainAsync(spread), DrainAsync(spread));

        TestAssert.Equal(7, snapshotter.CaptureCount);
        TestAssert.True(spread.CanTurnForward);
        TestAssert.True(spread.CanTurnBackward);
    }

    private static async Task Forward_turn_shows_the_adjacent_page_on_the_sheet_back()
    {
        var snapshotter = new DistinctSizeSnapshotter();
        var spread = new PageSpreadView(snapshotter)
        {
            LeftContent = new Grid(),
            RightContent = new Grid(),
            PreviousLeftContent = new Grid(),
            NextRightContent = new Grid(),
            PreviousRightContent = new Grid(),
            NextLeftContent = new Grid(),
        };
        await spread.RefreshSnapshotAsync();

        BeginSettlingTurn(spread, PageTurnDirection.Forward);

        // Capture order: left, right, previous-left, next-right,
        // previous-right, next-left. A forward turn curls the right page,
        // reveals the next right page underneath, and lands with the next
        // left page on the sheet's back.
        TestAssert.Equal(6, spread.Overlay.FrontImage!.Width);
        TestAssert.Equal(8, spread.Overlay.UnderPageImage!.Width);
        TestAssert.Equal(10, spread.Overlay.BackImage!.Width);
        TestAssert.Equal(PageBacksideMode.CustomContent, spread.Overlay.BacksideMode);

        TestAssert.TickUntilIdle(spread.Overlay.Controller);

        TestAssert.Null(spread.Overlay.BackImage);
        TestAssert.Equal(spread.BacksideMode, spread.Overlay.BacksideMode);
    }

    private static async Task Backward_turn_shows_the_adjacent_page_on_the_sheet_back()
    {
        var snapshotter = new DistinctSizeSnapshotter();
        var spread = new PageSpreadView(snapshotter)
        {
            LeftContent = new Grid(),
            RightContent = new Grid(),
            PreviousLeftContent = new Grid(),
            NextRightContent = new Grid(),
            PreviousRightContent = new Grid(),
            NextLeftContent = new Grid(),
        };
        await spread.RefreshSnapshotAsync();

        BeginSettlingTurn(spread, PageTurnDirection.Backward);

        // A backward turn curls the left page, reveals the previous left
        // page underneath, and lands with the previous right page on the
        // sheet's back.
        TestAssert.Equal(5, spread.Overlay.FrontImage!.Width);
        TestAssert.Equal(7, spread.Overlay.PreviousPageImage!.Width);
        TestAssert.Equal(9, spread.Overlay.BackImage!.Width);

        TestAssert.TickUntilIdle(spread.Overlay.Controller);

        TestAssert.Null(spread.Overlay.BackImage);
    }

    private static void Overlay_surface_preserves_the_stationary_page()
    {
        var spread = new PageSpreadView(new CountingSnapshotter());
        TestAssert.Equal(0f, spread.Overlay.SurfaceColor.Alpha);
    }

    private static void Handler_teardown_restores_both_static_pages()
    {
        var spread = new PageSpreadView(new CountingSnapshotter());
        var spreadFrame = (Grid)spread.Children[0];
        // Each cell stacks its destination hosts below the live page host.
        var leftHost = (Grid)((Grid)spreadFrame.Children[0]).Children[2];
        var rightHost = (Grid)((Grid)spreadFrame.Children[1]).Children[2];
        leftHost.IsVisible = false;
        rightHost.IsVisible = false;
        spread.Overlay.Opacity = 1;

        InvokeHandlerChanged(spread);

        TestAssert.True(leftHost.IsVisible);
        TestAssert.True(rightHost.IsVisible);
        TestAssert.Equal(0d, spread.Overlay.Opacity);
    }

    private static async Task<PageSpreadView> CreateReadySpreadAsync(
        CountingSnapshotter snapshotter)
    {
        var spread = new PageSpreadView(snapshotter)
        {
            LeftContent = new Grid(),
            RightContent = new Grid(),
            PreviousLeftContent = new Grid(),
            NextRightContent = new Grid(),
        };
        await spread.RefreshSnapshotAsync();
        return spread;
    }

    private static void InvokeHandlerChanged(PageSpreadView view) =>
        typeof(PageSpreadView)
            .GetMethod("OnHandlerChanged", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(view, null);

    private static void BeginSettlingTurn(PageSpreadView spread, PageTurnDirection direction)
    {
        var prepared = (bool)typeof(PageSpreadView)
            .GetMethod("PrepareTurn", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(spread, [direction])!;
        TestAssert.True(prepared);
        TestAssert.True(spread.Overlay.Controller.BeginProgrammaticTurn(
            new Vector2(400, 500),
            direction));
    }

    private sealed class CountingSnapshotter : IPageContentSnapshotter
    {
        public int CaptureCount { get; private set; }

        public Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken)
        {
            CaptureCount++;
            using var surface = SKSurface.Create(new SKImageInfo(4, 4));
            return Task.FromResult(surface.Snapshot());
        }
    }

    /// <summary>Returns images whose width encodes the capture ordinal.</summary>
    private sealed class DistinctSizeSnapshotter : IPageContentSnapshotter
    {
        private int _captureCount;

        public Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken)
        {
            var width = 4 + ++_captureCount;
            using var surface = SKSurface.Create(new SKImageInfo(width, 4));
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

    private sealed class SequencedSnapshotter : IPageContentSnapshotter
    {
        private readonly Queue<Func<CancellationToken, Task<SKImage>>> _captures = new();

        public int CaptureCount { get; private set; }

        public void EnqueueImages(int count)
        {
            for (var index = 0; index < count; index++)
            {
                EnqueueImage();
            }
        }

        public void EnqueueImage() => _captures.Enqueue(_ => Task.FromResult(CreateImage()));

        public DelayedFailure EnqueueDelayedFailure()
        {
            var failure = new DelayedFailure();
            _captures.Enqueue(failure.CaptureAsync);
            return failure;
        }

        public Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken)
        {
            CaptureCount++;
            return _captures.Dequeue()(cancellationToken);
        }

        private static SKImage CreateImage()
        {
            using var surface = SKSurface.Create(new SKImageInfo(4, 4));
            return surface.Snapshot();
        }
    }

    private sealed class DelayedFailure
    {
        private readonly TaskCompletionSource _started = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<SKImage> _completion = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;

        public Task<SKImage> CaptureAsync(CancellationToken cancellationToken)
        {
            _started.SetResult();
            return _completion.Task;
        }

        public void Fail(Exception exception) => _completion.SetException(exception);
    }
}
