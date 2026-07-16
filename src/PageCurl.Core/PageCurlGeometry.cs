using System.Numerics;

namespace PageCurl.Core;

/// <summary>Builds a reusable, shaded cylinder-style page curl mesh.</summary>
public static class PageCurlGeometry
{
    private const float Epsilon = 0.0001f;

    public static CurlPose ComputePose(
        Vector2 pageSize,
        Vector2 dragStart,
        Vector2 pointer,
        PageTurnDirection direction,
        float progress,
        PageCurlOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (!IsValidPageSize(pageSize) || !IsFinite(dragStart) || !IsFinite(pointer))
        {
            return CurlPose.Inactive(direction);
        }

        var clampedProgress = Math.Clamp(progress, 0, 1);
        // During a settle the finger is no longer moving, so reconstruct the
        // curl edge from the animated progress. During a drag this is the same
        // x-coordinate that the controller derived from the pointer.
        var animatedPointer = new Vector2(
            direction == PageTurnDirection.Forward
                ? pageSize.X * (1f - clampedProgress)
                : pageSize.X * clampedProgress,
            pointer.Y);
        var delta = animatedPointer - dragStart;
        var distance = delta.Length();
        var curlDirection = distance > Epsilon
            ? Vector2.Normalize(delta)
            : direction == PageTurnDirection.Forward ? -Vector2.UnitX : Vector2.UnitX;

        var baseRadius = MathF.Max(pageSize.X * options.RadiusScale, 1f);
        var curlLength = MathF.PI * baseRadius;
        var radius = baseRadius;
        var maximumDistance = MathF.Max(pageSize.X * 2f - curlLength, baseRadius);
        if (distance > maximumDistance)
        {
            radius = MathF.Max((pageSize.X * 2f - distance) / MathF.PI, 1f);
            curlLength = MathF.PI * radius;
        }

        var position = animatedPointer;
        if (distance > curlLength)
        {
            position -= curlDirection * ((distance - curlLength) * 0.5f);
        }

        return new(position, curlDirection, radius, clampedProgress, direction, clampedProgress > Epsilon);
    }

    public static void Build(
        PageMeshBuffer buffer,
        Vector2 pageSize,
        CurlPose pose,
        PageCurlOptions options)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (!IsValidPageSize(pageSize)
            || !pose.IsActive
            || !IsFinite(pose.Position)
            || !IsFinite(pose.Direction)
            || pose.Direction.LengthSquared() <= Epsilon
            || !float.IsFinite(pose.Progress)
            || pose.Progress is <= Epsilon or > 1
            || !float.IsFinite(pose.Radius)
            || pose.Radius <= Epsilon)
        {
            WriteFlatPage(buffer, pageSize);
            return;
        }

        var direction = Vector2.Normalize(pose.Direction);
        var angle = MathF.Atan2(direction.Y, direction.X);
        var cos = MathF.Cos(angle);
        var sin = MathF.Sin(angle);
        var radius = MathF.Max(pose.Radius, 1f);
        var curlLength = MathF.PI * radius;
        var columns = buffer.Columns;
        var rows = buffer.Rows;

        for (var row = 0; row <= rows; row++)
        {
            var v = row / (float)rows;
            var sourceY = pageSize.Y * v;
            for (var column = 0; column <= columns; column++)
            {
                var u = column / (float)columns;
                var source = new Vector2(pageSize.X * u, sourceY);
                var local = Rotate(source - pose.Position, cos, -sin);
                var localX = local.X;
                var transformedX = localX;
                var depth = 0f;
                var shade = 1f;

                if (localX <= -curlLength)
                {
                    transformedX = -(curlLength + localX);
                    depth = radius * 2f;
                    shade = 0.35f;
                }
                else if (localX < 0)
                {
                    var theta = MathF.PI * (localX / curlLength);
                    transformedX = radius * MathF.Sin(theta);
                    depth = radius - (radius * MathF.Cos(theta));
                    shade = 0.1f + (0.9f * MathF.Sqrt(MathF.Max(0, MathF.Sin(theta) + 1f)));
                }

                var world = Rotate(new Vector2(transformedX, local.Y), cos, sin) + pose.Position;
                var texture = new Vector2(
                    Math.Clamp(source.X / pageSize.X, 0, 1),
                    Math.Clamp(source.Y / pageSize.Y, 0, 1));
                buffer.Grid[(row * (columns + 1)) + column] =
                    new PageMeshBuffer.GridVertex(world, texture, Math.Clamp(shade, 0.1f, 1f), depth);
            }
        }

