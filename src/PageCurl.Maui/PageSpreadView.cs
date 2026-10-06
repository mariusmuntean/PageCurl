using System.Numerics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using PageCurl.Core;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace PageCurl.Maui;

/// <summary>Displays two live pages with one sheet that can cross the gutter.</summary>
public sealed class PageSpreadView : Grid
{
    private const double ActivationWidth = 52;
    private const int SlotCount = 6;

    private readonly IPageContentSnapshotter _snapshotter;
    private readonly Grid _spreadFrame;
    private readonly Grid _leftHost;
    private readonly Grid _rightHost;
    private readonly Grid _previousLeftHost;
    private readonly Grid _nextRightHost;
    private readonly Grid _previousRightHost;
    private readonly Grid _nextLeftHost;
    private readonly BoxView _gutter;
    private readonly bool[] _snapshotDirty = new bool[SlotCount];
    private readonly long[] _snapshotRevision = new long[SlotCount];
    private TaskCompletionSource? _turnFinished;
    private TaskCompletionSource? _contentLayoutChanged;
    private int _lifecycleRevision;
    private PageSnapshotState<SKImage> _currentSnapshots = new();
    private PageSnapshotState<SKImage> _destinationSnapshots = new();
    private PageSnapshotState<SKImage> _sheetBackSnapshots = new();
    private Task _refreshChain = Task.CompletedTask;
    private bool _turnInProgress;
    private bool _snapshotRefreshPending;
    private bool _refreshQueued;
    private bool _handlerDisconnected;
    private long? _activePointerId;

    private static readonly BindablePropertyKey CanTurnForwardPropertyKey =
        BindableProperty.CreateReadOnly(
            nameof(CanTurnForward),
            typeof(bool),
            typeof(PageSpreadView),
            false);

    private static readonly BindablePropertyKey CanTurnBackwardPropertyKey =
        BindableProperty.CreateReadOnly(
            nameof(CanTurnBackward),
            typeof(bool),
            typeof(PageSpreadView),
            false);

    public static readonly BindableProperty CanTurnForwardProperty =
        CanTurnForwardPropertyKey.BindableProperty;

    public static readonly BindableProperty CanTurnBackwardProperty =
        CanTurnBackwardPropertyKey.BindableProperty;

    public static readonly BindableProperty LeftContentProperty = ContentProperty(
        nameof(LeftContent), SnapshotSlot.Left);

    public static readonly BindableProperty RightContentProperty = ContentProperty(
        nameof(RightContent), SnapshotSlot.Right);

    public static readonly BindableProperty PreviousLeftContentProperty = ContentProperty(
        nameof(PreviousLeftContent), SnapshotSlot.PreviousLeft);

    public static readonly BindableProperty NextRightContentProperty = ContentProperty(
        nameof(NextRightContent), SnapshotSlot.NextRight);

    public static readonly BindableProperty PreviousRightContentProperty = ContentProperty(
        nameof(PreviousRightContent), SnapshotSlot.PreviousRight);

    public static readonly BindableProperty NextLeftContentProperty = ContentProperty(
        nameof(NextLeftContent), SnapshotSlot.NextLeft);

    public static readonly BindableProperty PageAspectRatioProperty = BindableProperty.Create(
        nameof(PageAspectRatio),
        typeof(float),
        typeof(PageSpreadView),
        0.75f,
        validateValue: static (_, value) => value is float ratio
            && float.IsFinite(ratio)
            && ratio > 0,
        propertyChanged: OnPresentationChanged);

    public static readonly BindableProperty PageChangeThresholdProperty = BindableProperty.Create(
        nameof(PageChangeThreshold),
        typeof(float),
        typeof(PageSpreadView),
        0.5f,
        validateValue: static (_, value) => value is float threshold
            && float.IsFinite(threshold)
            && threshold is >= 0 and <= 1,
        propertyChanged: OnPresentationChanged);

    public static readonly BindableProperty FlickEnabledProperty = BindableProperty.Create(
        nameof(FlickEnabled),
        typeof(bool),
        typeof(PageSpreadView),
        true,
        propertyChanged: OnPresentationChanged);

