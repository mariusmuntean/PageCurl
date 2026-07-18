using System.Numerics;
using PageCurl.Core;

namespace PageCurl.Core.Tests;

internal static class PageTurnPerformanceTests
{
    private static readonly Vector2 PageSize = new(1200, 1600);
    private static readonly PageEdge[] PhysicalEdges =
    {
        PageEdge.Top,
        PageEdge.Right,
        PageEdge.Bottom,
        PageEdge.Left,
    };

    public static void RunAll()
    {
        Geometry_build_reuses_storage_without_managed_allocations();
        Controller_drag_and_settle_reuses_storage_without_managed_allocations();
    }

    private static void Geometry_build_reuses_storage_without_managed_allocations()
    {
        var options = new PageCurlOptions();
        var buffer = new PageMeshBuffer(options);
        foreach (var freeEdge in PhysicalEdges)
        {
            var pose = PageCurlGeometry.ComputePose(
                PageSize,
                DragStartFor(freeEdge),
                PointerFor(freeEdge),
                Opposite(freeEdge),
                freeEdge,
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
            TestAssert.True(allocated == 0, $"{freeEdge} geometry loop allocated {allocated} bytes.");
        }
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

    private static Vector2 DragStartFor(PageEdge freeEdge) => freeEdge switch
    {
        PageEdge.Top => new(PageSize.X * 0.5f, 0),
        PageEdge.Right => new(PageSize.X, PageSize.Y * 0.5f),
        PageEdge.Bottom => new(PageSize.X * 0.5f, PageSize.Y),
        PageEdge.Left => new(0, PageSize.Y * 0.5f),
        _ => Vector2.Zero,
    };

    private static Vector2 PointerFor(PageEdge freeEdge) => freeEdge switch
    {
        PageEdge.Top => new(PageSize.X * 0.5f, PageSize.Y * 0.2f),
        PageEdge.Right => new(PageSize.X * 0.2f, PageSize.Y * 0.5f),
        PageEdge.Bottom => new(PageSize.X * 0.5f, PageSize.Y * 0.8f),
        PageEdge.Left => new(PageSize.X * 0.8f, PageSize.Y * 0.5f),
        _ => Vector2.Zero,
    };

    private static PageEdge Opposite(PageEdge edge) => edge switch
    {
        PageEdge.Top => PageEdge.Bottom,
        PageEdge.Right => PageEdge.Left,
        PageEdge.Bottom => PageEdge.Top,
        PageEdge.Left => PageEdge.Right,
        _ => PageEdge.Auto,
    };
}
