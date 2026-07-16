using System.Numerics;
using PageCurl.Core;

namespace PageCurl.Core.Tests;

internal static class PageTurnPerformanceTests
{
    private static readonly Vector2 PageSize = new(1200, 1600);

    public static void RunAll()
    {
        Geometry_build_reuses_storage_without_managed_allocations();
        Controller_drag_and_settle_reuses_storage_without_managed_allocations();
    }

    private static void Geometry_build_reuses_storage_without_managed_allocations()
    {
        var options = new PageCurlOptions();
        var buffer = new PageMeshBuffer(options);
        var pose = PageCurlGeometry.ComputePose(
            PageSize,
            new Vector2(PageSize.X, PageSize.Y * 0.5f),
            new Vector2(PageSize.X * 0.2f, PageSize.Y * 0.5f),
            PageTurnDirection.Forward,
            0.8f,
            options);

        for (var i = 0; i < 100; i++)
        {
            PageCurlGeometry.Build(buffer, PageSize, pose, options);
        }

        CollectBeforeMeasurement();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            PageCurlGeometry.Build(buffer, PageSize, pose, options);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        TestAssert.True(allocated == 0, $"Geometry loop allocated {allocated} bytes.");
    }

    private static void Controller_drag_and_settle_reuses_storage_without_managed_allocations()
    {
        var controller = new PageTurnController();
        RunControllerTurn(controller);
        CollectBeforeMeasurement();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            RunControllerTurn(controller);
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        TestAssert.True(allocated == 0, $"Controller loop allocated {allocated} bytes.");
    }

    private static void RunControllerTurn(PageTurnController controller)
    {
        TestAssert.True(controller.BeginDrag(new Vector2(PageSize.X * 0.9f, PageSize.Y * 0.5f), PageSize));
        controller.UpdateDrag(new Vector2(PageSize.X * 0.2f, PageSize.Y * 0.5f));
        controller.EndDrag();
        for (var i = 0; i < 240 && controller.Snapshot.State != PageTurnState.Idle; i++)
        {
            controller.Tick(1.0 / 60.0);
        }

        TestAssert.Equal(PageTurnState.Idle, controller.Snapshot.State);
    }

    private static void CollectBeforeMeasurement()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
