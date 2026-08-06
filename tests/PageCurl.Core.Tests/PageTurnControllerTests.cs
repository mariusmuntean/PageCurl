using System.Numerics;
using PageCurl.Core;

namespace PageCurl.Core.Tests;

internal static class PageTurnControllerTests
{
    private static readonly Vector2 PageSize = new(100, 200);
    private static readonly PageEdge[] PhysicalEdges =
    {
        PageEdge.Top,
        PageEdge.Right,
        PageEdge.Bottom,
        PageEdge.Left,
    };

    public static void RunAll()
    {
        BeginDrag_chooses_edge_and_clamps_drag_start_to_page();
        BeginDrag_supports_all_page_corners();
        Locked_edge_and_endpoints_do_not_start_a_drag();
        Unlocked_edges_still_start_a_drag();
        UpdateDrag_maps_finger_position_to_normalized_progress(PageTurnDirection.Forward);
        UpdateDrag_maps_finger_position_to_normalized_progress(PageTurnDirection.Backward);
        Release_before_threshold_settles_back_and_reports_cancelled_turn();
        Release_after_threshold_settles_forward_and_reports_committed_turn();
        Release_without_animation_completes_immediately();
        Programmatic_forward_turn_starts_from_the_forward_edge();
        Programmatic_backward_turn_starts_from_the_backward_edge();
        Programmatic_turn_rejects_invalid_sizes_and_busy_controller();
        Cancel_always_returns_to_the_start_endpoint();
        Settling_progress_moves_the_curl_pose();
        Corner_release_preserves_curl_direction_while_settling_back();
        Invalid_page_size_is_rejected_without_entering_drag();
        Flick_release_commits_below_the_threshold();
        Flick_release_cancels_above_the_threshold();
        Non_monotonic_timestamps_do_not_create_release_velocity();
        Invalid_timestamp_rebases_before_the_next_valid_sample();
        Release_velocity_is_carried_into_forward_settling();
        Release_velocity_is_carried_into_backward_settling();
        Committed_flick_retains_visible_settle_motion();
        Timestamped_drag_remains_directly_attached_to_the_finger();
    }

    private static void BeginDrag_chooses_edge_and_clamps_drag_start_to_page()
    {
        var controller = new PageTurnController();

        TestAssert.True(controller.BeginDrag(new Vector2(90, -20), PageSize));

        var snapshot = controller.Snapshot;
        TestAssert.Equal(PageTurnState.Dragging, snapshot.State);
        TestAssert.Equal(PageTurnDirection.Forward, snapshot.Direction);
        TestAssert.Equal(new Vector2(100, 0), snapshot.DragStartPosition);

        TestAssert.True(controller.BeginDrag(new Vector2(10, 240), PageSize));
        snapshot = controller.Snapshot;
        TestAssert.Equal(PageTurnDirection.Backward, snapshot.Direction);
        TestAssert.Equal(new Vector2(0, 200), snapshot.DragStartPosition);
    }

    private static void BeginDrag_supports_all_page_corners()
    {
        var cases = new[]
        {
            (Pointer: new Vector2(0, 0), Direction: PageTurnDirection.Backward, Start: new Vector2(0, 0)),
            (Pointer: new Vector2(0, PageSize.Y), Direction: PageTurnDirection.Backward, Start: new Vector2(0, PageSize.Y)),
            (Pointer: new Vector2(PageSize.X, 0), Direction: PageTurnDirection.Forward, Start: new Vector2(PageSize.X, 0)),
            (Pointer: new Vector2(PageSize.X, PageSize.Y), Direction: PageTurnDirection.Forward, Start: new Vector2(PageSize.X, PageSize.Y)),
        };

        foreach (var testCase in cases)
        {
            var controller = new PageTurnController();

            TestAssert.True(controller.BeginDrag(testCase.Pointer, PageSize));
            TestAssert.Equal(testCase.Direction, controller.Snapshot.Direction);
            TestAssert.Equal(testCase.Start, controller.Snapshot.DragStartPosition);
        }
    }

    private static void Locked_edge_and_endpoints_do_not_start_a_drag()
    {
        foreach (var lockedEdge in PhysicalEdges)
        {
            foreach (var point in PointsOnEdge(lockedEdge))
            {
                var controller = new PageTurnController();

                TestAssert.False(controller.BeginDrag(point, PageSize, lockedEdge));
                TestAssert.Equal(PageTurnState.Idle, controller.Snapshot.State);
            }
        }
    }