    public static readonly BindableProperty FlickVelocityThresholdProperty = BindableProperty.Create(
        nameof(FlickVelocityThreshold),
        typeof(float),
        typeof(PageSpreadView),
        1.5f,
        validateValue: static (_, value) => value is float threshold
            && float.IsFinite(threshold)
            && threshold > 0,
        propertyChanged: OnPresentationChanged);

    public static readonly BindableProperty BacksideModeProperty = BindableProperty.Create(
        nameof(BacksideMode),
        typeof(PageBacksideMode),
        typeof(PageSpreadView),
        PageBacksideMode.MirroredFront,
        validateValue: static (_, value) => value is PageBacksideMode mode && Enum.IsDefined(mode),
        propertyChanged: OnPresentationChanged);

    public static readonly BindableProperty BacksideColorProperty = BindableProperty.Create(
        nameof(BacksideColor),
        typeof(Color),
        typeof(PageSpreadView),
        Colors.White,
        propertyChanged: OnPresentationChanged);

    public static readonly BindableProperty BacksideDimFactorProperty = BindableProperty.Create(
        nameof(BacksideDimFactor),
        typeof(float),
        typeof(PageSpreadView),
        0.82f,
        validateValue: static (_, value) => value is float factor && float.IsFinite(factor),
        coerceValue: static (_, value) => float.IsFinite((float)value) ? Math.Clamp((float)value, 0, 1) : value,
        propertyChanged: OnPresentationChanged);

    public static readonly BindableProperty ReduceMotionProperty = BindableProperty.Create(
        nameof(ReduceMotion),
        typeof(bool),
        typeof(PageSpreadView),
        false,
        propertyChanged: OnPresentationChanged);

    public static readonly BindableProperty GutterColorProperty = BindableProperty.Create(
        nameof(GutterColor),
        typeof(Color),
        typeof(PageSpreadView),
        Color.FromArgb("#1F17120E"),
        propertyChanged: OnPresentationChanged);

    public static readonly BindableProperty GutterWidthProperty = BindableProperty.Create(
        nameof(GutterWidth),
        typeof(double),
        typeof(PageSpreadView),
        1d,
        validateValue: static (_, value) => value is double width
            && double.IsFinite(width)
            && width is >= 0 and <= 24,
        propertyChanged: OnPresentationChanged);

    public PageSpreadView()
        : this(new PageContentSnapshotter())
    {
    }

