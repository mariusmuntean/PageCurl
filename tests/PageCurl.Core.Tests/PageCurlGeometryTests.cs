using System.Numerics;
using PageCurl.Core;

namespace PageCurl.Core.Tests;

internal static class PageCurlGeometryTests
{
    private static readonly Vector2 PageSize = new(100, 160);
    private static readonly PageEdge[] PhysicalEdges =
    {
        PageEdge.Top,
        PageEdge.Right,
        PageEdge.Bottom,
        PageEdge.Left,
    };

    public static void RunAll()
    {
        Mid_curl_produces_finite_front_and_back_mesh_regions();
        Mesh_texture_coordinates_and_shading_are_valid();
        Inactive_pose_generates_a_flat_front_page();
        Invalid_pose_generates_a_flat_front_page();
        Repeated_builds_reuse_mesh_arrays();
        Build_result_depends_only_on_the_current_pose();
        Sampled_drag_poses_remain_finite_and_bounded();
        Corner_origin_drag_poses_remain_finite_and_bounded();
        Dragged_free_corner_tracks_pointer_before_limit();
        Every_free_edge_produces_a_finite_bounded_mesh();
        Every_selected_hinge_edge_remains_stationary();
        Curl_radius_relaxes_at_both_turn_endpoints();
        Developable_surface_preserves_material_triangle_lengths();
    }

    private static void Mid_curl_produces_finite_front_and_back_mesh_regions()
    {
        var options = new PageCurlOptions { MeshColumns = 18, MeshRows = 8 };
        var buffer = new PageMeshBuffer(options);
        var pose = new CurlPose(
            new Vector2(45, 80),
            Vector2.Normalize(new Vector2(-1, 0.15f)),
            28,
            0.5f,
            PageTurnDirection.Forward,
            true);

        PageCurlGeometry.Build(buffer, PageSize, pose, options);

        TestAssert.True(buffer.FrontVertexCount > 0);
        TestAssert.True(buffer.BackVertexCount > 0);
        AssertFiniteAndBounded(buffer.FrontVertices, buffer.FrontVertexCount);
        AssertFiniteAndBounded(buffer.BackVertices, buffer.BackVertexCount);
    }

    private static void Mesh_texture_coordinates_and_shading_are_valid()
    {
        var options = new PageCurlOptions { MeshColumns = 12, MeshRows = 6 };
        var buffer = new PageMeshBuffer(options);
        var pose = new CurlPose(
            new Vector2(50, 80),
            Vector2.UnitX,
            25,
            0.5f,
            PageTurnDirection.Forward,
            true);

        PageCurlGeometry.Build(buffer, PageSize, pose, options);

        foreach (var vertex in buffer.FrontVertices.AsSpan(0, buffer.FrontVertexCount))
        {
            TestAssert.InRange(vertex.TextureCoordinate.X, 0f, 1f);
            TestAssert.InRange(vertex.TextureCoordinate.Y, 0f, 1f);
            TestAssert.InRange(vertex.Shade, 0.09f, 1.01f);
        }

        foreach (var vertex in buffer.BackVertices.AsSpan(0, buffer.BackVertexCount))
        {
            TestAssert.InRange(vertex.TextureCoordinate.X, 0f, 1f);
            TestAssert.InRange(vertex.TextureCoordinate.Y, 0f, 1f);
            TestAssert.InRange(vertex.Shade, 0.09f, 1.01f);
        }
    }

    private static void Inactive_pose_generates_a_flat_front_page()
    {
        var options = new PageCurlOptions { MeshColumns = 4, MeshRows = 3 };
        var buffer = new PageMeshBuffer(options);

        PageCurlGeometry.Build(
            buffer,
            PageSize,
            CurlPose.Inactive(PageTurnDirection.Forward),
            options);

        TestAssert.Equal(6, buffer.FrontVertexCount);
        TestAssert.Equal(0, buffer.BackVertexCount);
        TestAssert.Equal(new Vector2(0, 0), buffer.FrontVertices[0].Position);
        TestAssert.Equal(new Vector2(PageSize.X, PageSize.Y), buffer.FrontVertices[5].Position);
    }

