using System.Numerics;

namespace PageCurl.Core;

/// <summary>
/// Tracks a finger drag and settles it to the nearest page endpoint using a
/// critically damped spring. It has no UI or platform dependencies.
/// </summary>
public sealed class PageTurnController
{
    private const float EdgeEpsilon = 0.001f;
    private const float ReleaseFlickThreshold = 1.5f;
    private const float VelocitySmoothingFactor = 0.5f;
    private const float MaximumReleaseVelocity = 8f;
    private const double MaximumDragSampleIntervalSeconds = 0.25;
    private readonly PageCurlOptions _options;
    private PageTurnState _state;
    private PageTurnDirection _direction;
    private PageEdge _hingeEdge = PageEdge.Auto;
    private PageEdge _freeEdge = PageEdge.Auto;
    private PageEdge _lockedEdge = PageEdge.Auto;
    private bool _edgeSelectionPending;
    private Vector2 _pageSize;
    private Vector2 _dragStartPosition;
    private Vector2 _initialPointerPosition;
    private Vector2 _pointerPosition;
    private float _progress;
    private float _targetProgress;
    private float _velocity;
    private double _lastDragTimestamp;
    private float _lastSampledProgress;
    private float _smoothedProgressVelocity;
    private bool _hasDragTimestamp;
    private bool _completionRaised;

    public PageTurnController(PageCurlOptions? options = null)
    {
        _options = options ?? new PageCurlOptions();
        _options.Validate();
        _state = PageTurnState.Idle;
        _direction = PageTurnDirection.Forward;
    }

    public event EventHandler<PageTurnCompletedEventArgs>? PageTurnCompleted;

    public PageTurnSnapshot Snapshot => new(
        _state,
        _direction,
        _dragStartPosition,
        _pointerPosition,
        _progress,
        _targetProgress)
    {
        HingeEdge = _hingeEdge,
        FreeEdge = _freeEdge,
    };

    public bool BeginDrag(Vector2 pointer, Vector2 pageSize)
    {
        if (!IsValidPageSize(pageSize) || !IsFinite(pointer))
        {
            return false;
        }

        _pageSize = pageSize;
        var clampedPointer = ClampToPage(pointer, pageSize);
        _direction = clampedPointer.X >= pageSize.X * 0.5f
            ? PageTurnDirection.Forward
            : PageTurnDirection.Backward;
        _hingeEdge = _direction == PageTurnDirection.Forward ? PageEdge.Left : PageEdge.Right;
        _freeEdge = _direction == PageTurnDirection.Forward ? PageEdge.Right : PageEdge.Left;
        _lockedEdge = _hingeEdge;
        _edgeSelectionPending = false;
        _initialPointerPosition = clampedPointer;
        _dragStartPosition = new(
            _direction == PageTurnDirection.Forward ? pageSize.X : 0,
            clampedPointer.Y);
        _pointerPosition = clampedPointer;
        _progress = 0;
        _targetProgress = 0;
        _velocity = 0;
        ResetDragVelocity();
        _completionRaised = false;
        _state = PageTurnState.Dragging;
        return true;
    }

    /// <summary>
    /// Begins a drag using a physical edge-aware interaction model. The
    /// locked edge is the stationary hinge; <see cref="PageEdge.Auto"/>
    /// chooses the nearest free edge and its opposite hinge.
    /// </summary>
    public bool BeginDrag(Vector2 pointer, Vector2 pageSize, PageEdge lockedEdge)
    {
        if (!IsValidPageSize(pageSize) || !IsFinite(pointer) || !IsValidEdge(lockedEdge))
        {
            return false;
        }

        var clampedPointer = ClampToPage(pointer, pageSize);
        var nearestEdge = SelectNearestEdge(clampedPointer, pageSize);
        if (lockedEdge != PageEdge.Auto && IsOnEdge(clampedPointer, pageSize, lockedEdge))
        {
            return false;
        }

        _pageSize = pageSize;
        _lockedEdge = lockedEdge;
        _direction = clampedPointer.X >= pageSize.X * 0.5f
            ? PageTurnDirection.Forward
            : PageTurnDirection.Backward;
        _initialPointerPosition = clampedPointer;
        _pointerPosition = clampedPointer;
        _progress = 0;
        _targetProgress = 0;
        _velocity = 0;
        ResetDragVelocity();
        _completionRaised = false;

        _edgeSelectionPending = lockedEdge == PageEdge.Auto && IsCorner(clampedPointer, pageSize);
        if (_edgeSelectionPending)
        {
            _freeEdge = PageEdge.Auto;
            _hingeEdge = PageEdge.Auto;
            _dragStartPosition = clampedPointer;
        }
        else
        {
            _freeEdge = lockedEdge == PageEdge.Auto
                ? nearestEdge
                : SelectNearestFreeEdge(clampedPointer, pageSize, lockedEdge);
            if (_freeEdge == PageEdge.Auto)
            {
                return false;
            }

            SelectEdges(_freeEdge);
            _dragStartPosition = ProjectToEdge(clampedPointer, pageSize, _freeEdge);
        }

        _state = PageTurnState.Dragging;
        return true;
    }