    private static void Unlocked_edges_still_start_a_drag()
    {
        foreach (var lockedEdge in PhysicalEdges)
        {
            foreach (var freeEdge in PhysicalEdges)
            {
                if (freeEdge == lockedEdge)
                {
                    continue;
                }

                var controller = new PageTurnController();

                TestAssert.True(controller.BeginDrag(EdgeCenter(freeEdge), PageSize, lockedEdge));
                TestAssert.Equal(freeEdge, controller.Snapshot.FreeEdge);
                TestAssert.Equal(lockedEdge, controller.Snapshot.HingeEdge);
            }
        }
    }

    private static void UpdateDrag_maps_finger_position_to_normalized_progress(PageTurnDirection direction)
    {
        var controller = new PageTurnController();
        var start = direction == PageTurnDirection.Forward
            ? new Vector2(90, 100)
            : new Vector2(10, 100);

        TestAssert.True(controller.BeginDrag(start, PageSize));
        controller.UpdateDrag(new Vector2(50, 100), 0.1);

        TestAssert.Equal(0.5f, controller.Snapshot.Progress, 3);
        TestAssert.Equal(new Vector2(50, 100), controller.Snapshot.PointerPosition);
    }

    private static void Release_before_threshold_settles_back_and_reports_cancelled_turn()
    {
        var controller = new PageTurnController();
        PageTurnCompletedEventArgs? completed = null;
        controller.PageTurnCompleted += (_, args) => completed = args;

        controller.BeginDrag(new Vector2(90, 100), PageSize);
        controller.UpdateDrag(new Vector2(65, 100), 0.1);
        controller.EndDrag();

        TestAssert.Equal(PageTurnState.Settling, controller.Snapshot.State);
        TestAssert.Equal(0f, controller.Snapshot.TargetProgress);

        RunUntilIdle(controller);

        TestAssert.NotNull(completed);
        TestAssert.False(completed!.Committed);
        TestAssert.Equal(PageTurnState.Idle, controller.Snapshot.State);
        TestAssert.Equal(0f, controller.Snapshot.Progress, 3);
    }

    private static void Release_after_threshold_settles_forward_and_reports_committed_turn()
    {
        var controller = new PageTurnController();
        PageTurnCompletedEventArgs? completed = null;
        controller.PageTurnCompleted += (_, args) => completed = args;

        controller.BeginDrag(new Vector2(90, 100), PageSize);
        controller.UpdateDrag(new Vector2(25, 100), 0.1);
        controller.EndDrag();

        TestAssert.Equal(1f, controller.Snapshot.TargetProgress);
        RunUntilIdle(controller);

        TestAssert.NotNull(completed);
        TestAssert.True(completed!.Committed);
        TestAssert.Equal(1f, controller.Snapshot.Progress, 3);
        TestAssert.Equal(PageTurnState.Idle, controller.Snapshot.State);
    }

    private static void Release_without_animation_completes_immediately()
    {
        var controller = new PageTurnController();
        PageTurnCompletedEventArgs? completed = null;
        var completionCount = 0;
        controller.PageTurnCompleted += (_, args) =>
        {
            completionCount++;
            completed = args;
        };

        controller.BeginDrag(new Vector2(90, 100), PageSize);
        controller.UpdateDrag(new Vector2(25, 100));
        controller.EndDrag(animate: false);

        TestAssert.Equal(PageTurnState.Idle, controller.Snapshot.State);
        TestAssert.Equal(1f, controller.Snapshot.Progress);
        TestAssert.NotNull(completed);
        TestAssert.True(completed!.Committed);
        TestAssert.Equal(1, completionCount);
    }

    private static void Programmatic_forward_turn_starts_from_the_forward_edge()
    {
        var controller = new PageTurnController();
        PageTurnCompletedEventArgs? completed = null;
        var completionCount = 0;
        controller.PageTurnCompleted += (_, args) =>
        {
            completionCount++;
            completed = args;
        };

        TestAssert.True(controller.BeginProgrammaticTurn(PageSize, PageTurnDirection.Forward));

        var snapshot = controller.Snapshot;
        TestAssert.Equal(PageTurnState.Settling, snapshot.State);
        TestAssert.Equal(PageTurnDirection.Forward, snapshot.Direction);
        TestAssert.Equal(0f, snapshot.Progress);
        TestAssert.Equal(1f, snapshot.TargetProgress);
        RunUntilIdle(controller);

        TestAssert.NotNull(completed);
        TestAssert.True(completed!.Committed);
        TestAssert.Equal(1, completionCount);
    }

