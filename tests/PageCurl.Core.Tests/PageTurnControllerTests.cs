using System.Numerics;
using PageCurl.Core;

namespace PageCurl.Core.Tests;

internal static class PageTurnControllerTests
{
    private static readonly Vector2 PageSize = new(100, 200);

    public static void RunAll()
    {
        BeginDrag_chooses_edge_and_clamps_drag_start_to_page();
        UpdateDrag_maps_finger_position_to_normalized_progress(PageTurnDirection.Forward);
        UpdateDrag_maps_finger_position_to_normalized_progress(PageTurnDirection.Backward);
        Release_before_threshold_settles_back_and_reports_cancelled_turn();
        Release_after_threshold_settles_forward_and_reports_committed_turn();
        Cancel_always_returns_to_the_start_endpoint();
        Settling_progress_moves_the_curl_pose();
        Invalid_page_size_is_rejected_without_entering_drag();
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

    private static void Invalid_page_size_is_rejected_without_entering_drag()
    {
        var controller = new PageTurnController();

        TestAssert.False(controller.BeginDrag(new Vector2(10, 10), Vector2.Zero));
        TestAssert.Equal(PageTurnState.Idle, controller.Snapshot.State);
    }

    private static void RunUntilIdle(PageTurnController controller)
    {
        for (var i = 0; i < 240 && controller.Snapshot.State != PageTurnState.Idle; i++)
        {
            controller.Tick(1.0 / 60.0);
        }

        TestAssert.Equal(PageTurnState.Idle, controller.Snapshot.State);
    }
}