    private static void Invalid_pose_generates_a_flat_front_page()
    {
        var options = new PageCurlOptions { MeshColumns = 4, MeshRows = 3 };
        var invalidPoses = new[]
        {
            new CurlPose(new(float.NaN, 10), Vector2.UnitX, 25, 0.5f, PageTurnDirection.Forward, true),
            new CurlPose(new(50, 80), Vector2.UnitX, float.PositiveInfinity, 0.5f, PageTurnDirection.Forward, true),
            new CurlPose(new(50, 80), Vector2.UnitX, 25, float.NaN, PageTurnDirection.Forward, true),
        };

        foreach (var pose in invalidPoses)
        {
            var buffer = new PageMeshBuffer(options);
            PageCurlGeometry.Build(buffer, PageSize, pose, options);

            TestAssert.Equal(6, buffer.FrontVertexCount);
            TestAssert.Equal(0, buffer.BackVertexCount);
            AssertFiniteAndBounded(buffer.FrontVertices, buffer.FrontVertexCount);
        }
    }

    private static void Repeated_builds_reuse_mesh_arrays()
    {
        var options = new PageCurlOptions { MeshColumns = 18, MeshRows = 8 };
        var buffer = new PageMeshBuffer(options);
        var frontArray = buffer.FrontVertices;
        var backArray = buffer.BackVertices;

        var pose = new CurlPose(
            new Vector2(45, 80),
            Vector2.UnitX,
            28,
            0.5f,
            PageTurnDirection.Forward,
            true);

        PageCurlGeometry.Build(buffer, PageSize, pose, options);
        PageCurlGeometry.Build(buffer, PageSize, pose with { Progress = 0.75f }, options);

        TestAssert.Same(frontArray, buffer.FrontVertices);
        TestAssert.Same(backArray, buffer.BackVertices);
    }

    private static void Build_result_depends_only_on_the_current_pose()
    {
        var options = new PageCurlOptions { MeshColumns = 12, MeshRows = 8 };
        var reused = new PageMeshBuffer(options);
        var fresh = new PageMeshBuffer(options);
        var firstPose = PageCurlGeometry.ComputePose(
            PageSize,
            new Vector2(PageSize.X, 80),
            new Vector2(72, 62),
            PageTurnDirection.Forward,
            0.28f,
            options);
        var currentPose = PageCurlGeometry.ComputePose(
            PageSize,
            new Vector2(PageSize.X, 80),
            new Vector2(38, 105),
            PageTurnDirection.Forward,
            0.62f,
            options);

        PageCurlGeometry.Build(reused, PageSize, firstPose, options);
        PageCurlGeometry.Build(reused, PageSize, currentPose, options);
        PageCurlGeometry.Build(fresh, PageSize, currentPose, options);

        TestAssert.Equal(fresh.FrontVertexCount, reused.FrontVertexCount);
        TestAssert.Equal(fresh.BackVertexCount, reused.BackVertexCount);
        for (var index = 0; index < fresh.FrontVertexCount; index++)
        {
            TestAssert.Equal(fresh.FrontVertices[index], reused.FrontVertices[index]);
        }

        for (var index = 0; index < fresh.BackVertexCount; index++)
        {
            TestAssert.Equal(fresh.BackVertices[index], reused.BackVertices[index]);
        }
    }

    private static void Sampled_drag_poses_remain_finite_and_bounded()
    {
        var options = new PageCurlOptions { MeshColumns = 10, MeshRows = 5 };
        var buffer = new PageMeshBuffer(options);
        foreach (var direction in Enum.GetValues<PageTurnDirection>())
        {
            var dragStart = direction == PageTurnDirection.Forward
                ? new Vector2(PageSize.X, 80)
                : new Vector2(0, 80);
            for (var sample = 1; sample < 20; sample++)
            {
                var progress = sample / 20f;
                var pointer = new Vector2(
                    direction == PageTurnDirection.Forward
                        ? PageSize.X * (1 - progress)
                        : PageSize.X * progress,
                    20 + (sample * 6));
                var pose = PageCurlGeometry.ComputePose(
                    PageSize,
                    dragStart,
                    pointer,
                    direction,
                    progress,
                    options);
                PageCurlGeometry.Build(buffer, PageSize, pose, options);
                AssertFiniteAndBounded(buffer.FrontVertices, buffer.FrontVertexCount);
                AssertFiniteAndBounded(buffer.BackVertices, buffer.BackVertexCount);
            }
        }
    }