    private static void Programmatic_backward_turn_starts_from_the_backward_edge()
    {
        var controller = new PageTurnController();
        PageTurnCompletedEventArgs? completed = null;
        var completionCount = 0;
        controller.PageTurnCompleted += (_, args) =>
        {
            completionCount++;
            completed = args;
        };

        TestAssert.True(controller.BeginProgrammaticTurn(PageSize, PageTurnDirection.Backward));

        var snapshot = controller.Snapshot;
        TestAssert.Equal(PageTurnState.Settling, snapshot.State);
        TestAssert.Equal(PageTurnDirection.Backward, snapshot.Direction);
        TestAssert.Equal(new Vector2(0, 100), snapshot.DragStartPosition);
        TestAssert.Equal(1f, snapshot.TargetProgress);
        RunUntilIdle(controller);

        TestAssert.NotNull(completed);
        TestAssert.True(completed!.Committed);
        TestAssert.Equal(1, completionCount);
    }

    private static void Programmatic_turn_rejects_invalid_sizes_and_busy_controller()
    {
        var controller = new PageTurnController();

        TestAssert.False(controller.BeginProgrammaticTurn(Vector2.Zero, PageTurnDirection.Forward));
        TestAssert.True(controller.BeginDrag(new Vector2(90, 100), PageSize));
        TestAssert.False(controller.BeginProgrammaticTurn(PageSize, PageTurnDirection.Forward, animate: false));
    }

    private static void Cancel_always_returns_to_the_start_endpoint()
    {
        var controller = new PageTurnController();

        controller.BeginDrag(new Vector2(90, 100), PageSize);
        controller.UpdateDrag(new Vector2(10, 100), 0.1);
        controller.Cancel();
        RunUntilIdle(controller);

        TestAssert.Equal(0f, controller.Snapshot.Progress, 3);
        TestAssert.Equal(PageTurnState.Idle, controller.Snapshot.State);
    }

    private static void Settling_progress_moves_the_curl_pose()
    {
        var controller = new PageTurnController();
        controller.BeginDrag(new Vector2(90, 100), PageSize);
        controller.UpdateDrag(new Vector2(20, 100), 0.1);
        controller.EndDrag();

        var before = controller.GetCurlPose(PageSize);
        TestAssert.True(controller.Tick(1.0 / 60.0));
        var after = controller.GetCurlPose(PageSize);

        TestAssert.True(after.Progress > before.Progress);
        TestAssert.True(Vector2.Distance(after.Position, before.Position) > 0.01f);
    }

    private static void Corner_release_preserves_curl_direction_while_settling_back()
    {
        var controller = new PageTurnController();
        controller.BeginDrag(new Vector2(PageSize.X, 0), PageSize);
        controller.UpdateDrag(new Vector2(70, 30), 0.1);
        controller.EndDrag();

        var released = controller.GetCurlPose(PageSize);
        for (var frame = 0; frame < 120 && controller.Snapshot.Progress > 0.05f; frame++)
        {
            TestAssert.True(controller.Tick(1.0 / 60.0));
        }

        var settling = controller.GetCurlPose(PageSize);

        TestAssert.True(
            Vector2.Dot(released.Direction, settling.Direction) > 0.999f,
            $"Curl direction rotated from {released.Direction} to {settling.Direction} during rollback.");
    }

    private static void Invalid_page_size_is_rejected_without_entering_drag()
    {
        var controller = new PageTurnController();

        TestAssert.False(controller.BeginDrag(new Vector2(10, 10), Vector2.Zero));
        TestAssert.Equal(PageTurnState.Idle, controller.Snapshot.State);
    }

    private static void Flick_release_commits_below_the_threshold()
    {
        var controller = new PageTurnController();
        controller.BeginDrag(new Vector2(90, 100), PageSize);
        controller.UpdateDrag(new Vector2(70, 100), 1.000);
        controller.UpdateDrag(new Vector2(58, 100), 1.016);
        controller.EndDrag();

        TestAssert.True(controller.Snapshot.Progress < 0.5f);
        TestAssert.Equal(1f, controller.Snapshot.TargetProgress);
    }

    private static void Flick_release_cancels_above_the_threshold()
    {
        var controller = new PageTurnController();
        controller.BeginDrag(new Vector2(90, 100), PageSize);
        controller.UpdateDrag(new Vector2(42, 100), 1.000);
        controller.UpdateDrag(new Vector2(49, 100), 1.016);
        controller.EndDrag();

        TestAssert.True(controller.Snapshot.Progress > 0.5f);
        TestAssert.Equal(0f, controller.Snapshot.TargetProgress);
    }

    private static void Non_monotonic_timestamps_do_not_create_release_velocity()
    {
        var controller = new PageTurnController();
        controller.BeginDrag(new Vector2(90, 100), PageSize);
        controller.UpdateDrag(new Vector2(60, 100), 1.000);
        controller.UpdateDrag(new Vector2(30, 100), 0.500);
        controller.EndDrag();

        TestAssert.Equal(1f, controller.Snapshot.TargetProgress);
    }