        var orderCount = 0;
        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                var topLeft = (row * (columns + 1)) + column;
                var topRight = topLeft + 1;
                var bottomLeft = topLeft + columns + 1;
                var bottomRight = bottomLeft + 1;
                AddTriangle(buffer, ref orderCount, topLeft, topRight, bottomLeft, radius);
                AddTriangle(buffer, ref orderCount, topRight, bottomRight, bottomLeft, radius);
            }
        }

        SortOrders(buffer.Orders, orderCount);
        buffer.FrontVertexCount = 0;
        buffer.BackVertexCount = 0;
        for (var i = 0; i < orderCount; i++)
        {
            ref var order = ref buffer.Orders[i];
            var target = order.IsBack ? buffer.BackVertices : buffer.FrontVertices;
            var count = order.IsBack ? buffer.BackVertexCount : buffer.FrontVertexCount;
            target[count++] = ToMeshVertex(buffer.Grid[order.A]);
            target[count++] = ToMeshVertex(buffer.Grid[order.B]);
            target[count++] = ToMeshVertex(buffer.Grid[order.C]);
            if (order.IsBack)
            {
                buffer.BackVertexCount = count;
            }
            else
            {
                buffer.FrontVertexCount = count;
            }
        }
    }

    private static void AddTriangle(
        PageMeshBuffer buffer,
        ref int orderCount,
        int a,
        int b,
        int c,
        float radius)
    {
        var first = buffer.Grid[a];
        var second = buffer.Grid[b];
        var third = buffer.Grid[c];
        var depth = (first.Depth + second.Depth + third.Depth) / 3f;
        buffer.Orders[orderCount++] = new PageMeshBuffer.TriangleOrder
        {
            A = a,
            B = b,
            C = c,
            Depth = depth,
            IsBack = depth > radius * 0.2f,
        };
    }

    private static void SortOrders(PageMeshBuffer.TriangleOrder[] orders, int count)
    {
        for (var i = 1; i < count; i++)
        {
            var current = orders[i];
            var j = i - 1;
            while (j >= 0 && orders[j].Depth > current.Depth)
            {
                orders[j + 1] = orders[j];
                j--;
            }

            orders[j + 1] = current;
        }
    }

    private static MeshVertex ToMeshVertex(PageMeshBuffer.GridVertex vertex) =>
        new(vertex.Position, vertex.TextureCoordinate, vertex.Shade, vertex.Depth);

    private static void WriteFlatPage(PageMeshBuffer buffer, Vector2 pageSize)
    {
        var safePageSize = IsValidPageSize(pageSize) ? pageSize : Vector2.Zero;
        var bottomRight = new Vector2(safePageSize.X, safePageSize.Y);
        var bottomLeft = new Vector2(0, safePageSize.Y);
        buffer.FrontVertices[0] = new(new Vector2(0, 0), new(0, 0), 1, 0);
        buffer.FrontVertices[1] = new(new Vector2(safePageSize.X, 0), new(1, 0), 1, 0);
        buffer.FrontVertices[2] = new(bottomLeft, new(0, 1), 1, 0);
        buffer.FrontVertices[3] = new(bottomLeft, new(0, 1), 1, 0);
        buffer.FrontVertices[4] = new(new Vector2(safePageSize.X, 0), new(1, 0), 1, 0);
        buffer.FrontVertices[5] = new(bottomRight, new(1, 1), 1, 0);
        buffer.FrontVertexCount = 6;
        buffer.BackVertexCount = 0;
    }

    private static Vector2 Rotate(Vector2 value, float cos, float sin) =>
        new((value.X * cos) - (value.Y * sin), (value.X * sin) + (value.Y * cos));

    private static bool IsValidPageSize(Vector2 value) =>
        IsFinite(value) && value.X > 0 && value.Y > 0;

    private static bool IsFinite(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y);
}