    public void UpdateDrag(Vector2 pointer, double timestampSeconds = 0)
    {
        if (_state != PageTurnState.Dragging || !IsFinite(pointer))
        {
            return;
        }

        _pointerPosition = ClampToPage(pointer, _pageSize);
        if (_edgeSelectionPending)
        {
            var selectedEdge = SelectCornerEdge(
                _initialPointerPosition,
                _pointerPosition - _initialPointerPosition,
                _pageSize,
                _lockedEdge);
            if (selectedEdge != PageEdge.Auto)
            {
                _edgeSelectionPending = false;
                _freeEdge = selectedEdge;
                SelectEdges(selectedEdge);
                _dragStartPosition = ProjectToEdge(
                    _initialPointerPosition,
                    _pageSize,
                    selectedEdge);
            }
        }

        _progress = _freeEdge == PageEdge.Auto
            ? 0
            : CalculateProgress(_pointerPosition, _pageSize, _freeEdge);
        _progress = Math.Clamp(_progress, 0, 1);
        _targetProgress = _progress;
        UpdateProgressVelocity(timestampSeconds);
    }

    public void EndDrag(bool animate = true)
    {
        if (_state != PageTurnState.Dragging)
        {
            return;
        }

        _targetProgress = _smoothedProgressVelocity > ReleaseFlickThreshold
            ? 1
            : _smoothedProgressVelocity < -ReleaseFlickThreshold
                ? 0
                : _progress >= _options.SnapThreshold ? 1 : 0;
        if (!animate)
        {
            CompleteAtTarget();
            return;
        }

        BeginSettling();
    }

    /// <summary>Starts a full page turn without requiring pointer input.</summary>
    public bool BeginProgrammaticTurn(Vector2 pageSize, PageTurnDirection direction, bool animate = true)
    {
        if (_state != PageTurnState.Idle || !IsValidPageSize(pageSize) ||
            direction is not (PageTurnDirection.Forward or PageTurnDirection.Backward))
        {
            return false;
        }

        _pageSize = pageSize;
        _direction = direction;
        _hingeEdge = direction == PageTurnDirection.Forward ? PageEdge.Left : PageEdge.Right;
        _freeEdge = direction == PageTurnDirection.Forward ? PageEdge.Right : PageEdge.Left;
        _lockedEdge = _hingeEdge;
        _edgeSelectionPending = false;
        _dragStartPosition = new(
            direction == PageTurnDirection.Forward ? pageSize.X : 0,
            pageSize.Y * 0.5f);
        _initialPointerPosition = _dragStartPosition;
        _pointerPosition = _dragStartPosition;
        _progress = 0;
        _targetProgress = 1;
        _velocity = 0;
        ResetDragVelocity();
        _completionRaised = false;

        if (!animate)
        {
            CompleteAtTarget();
        }
        else
        {
            BeginSettling();
        }

        return true;
    }

    public void Cancel()
    {
        if (_state == PageTurnState.Idle)
        {
            return;
        }

        _targetProgress = 0;
        ResetDragVelocity();
        BeginSettling();
    }

    /// <summary>Advances the settle animation. Returns true while it needs frames.</summary>
    public bool Tick(double elapsedSeconds)
    {
        if (_state != PageTurnState.Settling)
        {
            return false;
        }

        var dt = float.IsFinite((float)elapsedSeconds)
            ? Math.Clamp((float)elapsedSeconds, 0, 0.1f)
            : 0;
        if (dt > 0)
        {
            var omega = _options.SpringFrequency;
            var error = _progress - _targetProgress;
            var c = _velocity + (omega * error);
            var decay = MathF.Exp(-omega * dt);
            var nextError = (error + (c * dt)) * decay;
            _velocity = (_velocity - (omega * c * dt)) * decay;
            _progress = Math.Clamp(_targetProgress + nextError, 0, 1);
        }

        if (MathF.Abs(_progress - _targetProgress) <= 0.001f && MathF.Abs(_velocity) <= 0.01f)
        {
            CompleteAtTarget();
            return false;
        }

        return true;
    }