    private static void Corner_origin_drag_poses_remain_finite_and_bounded()
    {
        var options = new PageCurlOptions { MeshColumns = 10, MeshRows = 5 };
        var buffer = new PageMeshBuffer(options);
        var cases = new[]
        {
            (Start: new Vector2(0, 0), Pointer: new Vector2(55, 35), Direction: PageTurnDirection.Backward),
            (Start: new Vector2(0, PageSize.Y), Pointer: new Vector2(55, 125), Direction: PageTurnDirection.Backward),
            (Start: new Vector2(PageSize.X, 0), Pointer: new Vector2(45, 35), Direction: PageTurnDirection.Forward),
            (Start: new Vector2(PageSize.X, PageSize.Y), Pointer: new Vector2(45, 125), Direction: PageTurnDirection.Forward),
        };

        foreach (var testCase in cases)
        {
            var pose = PageCurlGeometry.ComputePose(
                PageSize,
                testCase.Start,
                testCase.Pointer,
                testCase.Direction,
                0.5f,
                options);

            PageCurlGeometry.Build(buffer, PageSize, pose, options);

            AssertFiniteAndBounded(buffer.FrontVertices, buffer.FrontVertexCount);
            AssertFiniteAndBounded(buffer.BackVertices, buffer.BackVertexCount);
        }
    }

    private static void Dragged_free_corner_tracks_pointer_before_limit()
    {
        var options = new PageCurlOptions { MeshColumns = 10, MeshRows = 5 };
        var buffer = new PageMeshBuffer(options);
        var cases = new[]
        {
            (Hinge: PageEdge.Left, Free: PageEdge.Right, Start: new Vector2(PageSize.X, 0), Pointer: new Vector2(60, 40), Texture: new Vector2(1, 0), Progress: 0.4f),
            (Hinge: PageEdge.Right, Free: PageEdge.Left, Start: new Vector2(0, 0), Pointer: new Vector2(40, 40), Texture: new Vector2(0, 0), Progress: 0.4f),
            (Hinge: PageEdge.Bottom, Free: PageEdge.Top, Start: new Vector2(PageSize.X, 0), Pointer: new Vector2(60, 40), Texture: new Vector2(1, 0), Progress: 0.25f),
            (Hinge: PageEdge.Top, Free: PageEdge.Bottom, Start: new Vector2(PageSize.X, PageSize.Y), Pointer: new Vector2(60, 120), Texture: new Vector2(1, 1), Progress: 0.25f),
        };

        foreach (var testCase in cases)
        {
            var pose = PageCurlGeometry.ComputePose(
                PageSize,
                testCase.Start,
                testCase.Pointer,
                testCase.Hinge,
                testCase.Free,
                PageTurnDirection.Forward,
                testCase.Progress,
                options);

            TestAssert.True(Vector2.Distance(pose.Position, testCase.Pointer) <= 0.01f);
            PageCurlGeometry.Build(buffer, PageSize, pose, options);

            var closestDistance = FindClosestVertexToTexturePoint(
                buffer.FrontVertices,
                buffer.FrontVertexCount,
                buffer.BackVertices,
                buffer.BackVertexCount,
                testCase.Texture,
                testCase.Pointer);
            TestAssert.InRange(closestDistance, 0, 1.5f);
        }
    }

    private static float FindClosestVertexToTexturePoint(
        MeshVertex[] first,
        int firstCount,
        MeshVertex[] second,
        int secondCount,
        Vector2 texturePoint,
        Vector2 target)
    {
        var closest = float.PositiveInfinity;
        UpdateClosest(first, firstCount, texturePoint, target, ref closest);
        UpdateClosest(second, secondCount, texturePoint, target, ref closest);
        return closest;
    }

    private static void UpdateClosest(
        MeshVertex[] vertices,
        int count,
        Vector2 texturePoint,
        Vector2 target,
        ref float closest)
    {
        for (var i = 0; i < count; i++)
        {
            var vertex = vertices[i];
            if (Vector2.DistanceSquared(vertex.TextureCoordinate, texturePoint) > 0.0001f)
            {
                continue;
            }

            closest = MathF.Min(closest, Vector2.Distance(vertex.Position, target));
        }
    }

    private static void Every_free_edge_produces_a_finite_bounded_mesh()
    {
        var options = new PageCurlOptions { MeshColumns = 10, MeshRows = 5 };
        var buffer = new PageMeshBuffer(options);
        var cases = new[]
        {
            (Hinge: PageEdge.Left, Free: PageEdge.Right, Start: new Vector2(100, 80), Pointer: new Vector2(50, 80)),
            (Hinge: PageEdge.Right, Free: PageEdge.Left, Start: new Vector2(0, 80), Pointer: new Vector2(50, 80)),
            (Hinge: PageEdge.Bottom, Free: PageEdge.Top, Start: new Vector2(50, 0), Pointer: new Vector2(50, 80)),
            (Hinge: PageEdge.Top, Free: PageEdge.Bottom, Start: new Vector2(50, 160), Pointer: new Vector2(50, 80)),
        };

        foreach (var testCase in cases)
        {
            var pose = PageCurlGeometry.ComputePose(
                PageSize,
                testCase.Start,
                testCase.Pointer,
                testCase.Hinge,
                testCase.Free,
                PageTurnDirection.Forward,
                0.5f,
                options);

            PageCurlGeometry.Build(buffer, PageSize, pose, options);
            AssertFiniteAndBounded(buffer.FrontVertices, buffer.FrontVertexCount);
            AssertFiniteAndBounded(buffer.BackVertices, buffer.BackVertexCount);
        }
    }

