using System.Numerics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using PageCurl.Core;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace PageCurl.Maui;

/// <summary>
/// Hosts live MAUI content while flat and uses cached snapshots during a curl.
/// </summary>
public sealed class PageCurlView : Grid
{
    private const double CornerActivationSize = 52;
    private readonly IPageContentSnapshotter _snapshotter;
    private readonly Grid _pageFrame;
    private readonly Grid _frontHost;
    private readonly Grid _backHost;
    private readonly Grid _activationLayer;
    private PageSnapshotState<SKImage> _snapshots = new();
    private bool _handlerDisconnected;
    private long? _activePointerId;
    private TaskCompletionSource? _interactionFinished;
    private bool _refreshQueued;
    private long _contentRevision;

    public static readonly BindableProperty FrontContentProperty = BindableProperty.Create(
        nameof(FrontContent),
        typeof(View),
        typeof(PageCurlView),
        default(View),
        propertyChanged: OnFrontContentChanged);

    public static readonly BindableProperty BackContentProperty = BindableProperty.Create(
        nameof(BackContent),
        typeof(View),
        typeof(PageCurlView),
        default(View),
        propertyChanged: OnBackContentChanged);

    public static readonly BindableProperty LockedAnchorProperty = ForwardedProperty<PageAnchor>(
        nameof(LockedAnchor),
        PageAnchor.Auto,
        static value => Enum.IsDefined(value));

    public static readonly BindableProperty PageChangeThresholdProperty = ForwardedProperty<float>(
        nameof(PageChangeThreshold),
        0.5f,
        static value => float.IsFinite(value) && value is >= 0 and <= 1);

    public static readonly BindableProperty FlickEnabledProperty = ForwardedProperty(
        nameof(FlickEnabled),
        true);

    public static readonly BindableProperty FlickVelocityThresholdProperty = ForwardedProperty<float>(
        nameof(FlickVelocityThreshold),
        1.5f,
        static value => float.IsFinite(value) && value > 0);

    public static readonly BindableProperty PageAspectRatioProperty = ForwardedProperty<float>(
        nameof(PageAspectRatio),
        0f,
        static value => float.IsFinite(value) && value >= 0);

    public static readonly BindableProperty BacksideModeProperty = ForwardedProperty(
        nameof(BacksideMode),
        PageBacksideMode.MirroredFront,
        static value => Enum.IsDefined(value));

    public static readonly BindableProperty BacksideColorProperty = ForwardedProperty(
        nameof(BacksideColor),
        Colors.White);

    public static readonly BindableProperty BacksideDimFactorProperty = BindableProperty.Create(
        nameof(BacksideDimFactor),
        typeof(float),
        typeof(PageCurlView),
        0.82f,
        validateValue: static (_, value) => value is float factor && float.IsFinite(factor),
        propertyChanged: OnForwardedPropertyChanged,
        coerceValue: static (_, value) => float.IsFinite((float)value) ? Math.Clamp((float)value, 0, 1) : value);

    public static readonly BindableProperty ReduceMotionProperty = ForwardedProperty(
        nameof(ReduceMotion),
        false);

    public static readonly BindableProperty UnderPageImageProperty = ForwardedProperty<SKImage?>(
        nameof(UnderPageImage),
        null);

    public static readonly BindableProperty PreviousPageImageProperty = ForwardedProperty<SKImage?>(
        nameof(PreviousPageImage),
        null);

    public PageCurlView()
        : this(new PageContentSnapshotter())
    {
    }

    public PageCurlView(IPageContentSnapshotter snapshotter)
    {
        _snapshotter = snapshotter ?? throw new ArgumentNullException(nameof(snapshotter));
        HorizontalOptions = LayoutOptions.Fill;
        VerticalOptions = LayoutOptions.Fill;

        _pageFrame = new Grid
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            IsClippedToBounds = false,
        };
        _backHost = new Grid { InputTransparent = true };
        _frontHost = new Grid();
        Renderer = new PageTurnView
        {
            IsVisible = false,
            Opacity = 0,
            InputTransparent = true,
            ContentScale = PageContentScale.AspectFit,
            SurfaceColor = Colors.Transparent,
        };
        _activationLayer = CreateActivationLayer();
        _pageFrame.Children.Add(_backHost);
        _pageFrame.Children.Add(_frontHost);
        _pageFrame.Children.Add(_activationLayer);
        Children.Add(_pageFrame);
        Children.Add(Renderer);

