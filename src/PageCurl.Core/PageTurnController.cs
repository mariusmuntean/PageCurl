using System.Numerics;

namespace PageCurl.Core;

/// <summary>
/// Tracks a finger drag and settles it to the nearest page endpoint using a
/// critically damped spring. It has no UI or platform dependencies.
/// </summary>
public sealed class PageTurnController
{
    private const float EdgeEpsilon = 0.001f;
    private const float VelocitySmoothingFactor = 0.5f;
    private const float MaximumDragVelocity = 8f;
    private const float MaximumSettlingVelocity = 1.5f;
    private const double MaximumDragSampleIntervalSeconds = 0.25;
    private readonly PageCurlOptions _options;
    private PageTurnState _state;
    private PageTurnDirection _direction;
    private PageEdge _hingeEdge = PageEdge.Auto;
    private PageEdge _freeEdge = PageEdge.Auto;
    private PageEdge _lockedEdge = PageEdge.Auto;
    private PageAnchor _lockedAnchor = PageAnchor.Auto;
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
    private float _gesturePointerProgress;
    private bool _hasDragTimestamp;
    private bool _completionRaised;
    private bool _isCommitCandidate;

    public PageTurnController(PageCurlOptions? options = null)
    {
        _options = options ?? new PageCurlOptions();
        _options.Validate();
        _state = PageTurnState.Idle;
        _direction = PageTurnDirection.Forward;
    }

    public event EventHandler<PageTurnCompletedEventArgs>? PageTurnCompleted;

    public event EventHandler<PageChangeThresholdCrossedEventArgs>? PageChangeThresholdCrossed;

    public PageTurnSnapshot Snapshot => new(
        _state,
        _direction,
        _dragStartPosition,
        _pointerPosition,
        _progress,
        _targetProgress)
    {
        LockedAnchor = _lockedAnchor,
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
        _lockedAnchor = PageAnchorExtensions.FromEdge(_hingeEdge);
        _edgeSelectionPending = false;
        _initialPointerPosition = clampedPointer;
        _dragStartPosition = new(
            _direction == PageTurnDirection.Forward ? pageSize.X : 0,
            clampedPointer.Y);
        _pointerPosition = clampedPointer;
        _progress = 0;
        _targetProgress = 0;
        _velocity = 0;
        _gesturePointerProgress = 0;
        ResetDragVelocity();
        _completionRaised = false;
        ResetCommitCandidate();
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
        if (!IsValidEdge(lockedEdge))
        {
            return false;
        }

        return BeginDrag(pointer, pageSize, PageAnchorExtensions.FromEdge(lockedEdge));
    }

    /// <summary>
    /// Begins a drag with either a stationary edge or a pinned corner.
    /// <see cref="PageAnchor.Auto"/> selects an edge from the gesture start.
    /// </summary>
    public bool BeginDrag(Vector2 pointer, Vector2 pageSize, PageAnchor lockedAnchor)
    {
        if (!IsValidPageSize(pageSize) || !IsFinite(pointer) || !IsValidAnchor(lockedAnchor))
        {
            return false;
        }

        var clampedPointer = ClampToPage(pointer, pageSize);
        var nearestEdge = SelectNearestEdge(clampedPointer, pageSize);
        lockedAnchor.TryGetEdge(out var lockedEdge);
        if (lockedAnchor.IsCorner())
        {
            var pin = lockedAnchor.GetPosition(pageSize);
            if (Vector2.DistanceSquared(clampedPointer, pin) <= EdgeEpsilon * EdgeEpsilon)
            {
                return false;
            }
        }
        else if (lockedEdge != PageEdge.Auto && IsOnEdge(clampedPointer, pageSize, lockedEdge))
        {
            return false;
        }

        _pageSize = pageSize;
        _lockedEdge = lockedEdge;
        _lockedAnchor = lockedAnchor;
        _direction = clampedPointer.X >= pageSize.X * 0.5f
            ? PageTurnDirection.Forward
            : PageTurnDirection.Backward;
        _initialPointerPosition = clampedPointer;
        _pointerPosition = clampedPointer;
        _progress = 0;
        _targetProgress = 0;
        _velocity = 0;
        _gesturePointerProgress = 0;
        ResetDragVelocity();
        _completionRaised = false;
        ResetCommitCandidate();

        _edgeSelectionPending = lockedAnchor == PageAnchor.Auto && IsCorner(clampedPointer, pageSize);
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
            _dragStartPosition = lockedAnchor.IsCorner()
                ? clampedPointer
                : ProjectToEdge(clampedPointer, pageSize, _freeEdge);
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
            : _lockedAnchor.IsCorner()
                ? CalculateProgressFromGrab(
                    _pointerPosition,
                    _initialPointerPosition,
                    _pageSize,
                    _freeEdge)
                : CalculateProgress(_pointerPosition, _pageSize, _freeEdge);
        _progress = Math.Clamp(_progress, 0, 1);
        _gesturePointerProgress = _progress;
        _targetProgress = _progress;
        UpdateProgressVelocity(timestampSeconds);
        NotifyCommitCandidateTransition();
    }