    public CurlPose GetCurlPose(Vector2 pageSize)
    {
        if (_state == PageTurnState.Idle || !IsValidPageSize(pageSize))
        {
            return CurlPose.Inactive(_direction);
        }

        return PageCurlGeometry.ComputePose(
            pageSize,
            _dragStartPosition,
            _pointerPosition,
            _hingeEdge,
            _freeEdge,
            _direction,
            _progress,
            _options);
    }

    private void BeginSettling()
    {
        var releaseVelocity = float.IsFinite(_smoothedProgressVelocity)
            ? _smoothedProgressVelocity
            : 0;
        _velocity = _targetProgress >= 0.5f
            ? MathF.Max(0, releaseVelocity)
            : MathF.Min(0, releaseVelocity);
        _velocity = Math.Clamp(_velocity, -MaximumReleaseVelocity, MaximumReleaseVelocity);
        _completionRaised = false;
        _state = PageTurnState.Settling;
    }

    private void CompleteAtTarget()
    {
        _progress = _targetProgress;
        _velocity = 0;
        ResetDragVelocity();
        _state = PageTurnState.Idle;
        if (!_completionRaised)
        {
            _completionRaised = true;
            PageTurnCompleted?.Invoke(
                this,
                new PageTurnCompletedEventArgs(_direction, _targetProgress >= 0.5f));
        }
    }

    private void ResetDragVelocity()
    {
        _lastDragTimestamp = 0;
        _lastSampledProgress = 0;
        _smoothedProgressVelocity = 0;
        _hasDragTimestamp = false;
    }

    private void UpdateProgressVelocity(double timestampSeconds)
    {
        if (!double.IsFinite(timestampSeconds))
        {
            _smoothedProgressVelocity = 0;
            _hasDragTimestamp = false;
            return;
        }

        if (!_hasDragTimestamp)
        {
            _lastDragTimestamp = timestampSeconds;
            _lastSampledProgress = _progress;
            _hasDragTimestamp = true;
            return;
        }

        var interval = timestampSeconds - _lastDragTimestamp;
        if (!double.IsFinite(interval) || interval <= 0 || interval > MaximumDragSampleIntervalSeconds)
        {
            _lastDragTimestamp = timestampSeconds;
            _lastSampledProgress = _progress;
            _smoothedProgressVelocity = 0;
            return;
        }

        var sampledVelocity = (_progress - _lastSampledProgress) / (float)interval;
        if (!float.IsFinite(sampledVelocity))
        {
            return;
        }

        _smoothedProgressVelocity = Math.Clamp(
            ((_smoothedProgressVelocity * (1 - VelocitySmoothingFactor))
                + (sampledVelocity * VelocitySmoothingFactor)),
            -MaximumReleaseVelocity,
            MaximumReleaseVelocity);
        _lastDragTimestamp = timestampSeconds;
        _lastSampledProgress = _progress;
    }

    private static bool IsValidPageSize(Vector2 pageSize) =>
        IsFinite(pageSize) && pageSize.X > 0 && pageSize.Y > 0;

    private static bool IsValidEdge(PageEdge edge) =>
        edge is PageEdge.Auto or PageEdge.Top or PageEdge.Right or PageEdge.Bottom or PageEdge.Left;

    private static bool IsFinite(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y);

    private static Vector2 ClampToPage(Vector2 value, Vector2 pageSize) =>
        new(Math.Clamp(value.X, 0, pageSize.X), Math.Clamp(value.Y, 0, pageSize.Y));

    private void SelectEdges(PageEdge freeEdge)
    {
        _freeEdge = freeEdge;
        _hingeEdge = _lockedEdge == PageEdge.Auto ? Opposite(freeEdge) : _lockedEdge;
    }

    private static PageEdge SelectNearestEdge(Vector2 pointer, Vector2 pageSize)
    {
        var top = pointer.Y;
        var right = pageSize.X - pointer.X;
        var bottom = pageSize.Y - pointer.Y;
        var left = pointer.X;
        var minimum = MathF.Min(MathF.Min(top, right), MathF.Min(bottom, left));

        if (MathF.Abs(top - minimum) <= EdgeEpsilon)
        {
            return PageEdge.Top;
        }

        if (MathF.Abs(right - minimum) <= EdgeEpsilon)
        {
            return PageEdge.Right;
        }

        if (MathF.Abs(bottom - minimum) <= EdgeEpsilon)
        {
            return PageEdge.Bottom;
        }

        return PageEdge.Left;
    }

