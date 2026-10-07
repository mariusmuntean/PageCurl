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

/// <summary>A stationary page edge, pinned corner, or automatic selection.</summary>
public enum PageAnchor
{
    Auto,
    Top,
    Right,
    Bottom,
    Left,
    TopLeft,
    TopRight,
    BottomRight,
    BottomLeft,
}

/// <summary>Allocation-free conversions and positions for page anchors.</summary>
public static class PageAnchorExtensions
{
    public static PageAnchor FromEdge(PageEdge edge) => edge switch
    {
        PageEdge.Auto => PageAnchor.Auto,
        PageEdge.Top => PageAnchor.Top,
        PageEdge.Right => PageAnchor.Right,
        PageEdge.Bottom => PageAnchor.Bottom,
        PageEdge.Left => PageAnchor.Left,
        _ => throw new ArgumentOutOfRangeException(nameof(edge)),
    };

    public static bool TryGetEdge(this PageAnchor anchor, out PageEdge edge)
    {
        edge = anchor switch
        {
            PageAnchor.Auto => PageEdge.Auto,
            PageAnchor.Top => PageEdge.Top,
            PageAnchor.Right => PageEdge.Right,
            PageAnchor.Bottom => PageEdge.Bottom,
            PageAnchor.Left => PageEdge.Left,
            _ => PageEdge.Auto,
        };
        return anchor is PageAnchor.Auto
            or PageAnchor.Top
            or PageAnchor.Right
            or PageAnchor.Bottom
            or PageAnchor.Left;
    }

    public static bool IsCorner(this PageAnchor anchor) => anchor is
        PageAnchor.TopLeft
        or PageAnchor.TopRight
        or PageAnchor.BottomRight
        or PageAnchor.BottomLeft;

    public static Vector2 GetPosition(this PageAnchor anchor, Vector2 pageSize) => anchor switch
    {
        PageAnchor.Auto => pageSize * 0.5f,
        PageAnchor.Top => new(pageSize.X * 0.5f, 0),
        PageAnchor.Right => new(pageSize.X, pageSize.Y * 0.5f),
        PageAnchor.Bottom => new(pageSize.X * 0.5f, pageSize.Y),
        PageAnchor.Left => new(0, pageSize.Y * 0.5f),
        PageAnchor.TopLeft => Vector2.Zero,
        PageAnchor.TopRight => new(pageSize.X, 0),
        PageAnchor.BottomRight => pageSize,
        PageAnchor.BottomLeft => new(0, pageSize.Y),
        _ => throw new ArgumentOutOfRangeException(nameof(anchor)),
    };
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
    /// <summary>Maximum number of cells in a reusable page mesh.</summary>
    public const int MaximumMeshCellCount = 16_384;

    private float _pageChangeThreshold = 0.5f;
    private float _flickVelocityThreshold = 1.5f;

    /// <summary>Horizontal mesh resolution. The column/row product may not exceed <see cref="MaximumMeshCellCount"/>.</summary>
    public int MeshColumns { get; init; } = 64;

    /// <summary>Vertical mesh resolution. The column/row product may not exceed <see cref="MaximumMeshCellCount"/>.</summary>
    public int MeshRows { get; init; } = 64;

    /// <summary>Turn a hinged sheet across the opposite page; progress spans twice the grab-to-hinge distance.</summary>
    public bool TurnAcrossHinge { get; init; }

    /// <summary>Base cylinder radius as a fraction of page width.</summary>
    public float RadiusScale { get; init; } = 1f / 3f;

