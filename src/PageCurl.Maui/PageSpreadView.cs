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
    private readonly IPageContentSnapshotter _snapshotter;
    private readonly Grid _spreadFrame;
    private readonly Grid _leftHost;
    private readonly Grid _rightHost;
    private readonly Grid _previousLeftHost;
    private readonly Grid _nextRightHost;
    private readonly BoxView _gutter;
    private PageSnapshotState<SKImage> _currentSnapshots = new();
    private PageSnapshotState<SKImage> _destinationSnapshots = new();
    private bool _turnInProgress;
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

    public static readonly BindableProperty LeftContentProperty = BindableProperty.Create(
        nameof(LeftContent),
        typeof(View),
        typeof(PageSpreadView),
        default(View),
        propertyChanged: OnLeftContentChanged);

    public static readonly BindableProperty RightContentProperty = BindableProperty.Create(
        nameof(RightContent),
        typeof(View),
        typeof(PageSpreadView),
        default(View),
        propertyChanged: OnRightContentChanged);

    public static readonly BindableProperty PreviousLeftContentProperty = BindableProperty.Create(
        nameof(PreviousLeftContent),
        typeof(View),
        typeof(PageSpreadView),
        default(View),
        propertyChanged: OnPreviousLeftContentChanged);

    public static readonly BindableProperty NextRightContentProperty = BindableProperty.Create(
        nameof(NextRightContent),
        typeof(View),
        typeof(PageSpreadView),
        default(View),
        propertyChanged: OnNextRightContentChanged);

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
        coerceValue: static (_, value) => Math.Clamp((float)value, 0, 1),
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
        var leftCell = new Grid { IsClippedToBounds = true };
        leftCell.Children.Add(_previousLeftHost);
        leftCell.Children.Add(_leftHost);
        var rightCell = new Grid { IsClippedToBounds = true };
        rightCell.Children.Add(_nextRightHost);
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

        Overlay = new PageTurnView
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

    public bool TryTurnForward() => TryTurn(PageTurnDirection.Forward);

    public bool TryTurnBackward() => TryTurn(PageTurnDirection.Backward);

    public async Task RefreshSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (_turnInProgress)
        {
            return;
        }

        var currentCapture = _currentSnapshots.BeginCapture(cancellationToken);
        await CaptureAsync(LeftContent, _currentSnapshots, currentCapture, isBackSlot: false);
        currentCapture.Token.ThrowIfCancellationRequested();
        await CaptureAsync(RightContent, _currentSnapshots, currentCapture, isBackSlot: true);
        var destinationCapture = _destinationSnapshots.BeginCapture(cancellationToken);
        await CaptureAsync(
            PreviousLeftContent,
            _destinationSnapshots,
            destinationCapture,
            isBackSlot: false);
        destinationCapture.Token.ThrowIfCancellationRequested();
        await CaptureAsync(
            NextRightContent,
            _destinationSnapshots,
            destinationCapture,
            isBackSlot: true);
        UpdateNavigationAvailability();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is null)
        {
            _handlerDisconnected = true;
            _activePointerId = null;
            Overlay.CancelInteraction();
            ClearOverlayImages();
            _currentSnapshots.Dispose();
            _destinationSnapshots.Dispose();
            return;
        }

        if (_handlerDisconnected)
        {
            _currentSnapshots = new PageSnapshotState<SKImage>();
            _destinationSnapshots = new PageSnapshotState<SKImage>();
        }

        _handlerDisconnected = false;
        QueueSnapshotRefresh();
    }

    private static void OnLeftContentChanged(
        BindableObject bindable,
        object? oldValue,
        object? newValue) =>
        ((PageSpreadView)bindable).ReplaceContent(
            ((PageSpreadView)bindable)._leftHost,
            oldValue as View,
            newValue as View);

    private static void OnRightContentChanged(
        BindableObject bindable,
        object? oldValue,
        object? newValue) =>
        ((PageSpreadView)bindable).ReplaceContent(
            ((PageSpreadView)bindable)._rightHost,
            oldValue as View,
            newValue as View);

    private static void OnPreviousLeftContentChanged(
        BindableObject bindable,
        object? oldValue,
        object? newValue) =>
        ((PageSpreadView)bindable).ReplaceContent(
            ((PageSpreadView)bindable)._previousLeftHost,
            oldValue as View,
            newValue as View);

    private static void OnNextRightContentChanged(
        BindableObject bindable,
        object? oldValue,
        object? newValue) =>
        ((PageSpreadView)bindable).ReplaceContent(
            ((PageSpreadView)bindable)._nextRightHost,
            oldValue as View,
            newValue as View);

    private static void OnPresentationChanged(
        BindableObject bindable,
        object? oldValue,
        object? newValue)
    {
        var view = (PageSpreadView)bindable;
        view.ApplyPresentation();
        view.UpdateSpreadFrame();
    }

    private void ReplaceContent(Grid host, View? oldContent, View? newContent)
    {
        if (oldContent is not null)
        {
            host.Children.Remove(oldContent);
        }

        if (newContent is not null)
        {
            host.Children.Add(newContent);
        }

        UpdateNavigationAvailability();
        QueueSnapshotRefresh();
    }

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
                if (_activePointerId == e.Id)
                {
                    _activePointerId = null;
                    Overlay.EndInteraction(cancelled: false);
                    e.Handled = true;
                }

                break;
            case SKTouchAction.Cancelled:
                if (_activePointerId == e.Id)
                {
                    _activePointerId = null;
                    Overlay.EndInteraction(cancelled: true);
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
        if (_turnInProgress
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

        _turnInProgress = true;
        UpdateNavigationAvailability();
        Overlay.PageHorizontalPlacement = direction == PageTurnDirection.Forward
            ? PageHorizontalPlacement.Right
            : PageHorizontalPlacement.Left;
        Overlay.LockedAnchor = direction == PageTurnDirection.Forward
            ? PageAnchor.Left
            : PageAnchor.Right;
        Overlay.FrontImage = front;
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

    private async Task CaptureAsync(
        View? content,
        PageSnapshotState<SKImage> state,
        PageSnapshotCapture capture,
        bool isBackSlot)
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

            return;
        }

        try
        {
            var image = await _snapshotter.CaptureAsync(content, capture.Token);
            capture.Token.ThrowIfCancellationRequested();
            if (isBackSlot)
            {
                state.TryReplaceBack(capture.Generation, image);
            }
            else
            {
                state.TryReplaceFront(capture.Generation, image);
            }
        }
        catch (OperationCanceledException) when (capture.Token.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            SnapshotFailed?.Invoke(
                this,
                new PageSnapshotFailedEventArgs(content, isBackside: false, exception));
        }
    }

    private void OnOverlayCompleted(object? sender, PageTurnCompletedEventArgs e)
    {
        _turnInProgress = false;
        RestoreStaticPages();
        PageTurnCompleted?.Invoke(this, e);
    }

    private void RestoreStaticPages()
    {
        Overlay.Opacity = 0;
        _leftHost.IsVisible = true;
        _rightHost.IsVisible = true;
        _previousLeftHost.Opacity = 0;
        _nextRightHost.Opacity = 0;
        ClearOverlayImages();
        _turnInProgress = false;
        UpdateNavigationAvailability();
    }

    private void ClearOverlayImages()
    {
        Overlay.FrontImage = null;
        Overlay.BackImage = null;
        Overlay.UnderPageImage = null;
        Overlay.PreviousPageImage = null;
    }

    private void ApplyPresentation()
    {
        Overlay.PageAspectRatio = PageAspectRatio;
        Overlay.PageChangeThreshold = PageChangeThreshold;
        Overlay.FlickEnabled = FlickEnabled;
        Overlay.FlickVelocityThreshold = FlickVelocityThreshold;
        Overlay.BacksideMode = BacksideMode;
        Overlay.BacksideColor = BacksideColor;
        Overlay.BacksideDimFactor = BacksideDimFactor;
        Overlay.ReduceMotion = ReduceMotion;
        _gutter.Color = GutterColor;
        _gutter.WidthRequest = GutterWidth;
    }

    private void UpdateNavigationAvailability()
    {
        SetValue(
            CanTurnForwardPropertyKey,
            !_turnInProgress
                && RightContent is not null
                && NextRightContent is not null
                && _currentSnapshots.Back is not null
                && _destinationSnapshots.Back is not null);
        SetValue(
            CanTurnBackwardPropertyKey,
            !_turnInProgress
                && LeftContent is not null
                && PreviousLeftContent is not null
                && _currentSnapshots.Front is not null
                && _destinationSnapshots.Front is not null);
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
        QueueSnapshotRefresh();
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        UpdateSpreadFrame();
        QueueSnapshotRefresh();
    }

    private async void QueueSnapshotRefresh()
    {
        if (_handlerDisconnected || Handler is null || _turnInProgress)
        {
            return;
        }

        try
        {
            await RefreshSnapshotAsync();
        }
        catch (OperationCanceledException)
        {
        }
    }
}