    private static PageEdge SelectNearestFreeEdge(Vector2 pointer, Vector2 pageSize, PageEdge lockedEdge)
    {
        var selected = PageEdge.Auto;
        var minimum = float.PositiveInfinity;
        var top = pointer.Y;
        var right = pageSize.X - pointer.X;
        var bottom = pageSize.Y - pointer.Y;
        var left = pointer.X;

        Consider(PageEdge.Top, top, lockedEdge, ref selected, ref minimum);
        Consider(PageEdge.Right, right, lockedEdge, ref selected, ref minimum);
        Consider(PageEdge.Bottom, bottom, lockedEdge, ref selected, ref minimum);
        Consider(PageEdge.Left, left, lockedEdge, ref selected, ref minimum);
        return selected;
    }

    private static void Consider(
        PageEdge edge,
        float distance,
        PageEdge lockedEdge,
        ref PageEdge selected,
        ref float minimum)
    {
        if (edge == lockedEdge || distance >= minimum)
        {
            return;
        }

        selected = edge;
        minimum = distance;
    }

    private static PageEdge SelectCornerEdge(
        Vector2 corner,
        Vector2 movement,
        Vector2 pageSize,
        PageEdge lockedEdge)
    {
        if (movement.LengthSquared() <= EdgeEpsilon * EdgeEpsilon)
        {
            return PageEdge.Auto;
        }

        var horizontal = MathF.Abs(movement.X) >= MathF.Abs(movement.Y);
        var preferred = horizontal
            ? movement.X >= 0 ? PageEdge.Left : PageEdge.Right
            : movement.Y >= 0 ? PageEdge.Top : PageEdge.Bottom;
        if (preferred != lockedEdge)
        {
            return preferred;
        }

        var alternate = horizontal
            ? movement.Y >= 0 ? PageEdge.Top : PageEdge.Bottom
            : movement.X >= 0 ? PageEdge.Left : PageEdge.Right;
        if (alternate != lockedEdge)
        {
            return alternate;
        }

        return SelectNearestFreeEdge(corner, pageSize, lockedEdge);
    }

    private static PageEdge Opposite(PageEdge edge) => edge switch
    {
        PageEdge.Top => PageEdge.Bottom,
        PageEdge.Right => PageEdge.Left,
        PageEdge.Bottom => PageEdge.Top,
        PageEdge.Left => PageEdge.Right,
        _ => PageEdge.Auto,
    };

    private static bool IsOnEdge(Vector2 pointer, Vector2 pageSize, PageEdge edge) => edge switch
    {
        PageEdge.Top => pointer.Y <= EdgeEpsilon,
        PageEdge.Right => pageSize.X - pointer.X <= EdgeEpsilon,
        PageEdge.Bottom => pageSize.Y - pointer.Y <= EdgeEpsilon,
        PageEdge.Left => pointer.X <= EdgeEpsilon,
        _ => false,
    };

    private static bool IsCorner(Vector2 pointer, Vector2 pageSize)
    {
        var vertical = pointer.Y <= EdgeEpsilon || pageSize.Y - pointer.Y <= EdgeEpsilon;
        var horizontal = pointer.X <= EdgeEpsilon || pageSize.X - pointer.X <= EdgeEpsilon;
        return vertical && horizontal;
    }

    private static Vector2 ProjectToEdge(Vector2 pointer, Vector2 pageSize, PageEdge edge) => edge switch
    {
        PageEdge.Top => new(pointer.X, 0),
        PageEdge.Right => new(pageSize.X, pointer.Y),
        PageEdge.Bottom => new(pointer.X, pageSize.Y),
        PageEdge.Left => new(0, pointer.Y),
        _ => pointer,
    };

    private static float CalculateProgress(Vector2 pointer, Vector2 pageSize, PageEdge freeEdge) => freeEdge switch
    {
        PageEdge.Top => pointer.Y / pageSize.Y,
        PageEdge.Right => (pageSize.X - pointer.X) / pageSize.X,
        PageEdge.Bottom => (pageSize.Y - pointer.Y) / pageSize.Y,
        PageEdge.Left => pointer.X / pageSize.X,
        _ => 0,
    };
}