    /// <summary>Page-travel fraction at or above which release commits the turn.</summary>
    public float PageChangeThreshold
    {
        get => _pageChangeThreshold;
        set
        {
            if (!float.IsFinite(value) || value is < 0 or > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(PageChangeThreshold),
                    "PageChangeThreshold must be between zero and one.");
            }
            _pageChangeThreshold = value;
        }
    }

    /// <summary>Compatibility alias for <see cref="PageChangeThreshold"/>.</summary>
    public float SnapThreshold
    {
        get => _pageChangeThreshold;
        set => PageChangeThreshold = value;
    }

    /// <summary>Whether release velocity may override the page-change threshold.</summary>
    public bool FlickEnabled { get; set; } = true;

    /// <summary>Normalized page-progress velocity required to recognize a flick.</summary>
    public float FlickVelocityThreshold
    {
        get => _flickVelocityThreshold;
        set
        {
            if (!float.IsFinite(value) || value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(FlickVelocityThreshold),
                    "FlickVelocityThreshold must be finite and positive.");
            }
            _flickVelocityThreshold = value;
        }
    }

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

        var cellCount = checked((long)MeshColumns * MeshRows);
        if (cellCount > MaximumMeshCellCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MeshColumns),
                $"MeshColumns multiplied by MeshRows may not exceed {MaximumMeshCellCount} cells.");
        }

        if (!float.IsFinite(RadiusScale) || RadiusScale <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(RadiusScale), "RadiusScale must be finite and positive.");
        }

        if (!float.IsFinite(SpringFrequency) || SpringFrequency <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(SpringFrequency), "SpringFrequency must be finite and positive.");
        }
    }
}

/// <summary>Raised when a drag enters or leaves page-change intent.</summary>
public sealed class PageChangeThresholdCrossedEventArgs : EventArgs
{
    public PageChangeThresholdCrossedEventArgs(
        PageTurnDirection direction,
        bool isCommitCandidate,
        float progress)
    {
        if (direction is not (PageTurnDirection.Forward or PageTurnDirection.Backward))
        {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }

        if (!float.IsFinite(progress) || progress is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(progress));
        }

        Direction = direction;
        IsCommitCandidate = isCommitCandidate;
        Progress = progress;
    }

    public PageTurnDirection Direction { get; }

    public bool IsCommitCandidate { get; }

    public float Progress { get; }
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
    public PageAnchor LockedAnchor { get; init; } = PageAnchor.Auto;

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
    /// <summary>The edge or corner that remains stationary during the turn.</summary>
    public PageAnchor LockedAnchor { get; init; } = PageAnchor.Auto;

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
    internal readonly int BaseGridVertexCount;
    internal readonly GridVertex[] Grid;
    internal readonly TriangleOrder[] Orders;

    public PageMeshBuffer(PageCurlOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        Columns = options.MeshColumns;
        Rows = options.MeshRows;
        BaseGridVertexCount = checked((Columns + 1) * (Rows + 1));
        var cellCount = checked(Columns * Rows);
        // The cylinder's single straight facing seam crosses at most one
        // row plus one column of cells. Only those cells need clipped vertices
        // and extra triangles; a cone uses just the original grid.
        var seamCellCount = checked(Columns + Rows);
        Grid = new GridVertex[checked(BaseGridVertexCount + (seamCellCount * 8))];
        Orders = new TriangleOrder[checked((cellCount * 2) + (seamCellCount * 4))];
        var vertexCapacity = Math.Max(6, checked(Orders.Length * 3));
        FrontVertices = new MeshVertex[vertexCapacity];
        BackVertices = new MeshVertex[vertexCapacity];
    }

    public MeshVertex[] FrontVertices { get; }

    public MeshVertex[] BackVertices { get; }

    public int FrontVertexCount { get; internal set; }

    public int BackVertexCount { get; internal set; }

    internal readonly struct GridVertex
    {
        public GridVertex(
            Vector2 position,
            Vector2 textureCoordinate,
            float shade,
            float depth,
            float facing)
        {
            Position = position;
            TextureCoordinate = textureCoordinate;
            Shade = shade;
            Depth = depth;
            Facing = facing;
        }

        public Vector2 Position { get; }

        public Vector2 TextureCoordinate { get; }

        public float Shade { get; }

        public float Depth { get; }

        public float Facing { get; }
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
