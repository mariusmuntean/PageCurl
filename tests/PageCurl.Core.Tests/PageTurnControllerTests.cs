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
        Spread_corner_grab_preserves_the_spine_axis();
        Spread_drag_crosses_the_hinge_and_releases_to_the_nearest_endpoint();
        BeginDrag_chooses_edge_and_clamps_drag_start_to_page();
        BeginDrag_supports_all_page_corners();
        Locked_edge_and_endpoints_do_not_start_a_drag();
        Unlocked_edges_still_start_a_drag();
        UpdateDrag_maps_finger_position_to_normalized_progress(PageTurnDirection.Forward);
        UpdateDrag_maps_finger_position_to_normalized_progress(PageTurnDirection.Backward);
        Mid_page_grab_starts_flat_and_curls_only_while_dragging();
        Interior_grabs_track_the_pointer_on_every_free_edge();
        Pose_coordinates_scale_to_the_render_page();
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
        Pinned_corners_reject_only_the_pinned_point();
        Anchor_overload_preserves_the_locked_anchor_in_snapshot_and_pose();
        Corner_anchor_preserves_the_exact_grabbed_material_point();
        Custom_page_change_threshold_controls_release_intent();
        Disabled_flick_does_not_override_the_page_change_threshold();
        Custom_flick_velocity_threshold_controls_release_intent();
        Threshold_event_fires_once_on_entry_and_once_on_exit();
        Reused_controller_starts_each_turn_without_previous_release_intent();
        Completion_handler_can_start_the_next_animated_turn();
        Invalid_frame_times_do_not_change_a_settling_turn();
        High_finite_spring_frequencies_complete_without_non_finite_state();
        Repeated_commit_and_rollback_turns_remain_bounded_and_complete_once();
    }

    private static void Invalid_frame_times_do_not_change_a_settling_turn()
    {
        var controller = new PageTurnController();
        controller.BeginProgrammaticTurn(PageSize, PageTurnDirection.Forward);
        var initial = controller.Snapshot;
        foreach (var time in new[] { -1d, 0d, double.NaN, double.PositiveInfinity, double.MaxValue })
        {
            TestAssert.True(controller.Tick(time));
            TestAssert.Equal(initial, controller.Snapshot);
        }
        RunUntilIdle(controller);
    }

    private static void High_finite_spring_frequencies_complete_without_non_finite_state()
    {
        foreach (var frequency in new[] { 1e20f, float.MaxValue })
        foreach (var direction in new[] { PageTurnDirection.Forward, PageTurnDirection.Backward })
        foreach (var tinyFirstSample in new[] { false, true })
        {
            var controller = new PageTurnController(new PageCurlOptions { SpringFrequency = frequency });
            var completions = 0;
            controller.PageTurnCompleted += (_, args) =>
            {
                completions++;
                TestAssert.True(args.Committed);
                TestAssert.Equal(direction, args.Direction);
            };
            TestAssert.True(controller.BeginProgrammaticTurn(PageSize, direction));
            if (tinyFirstSample)
            {
                TestAssert.True(controller.Tick(float.Epsilon));
                TestAssert.InRange(controller.Snapshot.Progress, 0, 1);
                TestAssert.Equal(0, completions);
            }

            TestAssert.False(controller.Tick(1d / 60),
                $"Spring frequency {frequency} kept requesting frames after reaching its endpoint.");
            TestAssert.Equal(PageTurnState.Idle, controller.Snapshot.State);
            TestAssert.Equal(1f, controller.Snapshot.Progress);
            TestAssert.Equal(1, completions);
            TestAssert.False(controller.Tick(1d / 60));
            TestAssert.Equal(1, completions);
        }
    }

    private static void Repeated_commit_and_rollback_turns_remain_bounded_and_complete_once()
    {
        var controller = new PageTurnController(new PageCurlOptions { FlickEnabled = false });
        var count = 0;
        PageTurnCompletedEventArgs? last = null;
        controller.PageTurnCompleted += (_, args) => { count++; last = args; };
        for (var turn = 0; turn < 500; turn++)
        {
            var forward = turn % 2 == 0;
            var commit = turn % 3 == 0;
            var grab = new Vector2(forward ? PageSize.X : 0, PageSize.Y / 2);
            TestAssert.True(controller.BeginDrag(grab, PageSize, forward ? PageAnchor.Left : PageAnchor.Right));
            var distance = PageSize.X * (commit ? .8f : .2f);
            controller.UpdateDrag(grab + new Vector2(forward ? -distance : distance, 0));
            var beforeInvalidInput = controller.Snapshot;
            controller.UpdateDrag(new(float.NaN, float.PositiveInfinity));
            TestAssert.Equal(beforeInvalidInput, controller.Snapshot);
            controller.EndDrag(animate: turn % 4 != 0);
            RunUntilIdle(controller);
            TestAssert.Equal(turn + 1, count);
            TestAssert.Equal(commit, last!.Committed);
            TestAssert.Equal(forward ? PageTurnDirection.Forward : PageTurnDirection.Backward, last.Direction);
            TestAssert.Equal(commit ? 1f : 0f, controller.Snapshot.Progress);
            controller.Cancel(animate: false);
            controller.EndDrag(animate: false);
            TestAssert.Equal(turn + 1, count);
        }
    }

    private static void Completion_handler_can_start_the_next_animated_turn()
    {
        var controller = new PageTurnController();
        var completions = 0;
        controller.PageTurnCompleted += (_, args) =>
        {
            completions++;
            TestAssert.True(args.Committed);
            if (completions == 1)
            {
                TestAssert.True(controller.BeginProgrammaticTurn(PageSize, PageTurnDirection.Backward));
            }
        };
        TestAssert.True(controller.BeginProgrammaticTurn(PageSize, PageTurnDirection.Forward));
        for (var frame = 0; frame < 240; frame++)
        {
            var needsFrames = controller.Tick(1.0 / 60);
            TestAssert.Equal(controller.Snapshot.State == PageTurnState.Settling, needsFrames);
            if (!needsFrames) break;
        }
        TestAssert.Equal(2, completions);
        TestAssert.Equal(PageTurnState.Idle, controller.Snapshot.State);
    }

    private static void Reused_controller_starts_each_turn_without_previous_release_intent()
    {
        var controller = new PageTurnController();
        var completions = new List<PageTurnCompletedEventArgs>();
        var thresholds = new List<PageChangeThresholdCrossedEventArgs>();
        controller.PageTurnCompleted += (_, args) => completions.Add(args);
        controller.PageChangeThresholdCrossed += (_, args) => thresholds.Add(args);

        // Leave both a threshold crossing and a strong forward velocity behind.
        TestAssert.True(controller.BeginDrag(new(100, 100), PageSize));
        controller.UpdateDrag(new(90, 100), 1);
        controller.UpdateDrag(new(10, 100), 1.016);
        controller.EndDrag(animate: false);
        TestAssert.True(completions[0].Committed);

        TestAssert.True(controller.BeginDrag(new(0, 100), PageSize, PageAnchor.Right));
        TestAssert.Equal(0f, controller.Snapshot.Progress);
        TestAssert.Equal(0f, controller.Snapshot.TargetProgress);
        controller.UpdateDrag(new(60, 100));
        TestAssert.Equal(2, thresholds.Count);
        TestAssert.Equal(PageTurnDirection.Backward, thresholds[1].Direction);
        controller.UpdateDrag(new(10, 100));
        controller.EndDrag(animate: false);
        TestAssert.False(completions[1].Committed);
        TestAssert.Equal(PageTurnDirection.Backward, completions[1].Direction);

        TestAssert.True(controller.BeginProgrammaticTurn(PageSize, PageTurnDirection.Forward, animate: false));
        TestAssert.True(completions[2].Committed);
        TestAssert.True(controller.BeginDrag(new(100, 100), PageSize));
        controller.EndDrag(animate: false);
        TestAssert.False(completions[3].Committed);
        controller.EndDrag(animate: false);
        TestAssert.Equal(4, completions.Count);
    }

    private static void Spread_corner_grab_preserves_the_spine_axis()
    {
        var controller = new PageTurnController(new PageCurlOptions { TurnAcrossHinge = true });
        TestAssert.True(controller.BeginDrag(new(400, 0), new(400, 500), PageAnchor.Left));
        controller.UpdateDrag(new(-300, 40));
        TestAssert.Equal(PageEdge.Right, controller.Snapshot.FreeEdge);
        TestAssert.Equal(0.875f, controller.Snapshot.Progress);
    }

    private static void Spread_drag_crosses_the_hinge_and_releases_to_the_nearest_endpoint()
    {
        var size = new Vector2(400, 500);
        foreach (var forward in new[] { true, false })
        {
            foreach (var fraction in new[] { 0.4f, 0.6f, 1f })
            {
                var options = new PageCurlOptions { TurnAcrossHinge = true, FlickEnabled = false };
                var controller = new PageTurnController(options);
                var grab = new Vector2(forward ? 400 : 0, 250);
                var pointer = grab + new Vector2((forward ? -800 : 800) * fraction, 0);
                TestAssert.True(controller.BeginDrag(grab, size, forward ? PageAnchor.Left : PageAnchor.Right));
                controller.UpdateDrag(pointer);
                TestAssert.Equal(fraction, controller.Snapshot.Progress, 3);
                TestAssert.Equal(pointer, controller.Snapshot.PointerPosition);
                TestAssert.True(Vector2.Distance(pointer, controller.GetCurlPose(size).Position) < 0.02f);
                var mesh = new PageMeshBuffer(options);
                PageCurlGeometry.Build(mesh, size, controller.GetCurlPose(size), options);
                var vertices = mesh.FrontVertices.Take(mesh.FrontVertexCount)
                    .Concat(mesh.BackVertices.Take(mesh.BackVertexCount)).ToArray();
                var uv = grab / size;
                var tip = vertices.First(v => Vector2.Distance(v.TextureCoordinate, uv) < 0.001f);
                TestAssert.True(Vector2.Distance(pointer, tip.Position) < 0.05f,
                    $"Sheet blocked at {tip.Position}, pointer {pointer}");
                foreach (var vertex in vertices.Where(v => Math.Abs(v.TextureCoordinate.X - (forward ? 0 : 1)) < 0.001f))
                    TestAssert.True(Vector2.Distance(vertex.Position, vertex.TextureCoordinate * size) < 0.05f, "Spine moved.");
                if (fraction == 1) TestAssert.Equal(0, mesh.FrontVertexCount);
                PageTurnCompletedEventArgs? completion = null;
                controller.PageTurnCompleted += (_, e) => completion = e;
                controller.EndDrag();
                for (var i = 0; i < 150 && controller.Tick(1d / 60); i++) { }
                TestAssert.NotNull(completion);
                TestAssert.Equal(fraction >= 0.5f, completion!.Committed);
            }
        }
    }

    private static void Pose_coordinates_scale_to_the_render_page()
    {
        foreach (var direction in new[] { PageTurnDirection.Forward, PageTurnDirection.Backward })
        {
            var controller = new PageTurnController();
            var size = new Vector2(300, 400);
            TestAssert.True(controller.BeginProgrammaticTurn(size, direction));
            controller.Tick(0.05);
            var pose = controller.GetCurlPose(size);
            var scaled = controller.GetCurlPose(size * 2);
            TestAssert.True(Vector2.Distance(pose.DragStart * 2, scaled.DragStart) < 0.01f,
                "Render scaling did not move the material grab to the free edge.");
            TestAssert.True(Vector2.Distance(pose.Position * 2, scaled.Position) < 0.01f);
            TestAssert.True(Math.Abs(pose.Radius * 2 - scaled.Radius) < 0.01f);
            TestAssert.Equal(pose.Progress, scaled.Progress);
        }
    }

    private static void Interior_grabs_track_the_pointer_on_every_free_edge()
    {
        var size = new Vector2(400, 500);
        var grab = new Vector2(200, 250);
        var cases = new[]
        {
            (Grab: grab, Anchor: PageAnchor.Left, Pointer: new Vector2(150, 250)),
            (Grab: grab, Anchor: PageAnchor.Right, Pointer: new Vector2(250, 250)),
            (Grab: new Vector2(200, 375), Anchor: PageAnchor.Top, Pointer: new Vector2(200, 325)),
            (Grab: new Vector2(200, 125), Anchor: PageAnchor.Bottom, Pointer: new Vector2(200, 175)),
        };
        foreach (var item in cases)
        {
            var options = new PageCurlOptions { FlickEnabled = false };
            var controller = new PageTurnController(options);
            TestAssert.True(controller.BeginDrag(item.Grab, size, item.Anchor));
            controller.UpdateDrag(item.Grab);
            TestAssert.True(Vector2.Distance(item.Grab, controller.GetCurlPose(size).Position) < 0.01f);
            foreach (var pointer in new[] { item.Pointer, item.Pointer + new Vector2(5, 5) })
            {
                controller.UpdateDrag(pointer);
                var pose = controller.GetCurlPose(size);
                TestAssert.True(Vector2.Distance(pointer, pose.Position) < 0.01f,
                    $"{item.Anchor}: expected {pointer}, got {pose.Position}");
                var mesh = new PageMeshBuffer(options);
                PageCurlGeometry.Build(mesh, size, pose, options);
                var vertex = mesh.FrontVertices.Take(mesh.FrontVertexCount)
                    .Concat(mesh.BackVertices.Take(mesh.BackVertexCount))
                    .First(v => Vector2.Distance(v.TextureCoordinate, item.Grab / size) < 0.0001f);
                TestAssert.True(Vector2.Distance(pointer, vertex.Position) < 0.02f,
                    $"{item.Anchor}: material grab expected {pointer}, got {vertex.Position}");
            }
            controller.EndDrag();
            RunUntilIdle(controller);
        }
    }

    private static void BeginDrag_chooses_edge_and_clamps_drag_start_to_page()
    {
        var controller = new PageTurnController();

        TestAssert.True(controller.BeginDrag(new Vector2(90, -20), PageSize));

        var snapshot = controller.Snapshot;
        TestAssert.Equal(PageTurnState.Dragging, snapshot.State);
        TestAssert.Equal(PageTurnDirection.Forward, snapshot.Direction);
        TestAssert.Equal(new Vector2(90, 0), snapshot.DragStartPosition);

        TestAssert.True(controller.BeginDrag(new Vector2(10, 240), PageSize));
        snapshot = controller.Snapshot;
        TestAssert.Equal(PageTurnDirection.Backward, snapshot.Direction);
        TestAssert.Equal(new Vector2(10, 200), snapshot.DragStartPosition);
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

        // Progress is measured from the grab point, so the same finger
        // position yields the travel fraction the finger actually covered.
        var expected = direction == PageTurnDirection.Forward
            ? (90f - 50f) / 90f
            : (50f - 10f) / (PageSize.X - 10f);
        TestAssert.Equal(expected, controller.Snapshot.Progress, 3);
        TestAssert.Equal(new Vector2(50, 100), controller.Snapshot.PointerPosition);
    }

    private static void Mid_page_grab_starts_flat_and_curls_only_while_dragging()
    {
        var controller = new PageTurnController();
        var grab = new Vector2(PageSize.X * 0.5f, PageSize.Y * 0.5f);

        TestAssert.True(controller.BeginDrag(grab, PageSize));
        TestAssert.Equal(grab, controller.Snapshot.DragStartPosition);
        TestAssert.Equal(0f, controller.Snapshot.Progress, 3);
        // A zero-progress pose is inactive, so the page renders flat at the
        // grab instead of arriving half curled.
        TestAssert.False(controller.GetCurlPose(PageSize).IsActive);

        controller.UpdateDrag(grab + new Vector2(-PageSize.X * 0.1f, 0), 0.1);
        TestAssert.Equal(0.2f, controller.Snapshot.Progress, 3);
        TestAssert.True(controller.GetCurlPose(PageSize).IsActive);
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
        controller.UpdateDrag(new Vector2(30, 100), 1.000);
        controller.UpdateDrag(new Vector2(36, 100), 1.016);
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
        controller.UpdateDrag(new Vector2(44, 100), 1.008);
        controller.UpdateDrag(new Vector2(45, 100), 1.032);
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
        TestAssert.Equal((90f - 63f) / 90f, controller.Snapshot.Progress, 3);
    }

    private static void Pinned_corners_reject_only_the_pinned_point()
    {
        var cases = new[]
        {
            (PageAnchor.TopLeft, Vector2.Zero),
            (PageAnchor.TopRight, new Vector2(PageSize.X, 0)),
            (PageAnchor.BottomRight, PageSize),
            (PageAnchor.BottomLeft, new Vector2(0, PageSize.Y)),
        };

        foreach (var (anchor, pin) in cases)
        {
            var rejected = new PageTurnController();
            var accepted = new PageTurnController();

            TestAssert.False(rejected.BeginDrag(pin, PageSize, anchor));
            TestAssert.True(accepted.BeginDrag(PageSize * 0.5f, PageSize, anchor));
            TestAssert.Equal(anchor, accepted.Snapshot.LockedAnchor);
        }
    }

    private static void Anchor_overload_preserves_the_locked_anchor_in_snapshot_and_pose()
    {
        var controller = new PageTurnController();

        TestAssert.True(controller.BeginDrag(
            new Vector2(PageSize.X, PageSize.Y),
            PageSize,
            PageAnchor.TopLeft));
        controller.UpdateDrag(new Vector2(65, 130));

        TestAssert.Equal(PageAnchor.TopLeft, controller.Snapshot.LockedAnchor);
        TestAssert.Equal(PageAnchor.TopLeft, controller.GetCurlPose(PageSize).LockedAnchor);
    }

    private static void Corner_anchor_preserves_the_exact_grabbed_material_point()
    {
        var controller = new PageTurnController();
        var grabbedPoint = new Vector2(82, 146);

        TestAssert.True(controller.BeginDrag(grabbedPoint, PageSize, PageAnchor.TopLeft));
        controller.UpdateDrag(grabbedPoint);

        TestAssert.Equal(grabbedPoint, controller.Snapshot.DragStartPosition);
        TestAssert.Equal(0f, controller.Snapshot.Progress);

        var pointer = new Vector2(50, 112);
        controller.UpdateDrag(pointer);
        TestAssert.True(Vector2.Distance(controller.GetCurlPose(PageSize).Position, pointer) <= 0.001f);
    }

    private static void Custom_page_change_threshold_controls_release_intent()
    {
        var cases = new[]
        {
            (Threshold: 0f, PointerX: 100f, ExpectedTarget: 1f),
            (Threshold: 0.35f, PointerX: 65f, ExpectedTarget: 1f),
            (Threshold: 1f, PointerX: 0f, ExpectedTarget: 1f),
        };

        foreach (var testCase in cases)
        {
            var controller = new PageTurnController(new PageCurlOptions
            {
                PageChangeThreshold = testCase.Threshold,
                FlickEnabled = false,
            });
            controller.BeginDrag(new Vector2(100, 100), PageSize);
            controller.UpdateDrag(new Vector2(testCase.PointerX, 100));
            controller.EndDrag();

            TestAssert.Equal(testCase.ExpectedTarget, controller.Snapshot.TargetProgress);
        }
    }

    private static void Disabled_flick_does_not_override_the_page_change_threshold()
    {
        var controller = new PageTurnController(new PageCurlOptions
        {
            PageChangeThreshold = 0.8f,
            FlickEnabled = false,
        });
        controller.BeginDrag(new Vector2(100, 100), PageSize);
        controller.UpdateDrag(new Vector2(90, 100), 1.000);
        controller.UpdateDrag(new Vector2(70, 100), 1.016);
        controller.EndDrag();

        TestAssert.Equal(0f, controller.Snapshot.TargetProgress);
    }

    private static void Custom_flick_velocity_threshold_controls_release_intent()
    {
        var sensitive = new PageTurnController(new PageCurlOptions
        {
            PageChangeThreshold = 0.8f,
            FlickVelocityThreshold = 0.4f,
        });
        sensitive.BeginDrag(new Vector2(100, 100), PageSize);
        sensitive.UpdateDrag(new Vector2(90, 100), 1.000);
        sensitive.UpdateDrag(new Vector2(80, 100), 1.100);
        sensitive.EndDrag();

        var deliberate = new PageTurnController(new PageCurlOptions
        {
            PageChangeThreshold = 0.8f,
            FlickVelocityThreshold = 2f,
        });
        deliberate.BeginDrag(new Vector2(100, 100), PageSize);
        deliberate.UpdateDrag(new Vector2(90, 100), 1.000);
        deliberate.UpdateDrag(new Vector2(80, 100), 1.100);
        deliberate.EndDrag();

        TestAssert.Equal(1f, sensitive.Snapshot.TargetProgress);
        TestAssert.Equal(0f, deliberate.Snapshot.TargetProgress);
    }

    private static void Threshold_event_fires_once_on_entry_and_once_on_exit()
    {
        var controller = new PageTurnController(new PageCurlOptions
        {
            PageChangeThreshold = 0.35f,
            FlickEnabled = false,
        });
        var entryCount = 0;
        var exitCount = 0;
        PageChangeThresholdCrossedEventArgs? latest = null;
        controller.PageChangeThresholdCrossed += (_, args) =>
        {
            latest = args;
            if (args.IsCommitCandidate)
            {
                entryCount++;
            }
            else
            {
                exitCount++;
            }
        };

        controller.BeginDrag(new Vector2(100, 100), PageSize, PageAnchor.Left);
        controller.UpdateDrag(new Vector2(70, 100));
        controller.UpdateDrag(new Vector2(60, 100));
        controller.UpdateDrag(new Vector2(50, 100));
        controller.UpdateDrag(new Vector2(70, 100));
        controller.UpdateDrag(new Vector2(80, 100));

        TestAssert.Equal(1, entryCount);
        TestAssert.Equal(1, exitCount);
        TestAssert.NotNull(latest);
        TestAssert.Equal(PageTurnDirection.Forward, latest!.Direction);
        TestAssert.False(latest.IsCommitCandidate);
        TestAssert.Equal(0.3f, latest.Progress, 3);
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
