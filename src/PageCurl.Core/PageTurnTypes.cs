using System.Numerics;

namespace PageCurl.Core;

/// <summary>The edge from which a page turn started.</summary>
public enum PageTurnDirection
{
    Forward,
    Backward,
}

/// <summary>The physical edge that anchors or starts a page curl.</summary>
public enum PageEdge
{
    Auto,
    Top,
    Right,
    Bottom,
    Left,
}

/// <summary>The interaction/animation phase of a page turn.</summary>
public enum PageTurnState
{
    Idle,
    Dragging,
    Settling,
}

/// <summary>
/// Owns one pointer for the duration of a page gesture so another finger
/// cannot move or release the active turn.
/// </summary>
public sealed class PageTurnPointerCapture
{
    private long? _activePointerId;

    public bool TryCapture(long pointerId)
    {
        if (_activePointerId.HasValue)
        {
            return false;
        }

        _activePointerId = pointerId;
        return true;
    }

    public bool Owns(long pointerId) => _activePointerId == pointerId;

    public bool Release(long pointerId)
    {
        if (!Owns(pointerId))
        {
            return false;
        }

        _activePointerId = null;
        return true;
    }

    public void Cancel() => _activePointerId = null;
}

/// <summary>Options shared by the interaction controller and the mesh builder.</summary>
public sealed class PageCurlOptions
{
    public int MeshColumns { get; init; } = 24;

    public int MeshRows { get; init; } = 24;

    /// <summary>Base cylinder radius as a fraction of page width.</summary>
    public float RadiusScale { get; init; } = 1f / 3f;

    /// <summary>Release progress at or above which the page is committed.</summary>
    public float SnapThreshold { get; init; } = 0.5f;

    /// <summary>Angular frequency used by the critically damped settle animation.</summary>
    public float SpringFrequency { get; init; } = 18f;

    internal void Validate()
    {
        if (MeshColumns < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(MeshColumns), "At least two mesh columns are required.");
        }

        if (MeshRows < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(MeshRows), "At least one mesh row is required.");
        }

        if (!float.IsFinite(RadiusScale) || RadiusScale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(RadiusScale), "RadiusScale must be finite and positive.");
        }

        if (!float.IsFinite(SnapThreshold) || SnapThreshold is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(SnapThreshold), "SnapThreshold must be between zero and one.");
        }

        if (!float.IsFinite(SpringFrequency) || SpringFrequency <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(SpringFrequency), "SpringFrequency must be finite and positive.");
        }
    }
}

/// <summary>The geometry state consumed by a renderer for one frame.</summary>
public readonly record struct CurlPose(
    Vector2 Position,
    Vector2 Direction,
    float Radius,
    float Progress,
    PageTurnDirection TurnDirection,
    bool IsActive)
{
    public PageEdge HingeEdge { get; init; } = PageEdge.Auto;

    public PageEdge FreeEdge { get; init; } = PageEdge.Auto;

    /// <summary>The pointer position projected onto the selected free edge at gesture start.</summary>
    public Vector2 DragStart { get; init; }

    /// <summary>Indicates that <see cref="DragStart"/> is available for renderer anchoring.</summary>
    public bool HasDragStart { get; init; }

    public static CurlPose Inactive(PageTurnDirection direction) =>
        new(Vector2.Zero, Vector2.UnitX, 0, 0, direction, false);
}

/// <summary>A position, UV and lighting value for one mesh vertex.</summary>
public readonly record struct MeshVertex(
    Vector2 Position,
    Vector2 TextureCoordinate,
    float Shade,
    float Depth);

/// <summary>A read-only snapshot of the page turn controller.</summary>
public readonly record struct PageTurnSnapshot(
    PageTurnState State,
    PageTurnDirection Direction,
    Vector2 DragStartPosition,
    Vector2 PointerPosition,
    float Progress,
    float TargetProgress)
{
    /// <summary>The physical edge currently acting as the stationary hinge.</summary>
    public PageEdge HingeEdge { get; init; } = PageEdge.Auto;

    /// <summary>The physical edge or corner the user is pulling from.</summary>
    public PageEdge FreeEdge { get; init; } = PageEdge.Auto;
}

/// <summary>Raised once when a release animation reaches its endpoint.</summary>
public sealed class PageTurnCompletedEventArgs : EventArgs
{
    public PageTurnCompletedEventArgs(PageTurnDirection direction, bool committed)
    {
        Direction = direction;
        Committed = committed;
    }

    public PageTurnDirection Direction { get; }

    public bool Committed { get; }
}

/// <summary>Reusable storage for the front and back triangle streams.</summary>
public sealed class PageMeshBuffer
{
    internal readonly int Columns;
    internal readonly int Rows;
    internal readonly GridVertex[] Grid;
    internal readonly TriangleOrder[] Orders;

    public PageMeshBuffer(PageCurlOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        Columns = options.MeshColumns;
        Rows = options.MeshRows;
        Grid = new GridVertex[(Columns + 1) * (Rows + 1)];
        Orders = new TriangleOrder[Columns * Rows * 2];

        var vertexCapacity = Math.Max(6, Orders.Length * 3);
        FrontVertices = new MeshVertex[vertexCapacity];
        BackVertices = new MeshVertex[vertexCapacity];
    }

    public MeshVertex[] FrontVertices { get; }

    public MeshVertex[] BackVertices { get; }

    public int FrontVertexCount { get; internal set; }

    public int BackVertexCount { get; internal set; }

    internal readonly struct GridVertex
    {
        public GridVertex(Vector2 position, Vector2 textureCoordinate, float shade, float depth)
        {
            Position = position;
            TextureCoordinate = textureCoordinate;
            Shade = shade;
            Depth = depth;
        }

        public Vector2 Position { get; }

        public Vector2 TextureCoordinate { get; }

        public float Shade { get; }

        public float Depth { get; }
    }

    internal struct TriangleOrder
    {
        public int A;
        public int B;
        public int C;
        public float Depth;
        public bool IsBack;
    }
}