    private static void Invalid_timestamp_rebases_before_the_next_valid_sample()
    {
        var controller = new PageTurnController();
        controller.BeginDrag(new Vector2(90, 100), PageSize);
        controller.UpdateDrag(new Vector2(40, 100), 1.000);
        controller.UpdateDrag(new Vector2(42, 100), 1.016);
        controller.UpdateDrag(new Vector2(49, 100), 1.008);
        controller.UpdateDrag(new Vector2(50, 100), 1.032);
        controller.EndDrag();

        TestAssert.Equal(1f, controller.Snapshot.TargetProgress);
    }

    private static void Release_velocity_is_carried_into_forward_settling()
    {
        var slow = new PageTurnController();
        slow.BeginDrag(new Vector2(90, 100), PageSize);
        slow.UpdateDrag(new Vector2(70, 100), 1.000);
        slow.UpdateDrag(new Vector2(40, 100), 1.500);
        slow.EndDrag();

        var fast = new PageTurnController();
        fast.BeginDrag(new Vector2(90, 100), PageSize);
        fast.UpdateDrag(new Vector2(70, 100), 1.000);
        fast.UpdateDrag(new Vector2(40, 100), 1.016);
        fast.EndDrag();

        slow.Tick(1.0 / 60.0);
        fast.Tick(1.0 / 60.0);

        TestAssert.True(fast.Snapshot.Progress > slow.Snapshot.Progress);
    }

    private static void Release_velocity_is_carried_into_backward_settling()
    {
        var slow = new PageTurnController();
        slow.BeginDrag(new Vector2(90, 100), PageSize);
        slow.UpdateDrag(new Vector2(40, 100), 1.000);
        slow.UpdateDrag(new Vector2(60, 100), 1.500);
        slow.EndDrag();

        var fast = new PageTurnController();
        fast.BeginDrag(new Vector2(90, 100), PageSize);
        fast.UpdateDrag(new Vector2(40, 100), 1.000);
        fast.UpdateDrag(new Vector2(60, 100), 1.016);
        fast.EndDrag();

        slow.Tick(1.0 / 60.0);
        fast.Tick(1.0 / 60.0);

        TestAssert.True(fast.Snapshot.Progress < slow.Snapshot.Progress);
    }

    private static void Committed_flick_retains_visible_settle_motion()
    {
        var controller = new PageTurnController();
        controller.BeginDrag(new Vector2(90, 100), PageSize);
        controller.UpdateDrag(new Vector2(70, 100), 1.000);
        controller.UpdateDrag(new Vector2(10, 100), 1.016);
        controller.EndDrag();

        for (var frame = 0; frame < 12; frame++)
        {
            TestAssert.True(controller.Tick(1.0 / 60.0));
        }

        TestAssert.Equal(PageTurnState.Settling, controller.Snapshot.State);
        TestAssert.True(controller.Snapshot.Progress < 0.999f);
    }

    private static void Timestamped_drag_remains_directly_attached_to_the_finger()
    {
        var controller = new PageTurnController();
        controller.BeginDrag(new Vector2(90, 100), PageSize);
        controller.UpdateDrag(new Vector2(63, 100), 1.000);

        TestAssert.Equal(new Vector2(63, 100), controller.Snapshot.PointerPosition);
        TestAssert.Equal(0.37f, controller.Snapshot.Progress, 3);
    }

    private static void RunUntilIdle(PageTurnController controller)
    {
        for (var i = 0; i < 240 && controller.Snapshot.State != PageTurnState.Idle; i++)
        {
            controller.Tick(1.0 / 60.0);
        }

        TestAssert.Equal(PageTurnState.Idle, controller.Snapshot.State);
    }

    private static Vector2[] PointsOnEdge(PageEdge edge) => edge switch
    {
        PageEdge.Top => [new(0, 0), new(PageSize.X * 0.5f, 0), new(PageSize.X, 0)],
        PageEdge.Right => [new(PageSize.X, 0), new(PageSize.X, PageSize.Y * 0.5f), PageSize],
        PageEdge.Bottom => [new(0, PageSize.Y), new(PageSize.X * 0.5f, PageSize.Y), PageSize],
        PageEdge.Left => [new(0, 0), new(0, PageSize.Y * 0.5f), new(0, PageSize.Y)],
        _ => [],
    };

    private static Vector2 EdgeCenter(PageEdge edge) => edge switch
    {
        PageEdge.Top => new(PageSize.X * 0.5f, 0),
        PageEdge.Right => new(PageSize.X, PageSize.Y * 0.5f),
        PageEdge.Bottom => new(PageSize.X * 0.5f, PageSize.Y),
        PageEdge.Left => new(0, PageSize.Y * 0.5f),
        _ => PageSize * 0.5f,
    };
}