    public void EndDrag(bool animate = true)
    {
        if (_state != PageTurnState.Dragging)
        {
            return;
        }

        var flickTarget = _options.FlickEnabled
            ? GetFlickTarget(_smoothedProgressVelocity, _options.FlickVelocityThreshold)
            : null;
        _targetProgress = flickTarget
            ?? (_progress >= _options.PageChangeThreshold ? 1f : 0f);
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
        _lockedAnchor = PageAnchorExtensions.FromEdge(_hingeEdge);
        _edgeSelectionPending = false;
        _dragStartPosition = new(
            direction == PageTurnDirection.Forward ? pageSize.X : 0,
            pageSize.Y * 0.5f);
        _initialPointerPosition = _dragStartPosition;
        _pointerPosition = _dragStartPosition;
        _progress = 0;
        _targetProgress = 1;
        _velocity = 0;
        _gesturePointerProgress = 0;
        ResetDragVelocity();
        _completionRaised = false;
        ResetCommitCandidate();

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
        ResetCommitCandidate();
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

        var pose = PageCurlGeometry.ComputePose(
            pageSize,
            _dragStartPosition,
            _pointerPosition,
            _hingeEdge,
            _freeEdge,
            _direction,
            _progress,
            _options);
        if (_lockedAnchor.IsCorner())
        {
            var animatedPointer = _gesturePointerProgress > EdgeEpsilon
                ? _dragStartPosition
                    + ((_pointerPosition - _dragStartPosition)
                        * (_progress / _gesturePointerProgress))
                : Vector2.Lerp(
                    _dragStartPosition,
                    ProgressEndpoint(_dragStartPosition, pageSize, _freeEdge),
                    _progress);
            pose = pose with { Position = animatedPointer };
        }

        return pose with
        {
            LockedAnchor = _lockedAnchor,
        };
    }

    private void BeginSettling()
    {
        var releaseVelocity = float.IsFinite(_smoothedProgressVelocity)
            ? _smoothedProgressVelocity
            : 0;
        _velocity = _targetProgress >= 0.5f
            ? MathF.Max(0, releaseVelocity)
            : MathF.Min(0, releaseVelocity);
        _velocity = Math.Clamp(_velocity, -MaximumSettlingVelocity, MaximumSettlingVelocity);
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

    private void ResetCommitCandidate() =>
        _isCommitCandidate = _progress >= _options.PageChangeThreshold;

    private void NotifyCommitCandidateTransition()
    {
        var isCommitCandidate = _progress >= _options.PageChangeThreshold;
        if (isCommitCandidate == _isCommitCandidate)
        {
            return;
        }

        _isCommitCandidate = isCommitCandidate;
        PageChangeThresholdCrossed?.Invoke(
            this,
            new PageChangeThresholdCrossedEventArgs(
                _direction,
                isCommitCandidate,
                _progress));
    }

    private static float? GetFlickTarget(float velocity, float threshold)
    {
        if (velocity > threshold)
        {
            return 1f;
        }

        if (velocity < -threshold)
        {
            return 0f;
        }

        return null;
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
            -MaximumDragVelocity,
            MaximumDragVelocity);
        _lastDragTimestamp = timestampSeconds;
        _lastSampledProgress = _progress;
    }

    private static bool IsValidPageSize(Vector2 pageSize) =>
        IsFinite(pageSize) && pageSize.X > 0 && pageSize.Y > 0;

    private static bool IsValidEdge(PageEdge edge) =>
        edge is PageEdge.Auto or PageEdge.Top or PageEdge.Right or PageEdge.Bottom or PageEdge.Left;

    private static bool IsValidAnchor(PageAnchor anchor) => anchor is
        PageAnchor.Auto
        or PageAnchor.Top
        or PageAnchor.Right
        or PageAnchor.Bottom
        or PageAnchor.Left
        or PageAnchor.TopLeft
        or PageAnchor.TopRight
        or PageAnchor.BottomRight
        or PageAnchor.BottomLeft;

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

    private static float CalculateProgressFromGrab(
        Vector2 pointer,
        Vector2 grab,
        Vector2 pageSize,
        PageEdge freeEdge) => freeEdge switch
    {
        PageEdge.Top => (pointer.Y - grab.Y) / MathF.Max(pageSize.Y - grab.Y, EdgeEpsilon),
        PageEdge.Right => (grab.X - pointer.X) / MathF.Max(grab.X, EdgeEpsilon),
        PageEdge.Bottom => (grab.Y - pointer.Y) / MathF.Max(grab.Y, EdgeEpsilon),
        PageEdge.Left => (pointer.X - grab.X) / MathF.Max(pageSize.X - grab.X, EdgeEpsilon),
        _ => 0,
    };

    private static Vector2 ProgressEndpoint(
        Vector2 grab,
        Vector2 pageSize,
        PageEdge freeEdge) => freeEdge switch
    {
        PageEdge.Top => new(grab.X, pageSize.Y),
        PageEdge.Right => new(0, grab.Y),
        PageEdge.Bottom => new(grab.X, 0),
        PageEdge.Left => new(pageSize.X, grab.Y),
        _ => grab,
    };
}