        Renderer.PageTurnCompleted += OnRendererPageTurnCompleted;
        Renderer.PageChangeThresholdCrossed += OnRendererThresholdCrossed;
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
        ApplyForwardedProperties();
    }

    public View? FrontContent
    {
        get => (View?)GetValue(FrontContentProperty);
        set => SetValue(FrontContentProperty, value);
    }

    public View? BackContent
    {
        get => (View?)GetValue(BackContentProperty);
        set => SetValue(BackContentProperty, value);
    }

    public PageAnchor LockedAnchor
    {
        get => (PageAnchor)GetValue(LockedAnchorProperty);
        set => SetValue(LockedAnchorProperty, value);
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

    public float PageAspectRatio
    {
        get => (float)GetValue(PageAspectRatioProperty);
        set => SetValue(PageAspectRatioProperty, value);
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

    public SKImage? UnderPageImage
    {
        get => (SKImage?)GetValue(UnderPageImageProperty);
        set => SetValue(UnderPageImageProperty, value);
    }

    public SKImage? PreviousPageImage
    {
        get => (SKImage?)GetValue(PreviousPageImageProperty);
        set => SetValue(PreviousPageImageProperty, value);
    }

    public PageTurnView Renderer { get; }

    public event EventHandler<PageSnapshotFailedEventArgs>? SnapshotFailed;

    public event EventHandler<PageTurnCompletedEventArgs>? PageTurnCompleted;

    public event EventHandler<PageChangeThresholdCrossedEventArgs>? PageChangeThresholdCrossed;

    public bool TryTurn(PageTurnDirection direction)
    {
        ShowRenderer();
        // The renderer is hidden while flat and may never have been measured.
        // Use the already fitted live frame for the first command as well.
        var pageSize = new Vector2(
            (float)(_pageFrame.Width > 0 ? _pageFrame.Width : _pageFrame.WidthRequest),
            (float)(_pageFrame.Height > 0 ? _pageFrame.Height : _pageFrame.HeightRequest));
        if (Renderer.TryTurn(direction, pageSize))
        {
            return true;
        }

        ShowLiveContent();
        return false;
    }

    public async Task RefreshSnapshotAsync(CancellationToken cancellationToken = default)
    {
        await WaitForInteractionAsync(cancellationToken);
        if (_handlerDisconnected) return;
        var snapshots = _snapshots;
        var revision = _contentRevision;
        var capture = snapshots.BeginCapture(cancellationToken);
        await CaptureContentAsync(FrontContent, snapshots, revision, capture, isBackside: false);
        capture.Token.ThrowIfCancellationRequested();
        await CaptureContentAsync(BackContent, snapshots, revision, capture, isBackside: true);
        capture.Token.ThrowIfCancellationRequested();
    }

    public void CancelInteraction()
    {
        _activePointerId = null;
        Renderer.CancelInteraction();
        ShowLiveContent();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (Handler is null)
        {
            _handlerDisconnected = true;
            _activePointerId = null;
            Renderer.CancelInteraction();
            ShowLiveContent();
            Renderer.FrontImage = null;
            Renderer.BackImage = null;
            _snapshots.Dispose();
            return;
        }

        if (_handlerDisconnected)
        {
            _snapshots = new PageSnapshotState<SKImage>();
        }

        _handlerDisconnected = false;
        QueueSnapshotRefresh();
    }

    private static BindableProperty ForwardedProperty<T>(
        string name,
        T defaultValue,
        Func<T, bool>? validate = null) => BindableProperty.Create(
            name,
            typeof(T),
            typeof(PageCurlView),
            defaultValue,
            validateValue: validate is null
                ? null
                : (_, value) => value is T typed && validate(typed),
            propertyChanged: OnForwardedPropertyChanged);

    private static void OnFrontContentChanged(
        BindableObject bindable,
        object? oldValue,
        object? newValue)
    {
        var view = (PageCurlView)bindable;
        view._contentRevision++;
        view.ReplaceHostedView(view._frontHost, oldValue as View, newValue as View);
        view.UpdatePageFrame();
        view.QueueSnapshotRefresh();
    }

    private static void OnBackContentChanged(
        BindableObject bindable,
        object? oldValue,
        object? newValue)
    {
        var view = (PageCurlView)bindable;
        view._contentRevision++;
        view.ReplaceHostedView(view._backHost, oldValue as View, newValue as View);
        view.QueueSnapshotRefresh();
    }

    private static void OnForwardedPropertyChanged(
        BindableObject bindable,
        object? oldValue,
        object? newValue)
    {
        var view = (PageCurlView)bindable;
        view.ApplyForwardedProperties();
        view.UpdatePageFrame();
    }

    private void ReplaceHostedView(Grid host, View? oldView, View? newView)
    {
        if (oldView is not null)
        {
            oldView.Loaded -= OnContentLayoutChanged;
            oldView.SizeChanged -= OnContentLayoutChanged;
            host.Children.Remove(oldView);
        }

        if (newView is not null)
        {
            newView.Loaded += OnContentLayoutChanged;
            newView.SizeChanged += OnContentLayoutChanged;
            host.Children.Add(newView);
        }
    }

    private void OnContentLayoutChanged(object? sender, EventArgs e)
    {
        _contentRevision++;
        QueueSnapshotRefresh();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == IsVisibleProperty.PropertyName)
        {
            // A capture started before hiding belongs to the previous presentation.
            if (!IsVisible) _contentRevision++;
            else QueueSnapshotRefresh();
        }
    }

    private Grid CreateActivationLayer()
    {
        var layer = new Grid
        {
            InputTransparent = true,
            CascadeInputTransparent = false,
            RowDefinitions =
            {
                new RowDefinition(CornerActivationSize),
                new RowDefinition(GridLength.Star),
                new RowDefinition(CornerActivationSize),
            },
            ColumnDefinitions =
            {
                new ColumnDefinition(CornerActivationSize),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(CornerActivationSize),
            },
        };

        AddActivationZone(layer, 0, 0);
        AddActivationZone(layer, 1, 0);
        AddActivationZone(layer, 2, 0);
        AddActivationZone(layer, 0, 1);
        AddActivationZone(layer, 2, 1);
        AddActivationZone(layer, 0, 2);
        AddActivationZone(layer, 1, 2);
        AddActivationZone(layer, 2, 2);
        return layer;
    }

    private void AddActivationZone(Grid layer, int column, int row)
    {
        var zone = new SKCanvasView
        {
            BackgroundColor = Colors.Transparent,
            EnableTouchEvents = true,
            BindingContext = AnchorForCell(column, row),
        };
        zone.Touch += OnActivationTouch;
        layer.Add(zone, column, row);
    }

    private void OnActivationTouch(object? sender, SKTouchEventArgs e)
    {
        if (sender is not SKCanvasView zone)
        {
            return;
        }

        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                if (_activePointerId.HasValue)
                {
                    e.Handled = true;
                    break;
                }

                if (zone.BindingContext is PageAnchor zoneAnchor
                    && IsZoneLocked(zoneAnchor, LockedAnchor))
                {
                    break;
                }

                var pressed = ToPagePointer(zone, e.Location, out var pageSize);
                if (Renderer.TryBeginInteraction(pressed, pageSize, LockedAnchor))
                {
                    _activePointerId = e.Id;
                    ShowRenderer();
                    e.Handled = true;
                }

                break;
            case SKTouchAction.Moved:
                if (_activePointerId == e.Id)
                {
                    Renderer.UpdateInteraction(ToPagePointer(zone, e.Location, out _));
                    e.Handled = true;
                }

                break;
            case SKTouchAction.Released:
            case SKTouchAction.Cancelled:
                if (_activePointerId == e.Id)
                {
                    _activePointerId = null;
                    Renderer.EndInteraction(cancelled: e.ActionType == SKTouchAction.Cancelled);
                    e.Handled = true;
                }

                break;
        }
    }

    private static PageAnchor AnchorForCell(int column, int row) => (column, row) switch
    {
        (0, 0) => PageAnchor.TopLeft,
        (1, 0) => PageAnchor.Top,
        (2, 0) => PageAnchor.TopRight,
        (0, 1) => PageAnchor.Left,
        (2, 1) => PageAnchor.Right,
        (0, 2) => PageAnchor.BottomLeft,
        (1, 2) => PageAnchor.Bottom,
        (2, 2) => PageAnchor.BottomRight,
        _ => PageAnchor.Auto,
    };

    private static bool IsZoneLocked(PageAnchor zone, PageAnchor lockedAnchor)
    {
        if (zone == lockedAnchor)
        {
            return true;
        }

        return lockedAnchor switch
        {
            PageAnchor.Top => zone is PageAnchor.TopLeft or PageAnchor.TopRight,
            PageAnchor.Right => zone is PageAnchor.TopRight or PageAnchor.BottomRight,
            PageAnchor.Bottom => zone is PageAnchor.BottomLeft or PageAnchor.BottomRight,
            PageAnchor.Left => zone is PageAnchor.TopLeft or PageAnchor.BottomLeft,
            _ => false,
        };
    }

    private async Task CaptureContentAsync(
        View? content,
        PageSnapshotState<SKImage> snapshots,
        long revision,
        PageSnapshotCapture capture,
        bool isBackside)
    {
        await WaitForInteractionAsync(capture.Token);
        if (!IsCurrentCapture(snapshots, revision)) return;
        if (content is null)
        {
            if (isBackside)
            {
                Renderer.BackImage = null;
                _snapshots.ClearBack();
            }
            else
            {
                Renderer.FrontImage = null;
                _snapshots.ClearFront();
            }

            return;
        }

        try
        {
            var image = await _snapshotter.CaptureAsync(content, capture.Token);
            if (capture.Token.IsCancellationRequested)
            {
                image.Dispose();
                capture.Token.ThrowIfCancellationRequested();
            }

            try
            {
                await WaitForInteractionAsync(capture.Token);
            }
            catch
            {
                image.Dispose();
                throw;
            }
            if (!IsCurrentCapture(snapshots, revision))
            {
                image.Dispose();
                return;
            }
            var replaced = isBackside
                ? snapshots.TryReplaceBack(capture.Generation, image)
                : snapshots.TryReplaceFront(capture.Generation, image);
            if (!replaced)
            {
                return;
            }

            if (isBackside)
            {
                Renderer.BackImage = _snapshots.Back;
            }
            else
            {
                Renderer.FrontImage = _snapshots.Front;
            }
        }
        catch (OperationCanceledException) when (capture.Token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            if (!capture.Token.IsCancellationRequested && IsCurrentCapture(snapshots, revision))
                SnapshotFailed?.Invoke(this, new PageSnapshotFailedEventArgs(content, isBackside, exception));
        }
    }

    private void ApplyForwardedProperties()
    {
        Renderer.LockedAnchor = LockedAnchor;
        Renderer.PageChangeThreshold = PageChangeThreshold;
        Renderer.FlickEnabled = FlickEnabled;
        Renderer.FlickVelocityThreshold = FlickVelocityThreshold;
        Renderer.BacksideMode = BacksideMode;
        Renderer.BacksideColor = BacksideColor;
        Renderer.BacksideDimFactor = BacksideDimFactor;
        Renderer.ReduceMotion = ReduceMotion;
        Renderer.UnderPageImage = UnderPageImage;
        Renderer.PreviousPageImage = PreviousPageImage;
    }

    private void UpdatePageFrame()
    {
        var viewport = new Vector2((float)Math.Max(Width, 0), (float)Math.Max(Height, 0));
        if (viewport.X <= 0 || viewport.Y <= 0)
        {
            return;
        }

        var intrinsic = FrontContent is null
            ? Vector2.Zero
            : new Vector2(
                (float)Math.Max(FrontContent.DesiredSize.Width, 0),
                (float)Math.Max(FrontContent.DesiredSize.Height, 0));
        var page = PageAspectLayout.Fit(viewport, PageAspectRatio, intrinsic);
        _pageFrame.WidthRequest = page.Size.X;
        _pageFrame.HeightRequest = page.Size.Y;
        Renderer.PageAspectRatio = page.Size.X / page.Size.Y;
    }

    private void ShowRenderer()
    {
        _frontHost.IsVisible = false;
        Renderer.IsVisible = true;
        Renderer.Opacity = 1;
    }

    private void ShowLiveContent()
    {
        Renderer.Opacity = 0;
        Renderer.IsVisible = false;
        _frontHost.IsVisible = true;
    }

    private void OnRendererPageTurnCompleted(object? sender, PageTurnCompletedEventArgs e)
    {
        ShowLiveContent();
        var finished = _interactionFinished;
        _interactionFinished = null;
        finished?.TrySetResult();
        PageTurnCompleted?.Invoke(this, e);
    }

    private void OnRendererThresholdCrossed(
        object? sender,
        PageChangeThresholdCrossedEventArgs e) =>
        PageChangeThresholdCrossed?.Invoke(this, e);

    private void OnLoaded(object? sender, EventArgs e)
    {
        UpdatePageFrame();
        QueueSnapshotRefresh();
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        _contentRevision++;
        UpdatePageFrame();
        QueueSnapshotRefresh();
    }

    private bool IsCurrentCapture(PageSnapshotState<SKImage> snapshots, long revision) =>
        !_handlerDisconnected && ReferenceEquals(snapshots, _snapshots) && revision == _contentRevision;

    private async Task WaitForInteractionAsync(CancellationToken cancellationToken)
    {
        while (Renderer.Controller.Snapshot.State != PageTurnState.Idle)
        {
            _interactionFinished ??= new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            await _interactionFinished.Task.WaitAsync(cancellationToken);
        }
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async void QueueSnapshotRefresh()
    {
        if (_handlerDisconnected || Handler is null || !IsVisible || _refreshQueued) return;
        _refreshQueued = true;
        await Task.Yield();
        _refreshQueued = false;
        if (_handlerDisconnected || Handler is null || !IsVisible) return;
        if (!IsContentReady(FrontContent) || !IsContentReady(BackContent)) return;
        try
        {
            await RefreshSnapshotAsync();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static bool IsContentReady(View? content) => content is null
        || (content.Handler is not null && content.Width > 0 && content.Height > 0);

    private Vector2 ToPagePointer(
        SKCanvasView zone,
        SKPoint location,
        out Vector2 pageSize)
    {
        var zoneCanvas = zone.CanvasSize;
        var zoneWidth = Math.Max(zone.Width, 1);
        var zoneHeight = Math.Max(zone.Height, 1);
        var localX = zoneCanvas.Width > 0
            ? location.X * (float)(zoneWidth / zoneCanvas.Width)
            : location.X;
        var localY = zoneCanvas.Height > 0
            ? location.Y * (float)(zoneHeight / zoneCanvas.Height)
            : location.Y;
        var frameWidth = Math.Max(_pageFrame.Width, 1);
        var frameHeight = Math.Max(_pageFrame.Height, 1);
        pageSize = Renderer.LastContentViewport.IsValid
            ? Renderer.LastContentViewport.Size
            : new Vector2((float)frameWidth, (float)frameHeight);
        return new Vector2(
            (float)((zone.X + localX) / frameWidth) * pageSize.X,
            (float)((zone.Y + localY) / frameHeight) * pageSize.Y);
    }
}
