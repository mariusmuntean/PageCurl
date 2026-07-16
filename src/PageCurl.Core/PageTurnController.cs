using System.Numerics;

namespace PageCurl.Core;

/// <summary>
/// Tracks a finger drag and settles it to the nearest page endpoint using a
/// critically damped spring. It has no UI or platform dependencies.
/// </summary>
public sealed class PageTurnController
{
    private readonly PageCurlOptions _options;
    private PageTurnState _state;
    private PageTurnDirection _direction;
    private Vector2 _pageSize;
    private Vector2 _dragStartPosition;
    private Vector2 _pointerPosition;
    private float _progress;
    private float _targetProgress;
    private float _velocity;
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
        _targetProgress);

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
        _dragStartPosition = new(
            _direction == PageTurnDirection.Forward ? pageSize.X : 0,
            clampedPointer.Y);
        _pointerPosition = clampedPointer;
        _progress = 0;
        _targetProgress = 0;
        _velocity = 0;
        _completionRaised = false;
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
        _progress = _direction == PageTurnDirection.Forward
            ? 1f - (_pointerPosition.X / _pageSize.X)
            : _pointerPosition.X / _pageSize.X;
        _progress = Math.Clamp(_progress, 0, 1);
        _targetProgress = _progress;
        _velocity = 0;
    }

    public void EndDrag(bool animate = true)
    {
        if (_state != PageTurnState.Dragging)
        {
            return;
        }

        _targetProgress = _progress >= _options.SnapThreshold ? 1 : 0;
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
        _dragStartPosition = new(
            direction == PageTurnDirection.Forward ? pageSize.X : 0,
            pageSize.Y * 0.5f);
        _pointerPosition = _dragStartPosition;
        _progress = 0;
        _targetProgress = 1;
        _velocity = 0;
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
            _direction,
            _progress,
            _options);
    }

    private void BeginSettling()
    {
        _velocity = 0;
        _completionRaised = false;
        _state = PageTurnState.Settling;
    }

    private void CompleteAtTarget()
    {
        _progress = _targetProgress;
        _velocity = 0;
        _state = PageTurnState.Idle;
        if (!_completionRaised)
        {
            _completionRaised = true;
            PageTurnCompleted?.Invoke(
                this,
                new PageTurnCompletedEventArgs(_direction, _targetProgress >= 0.5f));
        }
    }

    private static bool IsValidPageSize(Vector2 pageSize) =>
        IsFinite(pageSize) && pageSize.X > 0 && pageSize.Y > 0;

    private static bool IsFinite(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y);

    private static Vector2 ClampToPage(Vector2 value, Vector2 pageSize) =>
        new(Math.Clamp(value.X, 0, pageSize.X), Math.Clamp(value.Y, 0, pageSize.Y));
}
