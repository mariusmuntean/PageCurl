using System.Numerics;
using PageCurl.Core;

namespace PageCurl.Core.Tests;

internal static class PageAnchorTests
{
    public static void RunAll()
    {
        Every_physical_edge_round_trips_through_an_anchor();
        Corner_anchors_are_identified_without_being_edges();
        Anchor_positions_match_the_page_boundary();
        Threshold_and_flick_options_reject_invalid_values();
        Snap_threshold_remains_an_alias_for_page_change_threshold();
        Threshold_event_args_preserve_valid_semantics();
        Page_change_threshold_can_be_tuned_after_controller_creation();
        Oversized_meshes_are_rejected_before_allocation();
    }

    private static void Every_physical_edge_round_trips_through_an_anchor()
    {
        var edges = new[] { PageEdge.Top, PageEdge.Right, PageEdge.Bottom, PageEdge.Left };

        foreach (var edge in edges)
        {
            var anchor = PageAnchorExtensions.FromEdge(edge);

            TestAssert.True(anchor.TryGetEdge(out var roundTripped));
            TestAssert.Equal(edge, roundTripped);
        }

        TestAssert.Equal(PageAnchor.Auto, PageAnchorExtensions.FromEdge(PageEdge.Auto));
    }

    private static void Corner_anchors_are_identified_without_being_edges()
    {
        var corners = new[]
        {
            PageAnchor.TopLeft,
            PageAnchor.TopRight,
            PageAnchor.BottomRight,
            PageAnchor.BottomLeft,
        };

        foreach (var corner in corners)
        {
            TestAssert.True(corner.IsCorner());
            TestAssert.False(corner.TryGetEdge(out _));
        }
    }

    private static void Anchor_positions_match_the_page_boundary()
    {
        var size = new Vector2(100, 200);
        var cases = new[]
        {
            (PageAnchor.Auto, new Vector2(50, 100)),
            (PageAnchor.Top, new Vector2(50, 0)),
            (PageAnchor.Right, new Vector2(100, 100)),
            (PageAnchor.Bottom, new Vector2(50, 200)),
            (PageAnchor.Left, new Vector2(0, 100)),
            (PageAnchor.TopLeft, Vector2.Zero),
            (PageAnchor.TopRight, new Vector2(100, 0)),
            (PageAnchor.BottomRight, size),
            (PageAnchor.BottomLeft, new Vector2(0, 200)),
        };

        foreach (var (anchor, expected) in cases)
        {
            TestAssert.Equal(expected, anchor.GetPosition(size));
        }
    }

    private static void Threshold_and_flick_options_reject_invalid_values()
    {
        TestAssert.Throws<ArgumentOutOfRangeException>(() =>
            _ = new PageTurnController(new PageCurlOptions { PageChangeThreshold = -0.01f }));
        TestAssert.Throws<ArgumentOutOfRangeException>(() =>
            _ = new PageTurnController(new PageCurlOptions { PageChangeThreshold = 1.01f }));
        TestAssert.Throws<ArgumentOutOfRangeException>(() =>
            _ = new PageTurnController(new PageCurlOptions { PageChangeThreshold = float.NaN }));
        TestAssert.Throws<ArgumentOutOfRangeException>(() =>
            _ = new PageTurnController(new PageCurlOptions { FlickVelocityThreshold = 0 }));
        TestAssert.Throws<ArgumentOutOfRangeException>(() =>
            _ = new PageTurnController(new PageCurlOptions { FlickVelocityThreshold = float.PositiveInfinity }));
    }

    private static void Snap_threshold_remains_an_alias_for_page_change_threshold()
    {
        var legacy = new PageCurlOptions { SnapThreshold = 0.35f };
        var current = new PageCurlOptions { PageChangeThreshold = 0.65f };

        TestAssert.Equal(0.35f, legacy.PageChangeThreshold);
        TestAssert.Equal(0.65f, current.SnapThreshold);
        TestAssert.True(current.FlickEnabled);
        TestAssert.Equal(1.5f, current.FlickVelocityThreshold);
    }

    private static void Threshold_event_args_preserve_valid_semantics()
    {
        var args = new PageChangeThresholdCrossedEventArgs(
            PageTurnDirection.Backward,
            isCommitCandidate: true,
            progress: 0.35f);

        TestAssert.Equal(PageTurnDirection.Backward, args.Direction);
        TestAssert.True(args.IsCommitCandidate);
        TestAssert.Equal(0.35f, args.Progress);
        TestAssert.Throws<ArgumentOutOfRangeException>(() =>
            _ = new PageChangeThresholdCrossedEventArgs(
                PageTurnDirection.Forward,
                isCommitCandidate: false,
                progress: float.NaN));
    }

    private static void Page_change_threshold_can_be_tuned_after_controller_creation()
    {
        var options = new PageCurlOptions { FlickEnabled = false };
        var controller = new PageTurnController(options);
        options.PageChangeThreshold = 0.8f;

        controller.BeginDrag(new Vector2(100, 100), new Vector2(100, 200));
        controller.UpdateDrag(new Vector2(40, 100));
        controller.EndDrag();

        TestAssert.Equal(0f, controller.Snapshot.TargetProgress);
    }

    private static void Oversized_meshes_are_rejected_before_allocation()
    {
        TestAssert.Throws<ArgumentOutOfRangeException>(() =>
            _ = new PageMeshBuffer(new PageCurlOptions
            {
                MeshColumns = 129,
                MeshRows = 128,
            }));
        TestAssert.Throws<ArgumentOutOfRangeException>(() =>
            _ = new PageMeshBuffer(new PageCurlOptions
            {
                MeshColumns = int.MaxValue,
                MeshRows = 2,
            }));
    }
}