    private static void Every_selected_hinge_edge_remains_stationary()
    {
        var options = new PageCurlOptions { MeshColumns = 10, MeshRows = 5 };
        var buffer = new PageMeshBuffer(options);
        foreach (var hingeEdge in PhysicalEdges)
        {
            foreach (var freeEdge in PhysicalEdges)
            {
                if (hingeEdge == freeEdge)
                {
                    continue;
                }

                var pose = PageCurlGeometry.ComputePose(
                    PageSize,
                    EdgeCenter(freeEdge),
                    PageSize * 0.5f,
                    hingeEdge,
                    freeEdge,
                    PageTurnDirection.Forward,
                    0.5f,
                    options);

                PageCurlGeometry.Build(buffer, PageSize, pose, options);
                var representedVertices = AssertHingeVerticesAreStationary(
                    buffer.FrontVertices,
                    buffer.FrontVertexCount,
                    hingeEdge);
                representedVertices += AssertHingeVerticesAreStationary(
                    buffer.BackVertices,
                    buffer.BackVertexCount,
                    hingeEdge);
                TestAssert.True(representedVertices > 0, $"No mesh vertices represented the {hingeEdge} hinge.");
            }
        }
    }

    private static void Curl_radius_relaxes_at_both_turn_endpoints()
    {
        var options = new PageCurlOptions();
        var start = PageCurlGeometry.ComputePose(
            PageSize,
            new Vector2(PageSize.X, PageSize.Y * 0.5f),
            new Vector2(PageSize.X * 0.95f, PageSize.Y * 0.5f),
            PageTurnDirection.Forward,
            0.05f,
            options);
        var middle = PageCurlGeometry.ComputePose(
            PageSize,
            new Vector2(PageSize.X, PageSize.Y * 0.5f),
            new Vector2(PageSize.X * 0.5f, PageSize.Y * 0.5f),
            PageTurnDirection.Forward,
            0.5f,
            options);
        var end = PageCurlGeometry.ComputePose(
            PageSize,
            new Vector2(PageSize.X, PageSize.Y * 0.5f),
            new Vector2(PageSize.X * 0.05f, PageSize.Y * 0.5f),
            PageTurnDirection.Forward,
            0.95f,
            options);

        TestAssert.True(start.Radius > middle.Radius * 2);
        TestAssert.True(end.Radius > middle.Radius * 2);
    }

    private static void Developable_surface_preserves_material_triangle_lengths()
    {
        const int columns = 24;
        const int rows = 24;
        var options = new PageCurlOptions { MeshColumns = columns, MeshRows = rows };
        var buffer = new PageMeshBuffer(options);
        var pose = PageCurlGeometry.ComputePose(
            PageSize,
            new Vector2(PageSize.X, 80),
            new Vector2(52, 48),
            PageEdge.Left,
            PageEdge.Right,
            PageTurnDirection.Forward,
            0.48f,
            options);

        PageCurlGeometry.Build(buffer, PageSize, pose, options);

        var horizontalRestLength = PageSize.X / columns;
        var verticalRestLength = PageSize.Y / rows;
        var diagonalRestLength = MathF.Sqrt(
            (horizontalRestLength * horizontalRestLength)
            + (verticalRestLength * verticalRestLength));
        for (var row = 0; row <= rows; row++)
        {
            for (var column = 0; column <= columns; column++)
            {
                var texture = new Vector2(column / (float)columns, row / (float)rows);
                if (column < columns)
                {
                    AssertRestLength(
                        buffer,
                        texture,
                        new Vector2((column + 1) / (float)columns, texture.Y),
                        horizontalRestLength,
                        $"horizontal edge ({column}, {row})");
                }

                if (row < rows)
                {
                    AssertRestLength(
                        buffer,
                        texture,
                        new Vector2(texture.X, (row + 1) / (float)rows),
                        verticalRestLength,
                        $"vertical edge ({column}, {row})");
                }

                if (column < columns && row < rows)
                {
                    AssertRestLength(
                        buffer,
                        new Vector2((column + 1) / (float)columns, texture.Y),
                        new Vector2(texture.X, (row + 1) / (float)rows),
                        diagonalRestLength,
                        $"triangle diagonal ({column}, {row})");
                }
            }
        }
    }