    public PageSpreadView(IPageContentSnapshotter snapshotter)
    {
        _snapshotter = snapshotter ?? throw new ArgumentNullException(nameof(snapshotter));
        _spreadFrame = new Grid
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
            },
        };
        _leftHost = new Grid();
        _rightHost = new Grid();
        _previousLeftHost = new Grid { Opacity = 0, InputTransparent = true };
        _nextRightHost = new Grid { Opacity = 0, InputTransparent = true };
        _previousRightHost = new Grid { Opacity = 0, InputTransparent = true };
        _nextLeftHost = new Grid { Opacity = 0, InputTransparent = true };
        var leftCell = new Grid { IsClippedToBounds = true };
        leftCell.Children.Add(_previousLeftHost);
        leftCell.Children.Add(_nextLeftHost);
        leftCell.Children.Add(_leftHost);
        var rightCell = new Grid { IsClippedToBounds = true };
        rightCell.Children.Add(_nextRightHost);
        rightCell.Children.Add(_previousRightHost);
        rightCell.Children.Add(_rightHost);
        _spreadFrame.Add(leftCell, 0, 0);
        _spreadFrame.Add(rightCell, 1, 0);

        _gutter = new BoxView
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Fill,
            InputTransparent = true,
            Shadow = new Shadow
            {
                Brush = new SolidColorBrush(Colors.Black),
                Offset = Point.Zero,
                Radius = 12,
                Opacity = 0.22f,
            },
        };
        Grid.SetColumnSpan(_gutter, 2);
        _spreadFrame.Children.Add(_gutter);

        Overlay = new PageTurnView(new PageCurlOptions { TurnAcrossHinge = true })
        {
            Opacity = 0,
            InputTransparent = true,
            ContentScale = PageContentScale.AspectFit,
            SurfaceColor = Colors.Transparent,
        };
        Grid.SetColumnSpan(Overlay, 2);
        _spreadFrame.Children.Add(Overlay);
        AddActivationZone(PageTurnDirection.Backward);
        AddActivationZone(PageTurnDirection.Forward);
        Children.Add(_spreadFrame);

        Overlay.PageTurnCompleted += OnOverlayCompleted;
        Overlay.PageChangeThresholdCrossed += (_, args) =>
            PageChangeThresholdCrossed?.Invoke(this, args);
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
        MarkAllSnapshotsDirty();
        ApplyPresentation();
        UpdateNavigationAvailability();
    }

    public View? LeftContent
    {
        get => (View?)GetValue(LeftContentProperty);
        set => SetValue(LeftContentProperty, value);
    }

    public View? RightContent
    {
        get => (View?)GetValue(RightContentProperty);
        set => SetValue(RightContentProperty, value);
    }

    public View? PreviousLeftContent
    {
        get => (View?)GetValue(PreviousLeftContentProperty);
        set => SetValue(PreviousLeftContentProperty, value);
    }

    public View? NextRightContent
    {
        get => (View?)GetValue(NextRightContentProperty);
        set => SetValue(NextRightContentProperty, value);
    }

    /// <summary>Content shown on the back of the sheet during a forward turn.</summary>
    public View? NextLeftContent
    {
        get => (View?)GetValue(NextLeftContentProperty);
        set => SetValue(NextLeftContentProperty, value);
    }

    /// <summary>Content shown on the back of the sheet during a backward turn.</summary>
    public View? PreviousRightContent
    {
        get => (View?)GetValue(PreviousRightContentProperty);
        set => SetValue(PreviousRightContentProperty, value);
    }

    public float PageAspectRatio
    {
        get => (float)GetValue(PageAspectRatioProperty);
        set => SetValue(PageAspectRatioProperty, value);
    }

    public float PageChangeThreshold
    {
        get => (float)GetValue(PageChangeThresholdProperty);
        set => SetValue(PageChangeThresholdProperty, value);
    }

    public bool FlickEnabled
    {
        get => (bool)GetValue(FlickEnabledProperty);
        set => SetValue(FlickEnabledProperty, value);
    }

    public float FlickVelocityThreshold
    {
        get => (float)GetValue(FlickVelocityThresholdProperty);
        set => SetValue(FlickVelocityThresholdProperty, value);
    }

    public PageBacksideMode BacksideMode
    {
        get => (PageBacksideMode)GetValue(BacksideModeProperty);
        set => SetValue(BacksideModeProperty, value);
    }

    public Color BacksideColor
    {
        get => (Color)GetValue(BacksideColorProperty);
        set => SetValue(BacksideColorProperty, value);
    }

    public float BacksideDimFactor
    {
        get => (float)GetValue(BacksideDimFactorProperty);
        set => SetValue(BacksideDimFactorProperty, value);
    }

    public bool ReduceMotion
    {
        get => (bool)GetValue(ReduceMotionProperty);
        set => SetValue(ReduceMotionProperty, value);
    }

    public Color GutterColor
    {
        get => (Color)GetValue(GutterColorProperty);
        set => SetValue(GutterColorProperty, value);
    }

    public double GutterWidth
    {
        get => (double)GetValue(GutterWidthProperty);
        set => SetValue(GutterWidthProperty, value);
    }

    public bool CanTurnForward => (bool)GetValue(CanTurnForwardProperty);

    public bool CanTurnBackward => (bool)GetValue(CanTurnBackwardProperty);

    public PageTurnView Overlay { get; }

    public event EventHandler<PageSnapshotFailedEventArgs>? SnapshotFailed;

    public event EventHandler<PageTurnCompletedEventArgs>? PageTurnCompleted;

    public event EventHandler<PageChangeThresholdCrossedEventArgs>? PageChangeThresholdCrossed;

    /// <summary>Immediately releases the gesture and restores both stationary pages.</summary>
    public void CancelInteraction()
    {
        _activePointerId = null;
        Overlay.CancelInteraction();
        if (Overlay.Controller.Snapshot.State == PageTurnState.Idle) RestoreStaticPages();
    }

    public bool TryTurnForward() => TryTurn(PageTurnDirection.Forward);

    public bool TryTurnBackward() => TryTurn(PageTurnDirection.Backward);

    public Task RefreshSnapshotAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MarkAllSnapshotsDirty();
        UpdateNavigationAvailability();
        return DrainSnapshotRefreshAsync(cancellationToken);
    }

    private Task DrainSnapshotRefreshAsync(CancellationToken cancellationToken = default)
    {
        _snapshotRefreshPending = false;
        // Serialize every refresh behind the running one. Requests coalesce
        // through the per-slot dirty flags, so a queued request that finds
        // everything current completes without capturing anything.
        var previous = _refreshChain;
        var current = RefreshAfterAsync(previous, cancellationToken, _lifecycleRevision);
        _refreshChain = current;
        // The caller may stop waiting immediately, but the internal chain
        // still owns its predecessor barrier until the running capture ends.
        return current.WaitAsync(cancellationToken);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        _lifecycleRevision++;
        NotifyContentLayoutChanged();
        if (Handler is null)
        {
            _handlerDisconnected = true;
            _activePointerId = null;
            _snapshotRefreshPending = false;
            _refreshChain = Task.CompletedTask;
            Overlay.CancelInteraction();
            RestoreStaticPages();
            _currentSnapshots.Dispose();
            _destinationSnapshots.Dispose();
            _sheetBackSnapshots.Dispose();
            MarkAllSnapshotsDirty();
            UpdateNavigationAvailability();
            return;
        }

        if (_handlerDisconnected)
        {
            _currentSnapshots = new PageSnapshotState<SKImage>();
            _destinationSnapshots = new PageSnapshotState<SKImage>();
            _sheetBackSnapshots = new PageSnapshotState<SKImage>();
        }

        _handlerDisconnected = false;
        MarkAllSnapshotsDirty();
        QueueSnapshotRefresh();
    }

    private async Task RefreshAfterAsync(Task previous, CancellationToken cancellationToken, int lifecycleRevision)
    {
        // A cancelled predecessor must not cancel the requests queued
        // behind it.
        try
        {
            await previous;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
        }

        await WaitForTurnAsync(cancellationToken, lifecycleRevision);
        if (_handlerDisconnected || lifecycleRevision != _lifecycleRevision)
        {
            return;
        }

        for (var slot = 0; slot < SlotCount; slot++)
        {
            await WaitForTurnAsync(cancellationToken, lifecycleRevision);
            if (_handlerDisconnected || lifecycleRevision != _lifecycleRevision) return;
            if (!_snapshotDirty[slot])
            {
                continue;
            }

            var revision = _snapshotRevision[slot];
            var typedSlot = (SnapshotSlot)slot;
            await WaitForContentLayoutAsync(typedSlot, cancellationToken, lifecycleRevision);
            if (_handlerDisconnected || lifecycleRevision != _lifecycleRevision) return;
            if (_turnInProgress || _snapshotRevision[slot] != revision)
            {
                slot--;
                continue;
            }
            var state = GetSlotState(typedSlot);
            var capture = state.BeginCapture(cancellationToken);
            var stored = await CaptureSlotAsync(
                GetSlotContent(typedSlot),
                state,
                capture,
                IsSlotBack(typedSlot),
                typedSlot,
                revision);
            capture.Token.ThrowIfCancellationRequested();
            if (_snapshotRevision[slot] == revision)
            {
                _snapshotDirty[slot] = !stored;
            }
            UpdateNavigationAvailability();
            // UIKit capture must run on the UI thread. Give input and layout
            // a dispatch opportunity between pages instead of rasterizing
            // the entire spread in one uninterrupted event-loop turn.
            await Task.Yield();
            // A turn or a newer invalidation can arrive during capture. Retry
            // this slot after the turn, keeping this request awaitable.
            if (_turnInProgress || _snapshotRevision[slot] != revision) slot--;
        }
    }

    private async Task WaitForTurnAsync(CancellationToken cancellationToken, int lifecycleRevision)
    {
        while (_turnInProgress && lifecycleRevision == _lifecycleRevision)
        {
            await _turnFinished!.Task.WaitAsync(cancellationToken);
        }
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task WaitForContentLayoutAsync(SnapshotSlot slot, CancellationToken cancellationToken, int lifecycleRevision)
    {
        // A queued refresh can outlive the page views it was queued for.
        // Wait for replacement views to attach and arrange before capturing.
        // Neutral/custom snapshotters without a native handler remain usable.
        while (Handler is not null && !_handlerDisconnected && lifecycleRevision == _lifecycleRevision
            && GetSlotContent(slot) is { } content
            && (content.Handler is null || content.Width <= 0 || content.Height <= 0))
        {
            _contentLayoutChanged ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await _contentLayoutChanged.Task.WaitAsync(cancellationToken);
        }
        cancellationToken.ThrowIfCancellationRequested();
    }

    private void NotifyContentLayoutChanged()
    {
        var changed = _contentLayoutChanged;
        _contentLayoutChanged = null;
        changed?.TrySetResult();
    }

    private async Task<bool> CaptureSlotAsync(
        View? content,
        PageSnapshotState<SKImage> state,
        PageSnapshotCapture capture,
        bool isBackSlot,
        SnapshotSlot slot,
        long revision)
    {
        if (content is null)
        {
            if (isBackSlot)
            {
                state.ClearBack();
            }
            else
            {
                state.ClearFront();
            }

            return true;
        }

        try
        {
            var image = await _snapshotter.CaptureAsync(content, capture.Token);
            if (capture.Token.IsCancellationRequested)
            {
                image.Dispose();
                capture.Token.ThrowIfCancellationRequested();
            }

            if (_turnInProgress || !IsCurrentCapture(slot, revision, state))
            {
                image.Dispose();
                if (_turnInProgress) _snapshotRefreshPending = true;
                return false;
            }

            return isBackSlot
                ? state.TryReplaceBack(capture.Generation, image)
                : state.TryReplaceFront(capture.Generation, image);
        }
        catch (OperationCanceledException) when (capture.Token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            if (IsCurrentCapture(slot, revision, state))
            {
                SnapshotFailed?.Invoke(
                    this,
                    new PageSnapshotFailedEventArgs(content, isBackside: IsPhysicalBackside(slot), exception));
            }
            return false;
        }
    }

    private bool IsCurrentCapture(SnapshotSlot slot, long revision, PageSnapshotState<SKImage> state) =>
        !_handlerDisconnected
        && _snapshotRevision[(int)slot] == revision
        && ReferenceEquals(GetSlotState(slot), state);

    private static BindableProperty ContentProperty(string name, SnapshotSlot slot) =>
        BindableProperty.Create(
            name,
            typeof(View),
            typeof(PageSpreadView),
            default(View),
            propertyChanged: (bindable, oldValue, newValue) =>
            {
                var view = (PageSpreadView)bindable;
                view.ReplaceContent(
                    view.GetSlotHost(slot), oldValue as View, newValue as View, slot);
            });

    private Grid GetSlotHost(SnapshotSlot slot) => slot switch
    {
        SnapshotSlot.Left => _leftHost,
        SnapshotSlot.Right => _rightHost,
        SnapshotSlot.PreviousLeft => _previousLeftHost,
        SnapshotSlot.NextRight => _nextRightHost,
        SnapshotSlot.PreviousRight => _previousRightHost,
        SnapshotSlot.NextLeft => _nextLeftHost,
        _ => throw new ArgumentOutOfRangeException(nameof(slot)),
    };

    private static void OnPresentationChanged(
        BindableObject bindable,
        object? oldValue,
        object? newValue)
    {
        var view = (PageSpreadView)bindable;
        view.ApplyPresentation();
        view.UpdateSpreadFrame();
    }

    private void ReplaceContent(
        Grid host,
        View? oldContent,
        View? newContent,
        SnapshotSlot slot)
    {
        if (oldContent is not null)
        {
            oldContent.Loaded -= OnContentLayoutChanged;
            oldContent.SizeChanged -= OnContentLayoutChanged;
            host.Children.Remove(oldContent);
        }

        if (newContent is not null)
        {
            newContent.Loaded += OnContentLayoutChanged;
            newContent.SizeChanged += OnContentLayoutChanged;
            host.Children.Add(newContent);
        }

        _snapshotRevision[(int)slot]++;
        _snapshotDirty[(int)slot] = true;
        NotifyContentLayoutChanged();
        UpdateNavigationAvailability();
        QueueSnapshotRefresh();
    }

    private void OnContentLayoutChanged(object? sender, EventArgs e)
    {
        NotifyContentLayoutChanged();
        for (var slot = 0; slot < SlotCount; slot++)
        {
            if (!ReferenceEquals(GetSlotContent((SnapshotSlot)slot), sender)) continue;
            _snapshotRevision[slot]++;
            _snapshotDirty[slot] = true;
        }
        QueueSnapshotRefresh();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == IsVisibleProperty.PropertyName)
        {
            if (!IsVisible)
            {
                _lifecycleRevision++;
                NotifyContentLayoutChanged();
                MarkAllSnapshotsDirty();
                UpdateNavigationAvailability();
            }
            else QueueSnapshotRefresh();
        }
    }

    private View? GetSlotContent(SnapshotSlot slot) => slot switch
    {
        SnapshotSlot.Left => LeftContent,
        SnapshotSlot.Right => RightContent,
        SnapshotSlot.PreviousLeft => PreviousLeftContent,
        SnapshotSlot.NextRight => NextRightContent,
        SnapshotSlot.PreviousRight => PreviousRightContent,
        SnapshotSlot.NextLeft => NextLeftContent,
        _ => null,
    };

    private PageSnapshotState<SKImage> GetSlotState(SnapshotSlot slot) => slot switch
    {
        SnapshotSlot.Left or SnapshotSlot.Right => _currentSnapshots,
        SnapshotSlot.PreviousLeft or SnapshotSlot.NextRight => _destinationSnapshots,
        _ => _sheetBackSnapshots,
    };

    private static bool IsSlotBack(SnapshotSlot slot) => slot switch
    {
        SnapshotSlot.Right or SnapshotSlot.NextRight or SnapshotSlot.NextLeft => true,
        _ => false,
    };

    private static bool IsPhysicalBackside(SnapshotSlot slot) => slot is
        SnapshotSlot.PreviousRight or SnapshotSlot.NextLeft;

    private void AddActivationZone(PageTurnDirection direction)
    {
        var zone = new SKCanvasView
        {
            WidthRequest = ActivationWidth,
            HorizontalOptions = direction == PageTurnDirection.Forward
                ? LayoutOptions.End
                : LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Fill,
            EnableTouchEvents = true,
            BackgroundColor = Colors.Transparent,
            BindingContext = direction,
        };
        zone.Touch += OnActivationTouch;
        Grid.SetColumn(zone, direction == PageTurnDirection.Forward ? 1 : 0);
        _spreadFrame.Children.Add(zone);
    }

    private void OnActivationTouch(object? sender, SKTouchEventArgs e)
    {
        if (sender is not SKCanvasView zone
            || zone.BindingContext is not PageTurnDirection direction)
        {
            return;
        }

        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                if (_activePointerId.HasValue || !PrepareTurn(direction))
                {
                    break;
                }

                var pointer = ToPagePointer(zone, e.Location, direction, out var pageSize);
                var anchor = direction == PageTurnDirection.Forward
                    ? PageAnchor.Left
                    : PageAnchor.Right;
                if (!Overlay.TryBeginInteraction(pointer, pageSize, anchor))
                {
                    RestoreStaticPages();
                    break;
                }

                _activePointerId = e.Id;
                e.Handled = true;
                break;
            case SKTouchAction.Moved:
                if (_activePointerId == e.Id)
                {
                    Overlay.UpdateInteraction(ToPagePointer(zone, e.Location, direction, out _));
                    e.Handled = true;
                }

                break;
            case SKTouchAction.Released:
            case SKTouchAction.Cancelled:
                if (_activePointerId == e.Id)
                {
                    _activePointerId = null;
                    Overlay.EndInteraction(cancelled: e.ActionType == SKTouchAction.Cancelled);
                    e.Handled = true;
                }

                break;
        }
    }

    private bool TryTurn(PageTurnDirection direction)
    {
        if (!PrepareTurn(direction))
        {
            return false;
        }

        if (!Overlay.TryTurn(direction))
        {
            RestoreStaticPages();
            return false;
        }

        return true;
    }

    private bool PrepareTurn(PageTurnDirection direction)
    {
        if (_handlerDisconnected || _turnInProgress
            || (direction == PageTurnDirection.Forward && !CanTurnForward)
            || (direction == PageTurnDirection.Backward && !CanTurnBackward))
        {
            return false;
        }

        var front = direction == PageTurnDirection.Forward
            ? _currentSnapshots.Back
            : _currentSnapshots.Front;
        var destination = direction == PageTurnDirection.Forward
            ? _destinationSnapshots.Back
            : _destinationSnapshots.Front;
        if (front is null || destination is null)
        {
            return false;
        }

        // The sheet's back face is the adjacent page it lands on, so the
        // turn reads like a physical book instead of a mirrored photocopy.
        var backFace = direction == PageTurnDirection.Forward
            ? (NextLeftContent is null ? null : _sheetBackSnapshots.Back)
            : (PreviousRightContent is null ? null : _sheetBackSnapshots.Front);

        _turnFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _turnInProgress = true;
        UpdateNavigationAvailability();
        Overlay.PageHorizontalPlacement = direction == PageTurnDirection.Forward
            ? PageHorizontalPlacement.Right
            : PageHorizontalPlacement.Left;
        Overlay.LockedAnchor = direction == PageTurnDirection.Forward
            ? PageAnchor.Left
            : PageAnchor.Right;
        Overlay.FrontImage = front;
        Overlay.BackImage = backFace;
        Overlay.BacksideMode = backFace is null
            ? BacksideMode
            : PageBacksideMode.CustomContent;
        Overlay.UnderPageImage = direction == PageTurnDirection.Forward ? destination : null;
        Overlay.PreviousPageImage = direction == PageTurnDirection.Backward ? destination : null;
        Overlay.Opacity = 1;
        if (direction == PageTurnDirection.Forward)
        {
            _rightHost.IsVisible = false;
            _nextRightHost.Opacity = 1;
        }
        else
        {
            _leftHost.IsVisible = false;
            _previousLeftHost.Opacity = 1;
        }

        return true;
    }

    private void OnOverlayCompleted(object? sender, PageTurnCompletedEventArgs e)
    {
        _turnInProgress = false;
        RestoreStaticPages();
        PageTurnCompleted?.Invoke(this, e);
        if (_snapshotRefreshPending)
        {
            ResumePendingSnapshotRefresh();
        }
    }

    private void RestoreStaticPages()
    {
        _activePointerId = null;
        Overlay.Opacity = 0;
        _leftHost.IsVisible = true;
        _rightHost.IsVisible = true;
        _previousLeftHost.Opacity = 0;
        _nextRightHost.Opacity = 0;
        ClearOverlayImages();
        _turnInProgress = false;
        var finished = _turnFinished;
        _turnFinished = null;
        finished?.TrySetResult();
        UpdateNavigationAvailability();
    }

    private void ClearOverlayImages()
    {
        Overlay.FrontImage = null;
        Overlay.BackImage = null;
        Overlay.UnderPageImage = null;
        Overlay.PreviousPageImage = null;
        Overlay.BacksideMode = BacksideMode;
    }

    private void ApplyPresentation()
    {
        Overlay.PageAspectRatio = PageAspectRatio;
        Overlay.PageChangeThreshold = PageChangeThreshold;
        Overlay.FlickEnabled = FlickEnabled;
        Overlay.FlickVelocityThreshold = FlickVelocityThreshold;
        if (!_turnInProgress)
        {
            Overlay.BacksideMode = BacksideMode;
        }

        Overlay.BacksideColor = BacksideColor;
        Overlay.BacksideDimFactor = BacksideDimFactor;
        Overlay.ReduceMotion = ReduceMotion;
        _gutter.Color = GutterColor;
        _gutter.WidthRequest = GutterWidth;
    }

    private void UpdateNavigationAvailability()
    {
        SetValue(CanTurnForwardPropertyKey, IsForwardReady());
        SetValue(CanTurnBackwardPropertyKey, IsBackwardReady());
    }

    private bool IsForwardReady() =>
        IsTurnReady(SnapshotSlot.Right, SnapshotSlot.NextRight, SnapshotSlot.NextLeft);

    private bool IsBackwardReady() =>
        IsTurnReady(SnapshotSlot.Left, SnapshotSlot.PreviousLeft, SnapshotSlot.PreviousRight);

    private bool IsTurnReady(SnapshotSlot front, SnapshotSlot destination, SnapshotSlot back) =>
        !_turnInProgress
        && IsSlotReady(front)
        && IsSlotReady(destination)
        && (GetSlotContent(back) is null || IsSlotReady(back));

    private bool IsSlotReady(SnapshotSlot slot)
    {
        if (_snapshotDirty[(int)slot] || GetSlotContent(slot) is null)
        {
            return false;
        }

        var state = GetSlotState(slot);
        return (IsSlotBack(slot) ? state.Back : state.Front) is not null;
    }

    private void UpdateSpreadFrame()
    {
        var viewport = new Vector2((float)Math.Max(Width, 0), (float)Math.Max(Height, 0));
        if (viewport.X <= 0 || viewport.Y <= 0)
        {
            return;
        }

        var spread = PageAspectLayout.Fit(
            viewport,
            PageAspectRatio * 2,
            Vector2.Zero,
            fallbackAspectRatio: 1.5f);
        _spreadFrame.WidthRequest = spread.Size.X;
        _spreadFrame.HeightRequest = spread.Size.Y;
    }

    private Vector2 ToPagePointer(
        SKCanvasView zone,
        SKPoint location,
        PageTurnDirection direction,
        out Vector2 pageSize)
    {
        var zoneCanvas = zone.CanvasSize;
        var localX = zoneCanvas.Width > 0
            ? location.X * (float)(Math.Max(zone.Width, 1) / zoneCanvas.Width)
            : location.X;
        var localY = zoneCanvas.Height > 0
            ? location.Y * (float)(Math.Max(zone.Height, 1) / zoneCanvas.Height)
            : location.Y;
        var halfWidth = Math.Max(_spreadFrame.Width * 0.5, 1);
        var frameHeight = Math.Max(_spreadFrame.Height, 1);
        pageSize = Overlay.LastContentViewport.IsValid
            ? Overlay.LastContentViewport.Size
            : new Vector2((float)halfWidth, (float)frameHeight);
        var pageX = direction == PageTurnDirection.Forward
            ? halfWidth - Math.Max(zone.Width, 1) + localX
            : localX;
        return new Vector2(
            (float)(pageX / halfWidth) * pageSize.X,
            (float)(localY / frameHeight) * pageSize.Y);
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        UpdateSpreadFrame();
        MarkAllSnapshotsDirty();
        QueueSnapshotRefresh();
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        UpdateSpreadFrame();
        MarkAllSnapshotsDirty();
        QueueSnapshotRefresh();
    }

    private async void QueueSnapshotRefresh()
    {
        if (_turnInProgress)
        {
            _snapshotRefreshPending = true;
            return;
        }

        if (_handlerDisconnected || Handler is null || !IsVisible || _refreshQueued)
        {
            return;
        }

        _refreshQueued = true;
        var lifecycleRevision = _lifecycleRevision;
        await Task.Yield();
        // Let the newly visible spread present and receive input before
        // synchronous native capture; explicit refreshes do not wait here.
        await Task.Delay(16);
        _refreshQueued = false;
        if (_handlerDisconnected || Handler is null || !IsVisible) return;
        if (lifecycleRevision != _lifecycleRevision)
        {
            QueueSnapshotRefresh();
            return;
        }
        // Capture after children are attached and arranged. Their layout
        // events will retry readiness, without timers or exception parsing.
        for (var slot = 0; slot < SlotCount; slot++)
        {
            var content = GetSlotContent((SnapshotSlot)slot);
            if (content is not null && (content.Handler is null || content.Width <= 0 || content.Height <= 0)) return;
        }

        try
        {
            await DrainSnapshotRefreshAsync();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async void ResumePendingSnapshotRefresh()
    {
        var lifecycleRevision = _lifecycleRevision;
        // Cancellation can be followed by hiding the spread in the same input
        // callback. Return to that callback before rasterizing pending content.
        await Task.Yield();
        if (_handlerDisconnected || !IsVisible || lifecycleRevision != _lifecycleRevision)
        {
            return;
        }

        try
        {
            await DrainSnapshotRefreshAsync();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void MarkAllSnapshotsDirty()
    {
        for (var slot = 0; slot < SlotCount; slot++)
        {
            _snapshotRevision[slot]++;
            _snapshotDirty[slot] = true;
        }
    }

    private enum SnapshotSlot
    {
        Left,
        Right,
        PreviousLeft,
        NextRight,
        PreviousRight,
        NextLeft,
    }
}
