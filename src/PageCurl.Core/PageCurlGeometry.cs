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
        var hingeEdge = direction == PageTurnDirection.Forward ? PageEdge.Left : PageEdge.Right;
        var freeEdge = direction == PageTurnDirection.Forward ? PageEdge.Right : PageEdge.Left;
        return ComputePose(pageSize, dragStart, pointer, hingeEdge, freeEdge, direction, progress, options);
    }

    /// <summary>
    /// Computes a curl in the frame of the selected free edge while retaining
    /// the physical hinge for renderers that need to anchor that edge.
    /// </summary>
    public static CurlPose ComputePose(
        Vector2 pageSize,
        Vector2 dragStart,
        Vector2 pointer,
        PageEdge hingeEdge,
        PageEdge freeEdge,
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

        var resolvedFreeEdge = ResolveFreeEdge(hingeEdge, freeEdge, direction);
        var resolvedHingeEdge = ResolveHingeEdge(hingeEdge, resolvedFreeEdge, direction);
        var localPageSize = ToLocalPageSize(pageSize, resolvedFreeEdge);
        var localDragStart = ToFreeLocal(dragStart, pageSize, resolvedFreeEdge);
        var localPointer = ToFreeLocal(pointer, pageSize, resolvedFreeEdge);
        var clampedProgress = Math.Clamp(progress, 0, 1);
        // During a settle the finger is no longer moving, so reconstruct the
        // curl edge from the animated progress. During a drag this is the same
        // local x-coordinate that the controller derived from the pointer.
        var animatedPointer = new Vector2(
            localPageSize.X * (1f - clampedProgress),
            localPointer.Y);
        var delta = animatedPointer - localDragStart;
        var distance = delta.Length();
        var curlDirection = distance > Epsilon
            ? Vector2.Normalize(delta)
            : -Vector2.UnitX;

        var baseRadius = MathF.Max(localPageSize.X * options.RadiusScale, 1f);
        var curlLength = MathF.PI * baseRadius;
        var radius = baseRadius;
        var maximumDistance = MathF.Max(localPageSize.X * 2f - curlLength, baseRadius);
        if (distance > maximumDistance)
        {
            radius = MathF.Max((localPageSize.X * 2f - distance) / MathF.PI, 1f);
            curlLength = MathF.PI * radius;
        }

        var position = animatedPointer;
        if (distance > curlLength)
        {
            position -= curlDirection * ((distance - curlLength) * 0.5f);
        }

        var screenPosition = FromFreeLocal(position, pageSize, resolvedFreeEdge);
        var screenDirection = FromFreeLocalVector(curlDirection, resolvedFreeEdge);
        return new(
            screenPosition,
            screenDirection,
            radius,
            clampedProgress,
            direction,
            clampedProgress > Epsilon)
        {
            HingeEdge = resolvedHingeEdge,
            FreeEdge = resolvedFreeEdge,
        };
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

        var freeEdge = ResolveFreeEdge(pose.HingeEdge, pose.FreeEdge, pose.TurnDirection);
        var hingeEdge = ResolveHingeEdge(pose.HingeEdge, freeEdge, pose.TurnDirection);
        var localPosition = ToFreeLocal(pose.Position, pageSize, freeEdge);
        var localDirection = ToFreeLocalVector(pose.Direction, freeEdge);
        if (localDirection.LengthSquared() <= Epsilon)
        {
            WriteFlatPage(buffer, pageSize);
            return;
        }

        var direction = Vector2.Normalize(localDirection);
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
                var sourceLocal = ToFreeLocal(source, pageSize, freeEdge);
                var local = Rotate(sourceLocal - localPosition, cos, -sin);
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

                var worldLocal = Rotate(new Vector2(transformedX, local.Y), cos, sin) + localPosition;
                var curled = FromFreeLocal(worldLocal, pageSize, freeEdge);
                var anchorWeight = HingeWeight(source, pageSize, hingeEdge);
                var world = source + ((curled - source) * anchorWeight);
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

    private static PageEdge ResolveFreeEdge(PageEdge hingeEdge, PageEdge freeEdge, PageTurnDirection direction)
    {
        if (IsPhysicalEdge(freeEdge))
        {
            return freeEdge;
        }

        if (IsPhysicalEdge(hingeEdge))
        {
            return Opposite(hingeEdge);
        }

        return direction == PageTurnDirection.Forward ? PageEdge.Right : PageEdge.Left;
    }

    private static PageEdge ResolveHingeEdge(PageEdge hingeEdge, PageEdge freeEdge, PageTurnDirection direction)
    {
        if (IsPhysicalEdge(hingeEdge))
        {
            return hingeEdge;
        }

        if (IsPhysicalEdge(freeEdge))
        {
            return Opposite(freeEdge);
        }

        return direction == PageTurnDirection.Forward ? PageEdge.Left : PageEdge.Right;
    }

    private static Vector2 ToLocalPageSize(Vector2 pageSize, PageEdge freeEdge) => freeEdge switch
    {
        PageEdge.Top or PageEdge.Bottom => new(pageSize.Y, pageSize.X),
        _ => pageSize,
    };

    private static Vector2 ToFreeLocal(Vector2 point, Vector2 pageSize, PageEdge freeEdge) => freeEdge switch
    {
        PageEdge.Right => point,
        PageEdge.Left => new(pageSize.X - point.X, point.Y),
        PageEdge.Top => new(pageSize.Y - point.Y, point.X),
        PageEdge.Bottom => new(point.Y, point.X),
        _ => point,
    };

    private static Vector2 FromFreeLocal(Vector2 point, Vector2 pageSize, PageEdge freeEdge) => freeEdge switch
    {
        PageEdge.Right => point,
        PageEdge.Left => new(pageSize.X - point.X, point.Y),
        PageEdge.Top => new(point.Y, pageSize.Y - point.X),
        PageEdge.Bottom => new(point.Y, point.X),
        _ => point,
    };

    private static Vector2 ToFreeLocalVector(Vector2 vector, PageEdge freeEdge) => freeEdge switch
    {
        PageEdge.Right => vector,
        PageEdge.Left => new(-vector.X, vector.Y),
        PageEdge.Top => new(-vector.Y, vector.X),
        PageEdge.Bottom => new(vector.Y, vector.X),
        _ => vector,
    };

    private static Vector2 FromFreeLocalVector(Vector2 vector, PageEdge freeEdge) => freeEdge switch
    {
        PageEdge.Right => vector,
        PageEdge.Left => new(-vector.X, vector.Y),
        PageEdge.Top => new(vector.Y, -vector.X),
        PageEdge.Bottom => new(vector.Y, vector.X),
        _ => vector,
    };

    private static float HingeWeight(Vector2 source, Vector2 pageSize, PageEdge hingeEdge) => hingeEdge switch
    {
        PageEdge.Left => source.X / pageSize.X,
        PageEdge.Right => (pageSize.X - source.X) / pageSize.X,
        PageEdge.Top => source.Y / pageSize.Y,
        PageEdge.Bottom => (pageSize.Y - source.Y) / pageSize.Y,
        _ => 1,
    };

    private static bool IsPhysicalEdge(PageEdge edge) => edge is
        PageEdge.Top or PageEdge.Right or PageEdge.Bottom or PageEdge.Left;

    private static PageEdge Opposite(PageEdge edge) => edge switch
    {
        PageEdge.Top => PageEdge.Bottom,
        PageEdge.Right => PageEdge.Left,
        PageEdge.Bottom => PageEdge.Top,
        PageEdge.Left => PageEdge.Right,
        _ => PageEdge.Auto,
    };

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
