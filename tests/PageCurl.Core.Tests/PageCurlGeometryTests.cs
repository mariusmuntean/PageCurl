using System.Numerics;
using PageCurl.Core;

namespace PageCurl.Core.Tests;

internal static class PageCurlGeometryTests
{
    private static readonly Vector2 PageSize = new(100, 160);

    public static void RunAll()
    {
        Mid_curl_produces_finite_front_and_back_mesh_regions();
        Mesh_texture_coordinates_and_shading_are_valid();
        Inactive_pose_generates_a_flat_front_page();
        Invalid_pose_generates_a_flat_front_page();
        Repeated_builds_reuse_mesh_arrays();
        Sampled_drag_poses_remain_finite_and_bounded();
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
