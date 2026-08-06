using System.Numerics;

namespace PageCurl.Core;

/// <summary>Builds a reusable, shaded developable page curl mesh.</summary>
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
        var flatRadius = MathF.Max(localPageSize.X * 4f, baseRadius);
        var curlAmount = MathF.Sin(MathF.PI * clampedProgress);
        var radius = baseRadius + ((flatRadius - baseRadius) * (1f - curlAmount));
        var curlLength = MathF.PI * radius;

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
            DragStart = dragStart,
            HasDragStart = true,
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
        var grabSource = pose.HasDragStart && IsFinite(pose.DragStart)
            ? pose.DragStart
            : EdgeCenter(freeEdge, pageSize);
        var drag = grabSource - pose.Position;
        if (drag.LengthSquared() <= Epsilon)
        {
            drag = -pose.Direction;
        }

        var direction = Vector2.Normalize(drag);
        var axis = new Vector2(-direction.Y, direction.X);
        var minimumRadius = MathF.Min(pageSize.X, pageSize.Y) * 0.06f;
        var radius = MathF.Max(pose.Radius, minimumRadius);
        var grabProjection = Vector2.Dot(grabSource, direction);
        var requestedTravel = Vector2.Distance(grabSource, pose.Position);
        var hingeProjection = MaximumEdgeProjection(hingeEdge, pageSize, direction);
        var maximumArcLength = MathF.Max(0, grabProjection - hingeProjection);
        radius = FitRadiusToTravel(maximumArcLength, requestedTravel, radius, minimumRadius);
        var curlLength = MathF.PI * radius;
        var arcLength = MathF.Min(SolveArcLength(requestedTravel, radius), maximumArcLength);
        var curlStart = grabProjection - arcLength;
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
                var sourceProjection = Vector2.Dot(source, direction);
                var axisProjection = Vector2.Dot(source, axis);
                var distanceIntoCurl = sourceProjection - curlStart;
                var transformedProjection = sourceProjection;
                var depth = 0f;
                var shade = 1f;

                if (distanceIntoCurl >= curlLength)
                {
                    transformedProjection = curlStart - (distanceIntoCurl - curlLength);
                    depth = radius * 2f;
                    shade = 0.35f;
                }
                else if (distanceIntoCurl > 0)
                {
                    var theta = distanceIntoCurl / radius;
                    transformedProjection = curlStart + (radius * MathF.Sin(theta));
                    depth = radius - (radius * MathF.Cos(theta));
                    shade = 0.15f + (0.85f * MathF.Max(0, MathF.Cos(theta * 0.75f)));
                }

                var world = (direction * transformedProjection) + (axis * axisProjection);
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

    private static float SolveArcLength(float screenTravel, float radius)
    {
        var halfTurnLength = MathF.PI * radius;
        if (screenTravel >= halfTurnLength)
        {
            return (screenTravel + halfTurnLength) * 0.5f;
        }

        var lower = 0f;
        var upper = halfTurnLength;
        for (var iteration = 0; iteration < 16; iteration++)
        {
            var candidate = (lower + upper) * 0.5f;
            var projectedTravel = candidate - (radius * MathF.Sin(candidate / radius));
            if (projectedTravel < screenTravel)
            {
                lower = candidate;
            }
            else
            {
                upper = candidate;
            }
        }

        return (lower + upper) * 0.5f;
    }

    private static float FitRadiusToTravel(
        float availableArcLength,
        float requestedTravel,
        float preferredRadius,
        float minimumRadius)
    {
        if (ProjectedTravel(availableArcLength, preferredRadius) >= requestedTravel)
        {
            return preferredRadius;
        }

        if (ProjectedTravel(availableArcLength, minimumRadius) <= requestedTravel)
        {
            return minimumRadius;
        }

        var lower = minimumRadius;
        var upper = preferredRadius;
        for (var iteration = 0; iteration < 16; iteration++)
        {
            var candidate = (lower + upper) * 0.5f;
            if (ProjectedTravel(availableArcLength, candidate) > requestedTravel)
            {
                lower = candidate;
            }
            else
            {
                upper = candidate;
            }
        }

        return (lower + upper) * 0.5f;
    }

    private static float ProjectedTravel(float arcLength, float radius)
    {
        var halfTurnLength = MathF.PI * radius;
        return arcLength <= halfTurnLength
            ? arcLength - (radius * MathF.Sin(arcLength / radius))
            : (2f * arcLength) - halfTurnLength;
    }

    private static float MaximumEdgeProjection(PageEdge edge, Vector2 pageSize, Vector2 direction)
    {
        var (first, second) = EdgeEndpoints(edge, pageSize);
        return MathF.Max(Vector2.Dot(first, direction), Vector2.Dot(second, direction));
    }

    private static (Vector2 First, Vector2 Second) EdgeEndpoints(PageEdge edge, Vector2 pageSize) => edge switch
    {
        PageEdge.Top => (Vector2.Zero, new Vector2(pageSize.X, 0)),
        PageEdge.Right => (new Vector2(pageSize.X, 0), pageSize),
        PageEdge.Bottom => (new Vector2(0, pageSize.Y), pageSize),
        PageEdge.Left => (Vector2.Zero, new Vector2(0, pageSize.Y)),
        _ => (Vector2.Zero, Vector2.Zero),
    };

    private static Vector2 EdgeCenter(PageEdge edge, Vector2 pageSize)
    {
        var (first, second) = EdgeEndpoints(edge, pageSize);
        return (first + second) * 0.5f;
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

    private static bool IsValidPageSize(Vector2 value) =>
        IsFinite(value) && value.X > 0 && value.Y > 0;

    private static bool IsFinite(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y);
}