    private static void AssertRestLength(
        PageMeshBuffer buffer,
        Vector2 firstTexture,
        Vector2 secondTexture,
        float restLength,
        string label)
    {
        var first = ToPosition3(FindVertexAtTexture(buffer, firstTexture));
        var second = ToPosition3(FindVertexAtTexture(buffer, secondTexture));
        var relativeError = MathF.Abs(Vector3.Distance(first, second) - restLength) / restLength;
        TestAssert.True(relativeError <= 0.01f, $"{label} changed length by {relativeError:P2}.");
    }

    private static MeshVertex FindVertexAtTexture(PageMeshBuffer buffer, Vector2 texture)
    {
        var closest = default(MeshVertex);
        var closestDistanceSquared = float.PositiveInfinity;
        FindClosestTexture(buffer.FrontVertices, buffer.FrontVertexCount, texture, ref closest, ref closestDistanceSquared);
        FindClosestTexture(buffer.BackVertices, buffer.BackVertexCount, texture, ref closest, ref closestDistanceSquared);
        TestAssert.True(
            closestDistanceSquared <= 0.001f * 0.001f,
            $"No mesh vertex represents texture coordinate {texture}.");
        return closest;
    }

    private static void FindClosestTexture(
        MeshVertex[] vertices,
        int count,
        Vector2 texture,
        ref MeshVertex closest,
        ref float closestDistanceSquared)
    {
        for (var i = 0; i < count; i++)
        {
            var distanceSquared = Vector2.DistanceSquared(vertices[i].TextureCoordinate, texture);
            if (distanceSquared < closestDistanceSquared)
            {
                closest = vertices[i];
                closestDistanceSquared = distanceSquared;
            }
        }
    }

    private static Vector3 ToPosition3(MeshVertex vertex) =>
        new(vertex.Position, vertex.Depth);

    private static int AssertHingeVerticesAreStationary(MeshVertex[] vertices, int count, PageEdge hingeEdge)
    {
        var hingeVertexCount = 0;
        for (var i = 0; i < count; i++)
        {
            var vertex = vertices[i];
            if (!IsOnHinge(vertex.TextureCoordinate, hingeEdge))
            {
                continue;
            }

            hingeVertexCount++;
            if (hingeEdge is PageEdge.Left or PageEdge.Right)
            {
                TestAssert.Equal(hingeEdge == PageEdge.Left ? 0f : PageSize.X, vertex.Position.X, 3);
            }
            else
            {
                TestAssert.Equal(hingeEdge == PageEdge.Top ? 0f : PageSize.Y, vertex.Position.Y, 3);
            }
        }

        return hingeVertexCount;
    }

    private static bool IsOnHinge(Vector2 textureCoordinate, PageEdge hingeEdge) => hingeEdge switch
    {
        PageEdge.Top => textureCoordinate.Y <= 0.0001f,
        PageEdge.Right => textureCoordinate.X >= 0.9999f,
        PageEdge.Bottom => textureCoordinate.Y >= 0.9999f,
        PageEdge.Left => textureCoordinate.X <= 0.0001f,
        _ => false,
    };

    private static Vector2 EdgeCenter(PageEdge edge) => edge switch
    {
        PageEdge.Top => new(PageSize.X * 0.5f, 0),
        PageEdge.Right => new(PageSize.X, PageSize.Y * 0.5f),
        PageEdge.Bottom => new(PageSize.X * 0.5f, PageSize.Y),
        PageEdge.Left => new(0, PageSize.Y * 0.5f),
        _ => PageSize * 0.5f,
    };

    private static void AssertFiniteAndBounded(MeshVertex[] vertices, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var vertex = vertices[i];
            TestAssert.True(float.IsFinite(vertex.Position.X));
            TestAssert.True(float.IsFinite(vertex.Position.Y));
            TestAssert.InRange(vertex.Position.X, -PageSize.X * 1.5f, PageSize.X * 2.5f);
            TestAssert.InRange(vertex.Position.Y, -PageSize.Y * 1.5f, PageSize.Y * 2.5f);
        }
    }
}
