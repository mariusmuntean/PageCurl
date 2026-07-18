using System.Numerics;
using PageCurl.Core;

namespace PageCurl.Core.Tests;

internal static class PageEdgeTests
{
    public static void RunAll()
    {
        PageEdge_contract_exists();
        Controller_exposes_edge_aware_begin_drag();
        Snapshot_exposes_selected_hinge_edge();
        Auto_selects_the_nearest_edge_and_opposite_hinge();
        Auto_resolves_corner_ties_from_the_first_drag_vector();
        Explicit_lock_accepts_every_other_edge();
        Explicit_lock_rejects_a_start_on_the_locked_edge();
        Edge_progress_uses_the_selected_free_edge_normal();
        Every_corner_can_start_an_edge_aware_drag();
    }

    private static void PageEdge_contract_exists()
    {
        var edgeType = typeof(PageTurnController).Assembly.GetType("PageCurl.Core.PageEdge");

        TestAssert.NotNull(edgeType);
        TestAssert.True(edgeType!.IsEnum);
        TestAssert.NotNull(Enum.GetNames(edgeType).FirstOrDefault(name => name == "Auto"));
        TestAssert.NotNull(Enum.GetNames(edgeType).FirstOrDefault(name => name == "Top"));
        TestAssert.NotNull(Enum.GetNames(edgeType).FirstOrDefault(name => name == "Right"));
        TestAssert.NotNull(Enum.GetNames(edgeType).FirstOrDefault(name => name == "Bottom"));
        TestAssert.NotNull(Enum.GetNames(edgeType).FirstOrDefault(name => name == "Left"));
    }

    private static void Controller_exposes_edge_aware_begin_drag()
    {
        var edgeType = typeof(PageTurnController).Assembly.GetType("PageCurl.Core.PageEdge")!;
        var method = typeof(PageTurnController).GetMethod(
            nameof(PageTurnController.BeginDrag),
            new[] { typeof(Vector2), typeof(Vector2), edgeType });

        TestAssert.NotNull(method);
    }

    private static void Snapshot_exposes_selected_hinge_edge()
    {
        var property = typeof(PageTurnSnapshot).GetProperty("HingeEdge");

        TestAssert.NotNull(property);
    }

    private static void Auto_selects_the_nearest_edge_and_opposite_hinge()
    {
        var cases = new[]
        {
            (Pointer: new Vector2(50, 0), Free: PageEdge.Top, Hinge: PageEdge.Bottom),
            (Pointer: new Vector2(100, 100), Free: PageEdge.Right, Hinge: PageEdge.Left),
            (Pointer: new Vector2(50, 200), Free: PageEdge.Bottom, Hinge: PageEdge.Top),
            (Pointer: new Vector2(0, 100), Free: PageEdge.Left, Hinge: PageEdge.Right),
        };

        foreach (var testCase in cases)
        {
            var controller = new PageTurnController();

            TestAssert.True(controller.BeginDrag(testCase.Pointer, new Vector2(100, 200), PageEdge.Auto));
            TestAssert.Equal(testCase.Free, controller.Snapshot.FreeEdge);
            TestAssert.Equal(testCase.Hinge, controller.Snapshot.HingeEdge);
        }
    }

    private static void Auto_resolves_corner_ties_from_the_first_drag_vector()
    {
        var pageSize = new Vector2(100, 200);
        var horizontal = new PageTurnController();
        var vertical = new PageTurnController();

        TestAssert.True(horizontal.BeginDrag(Vector2.Zero, pageSize, PageEdge.Auto));
        TestAssert.True(vertical.BeginDrag(Vector2.Zero, pageSize, PageEdge.Auto));
        horizontal.UpdateDrag(new Vector2(35, 5));
        vertical.UpdateDrag(new Vector2(5, 35));

        TestAssert.Equal(PageEdge.Left, horizontal.Snapshot.FreeEdge);
        TestAssert.Equal(PageEdge.Right, horizontal.Snapshot.HingeEdge);
        TestAssert.Equal(PageEdge.Top, vertical.Snapshot.FreeEdge);
        TestAssert.Equal(PageEdge.Bottom, vertical.Snapshot.HingeEdge);
    }

    private static void Explicit_lock_accepts_every_other_edge()
    {
        var pageSize = new Vector2(100, 200);
        var cases = new[]
        {
            (Locked: PageEdge.Left, Pointer: new Vector2(50, 0), Free: PageEdge.Top),
            (Locked: PageEdge.Top, Pointer: new Vector2(100, 100), Free: PageEdge.Right),
            (Locked: PageEdge.Right, Pointer: new Vector2(50, 200), Free: PageEdge.Bottom),
            (Locked: PageEdge.Bottom, Pointer: new Vector2(0, 100), Free: PageEdge.Left),
        };

        foreach (var testCase in cases)
        {
            var controller = new PageTurnController();

            TestAssert.True(controller.BeginDrag(testCase.Pointer, pageSize, testCase.Locked));
            TestAssert.Equal(testCase.Locked, controller.Snapshot.HingeEdge);
            TestAssert.Equal(testCase.Free, controller.Snapshot.FreeEdge);
        }
    }

    private static void Explicit_lock_rejects_a_start_on_the_locked_edge()
    {
        var controller = new PageTurnController();

        TestAssert.False(controller.BeginDrag(new Vector2(0, 100), new Vector2(100, 200), PageEdge.Left));
        TestAssert.Equal(PageTurnState.Idle, controller.Snapshot.State);
    }

    private static void Edge_progress_uses_the_selected_free_edge_normal()
    {
        var pageSize = new Vector2(100, 200);
        var cases = new[]
        {
            (Locked: PageEdge.Bottom, Start: new Vector2(50, 0), Pointer: new Vector2(50, 100)),
            (Locked: PageEdge.Top, Start: new Vector2(50, 200), Pointer: new Vector2(50, 100)),
            (Locked: PageEdge.Left, Start: new Vector2(100, 100), Pointer: new Vector2(50, 100)),
            (Locked: PageEdge.Right, Start: new Vector2(0, 100), Pointer: new Vector2(50, 100)),
        };

        foreach (var testCase in cases)
        {
            var controller = new PageTurnController();

            TestAssert.True(controller.BeginDrag(testCase.Start, pageSize, testCase.Locked));
            controller.UpdateDrag(testCase.Pointer);
            TestAssert.Equal(0.5f, controller.Snapshot.Progress, 3);
        }
    }

    private static void Every_corner_can_start_an_edge_aware_drag()
    {
        var pageSize = new Vector2(100, 200);
        var corners = new[]
        {
            new Vector2(0, 0),
            new Vector2(pageSize.X, 0),
            new Vector2(pageSize.X, pageSize.Y),
            new Vector2(0, pageSize.Y),
        };

        foreach (var corner in corners)
        {
            var controller = new PageTurnController();
            TestAssert.True(controller.BeginDrag(corner, pageSize, PageEdge.Auto));
            controller.UpdateDrag(Vector2.Lerp(corner, pageSize * 0.5f, 0.4f));

            TestAssert.True(controller.Snapshot.FreeEdge is
                PageEdge.Top or PageEdge.Right or PageEdge.Bottom or PageEdge.Left);
            TestAssert.InRange(controller.Snapshot.Progress, 0, 1);
        }
    }
}
