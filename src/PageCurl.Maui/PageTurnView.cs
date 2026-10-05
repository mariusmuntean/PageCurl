using System.Numerics;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using PageCurl.Core;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace PageCurl.Maui;

/// <summary>
/// A GPU-backed MAUI view that renders a draggable page curl using SkiaSharp.
/// The images are supplied as SKImage instances so applications can own and
/// cache their decoding pipeline.
/// </summary>
public sealed class PageTurnView : SKGLView
{
    public static readonly BindableProperty FrontImageProperty = BindableProperty.Create(
        nameof(FrontImage),
        typeof(SKImage),
        typeof(PageTurnView),
        default(SKImage),
        propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty BackImageProperty = BindableProperty.Create(
        nameof(BackImage),
        typeof(SKImage),
        typeof(PageTurnView),
        default(SKImage),
        propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty UnderPageImageProperty = BindableProperty.Create(
        nameof(UnderPageImage),
        typeof(SKImage),
        typeof(PageTurnView),
        default(SKImage),
        propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty ReduceMotionProperty = BindableProperty.Create(
        nameof(ReduceMotion),
        typeof(bool),
        typeof(PageTurnView),
        false);

    public static readonly BindableProperty LockedEdgeProperty = BindableProperty.Create(
        nameof(LockedEdge),
        typeof(PageEdge),
        typeof(PageTurnView),
        PageEdge.Auto,
        validateValue: static (_, value) => value is PageEdge edge
            && Enum.IsDefined(edge),
        propertyChanged: OnLockedEdgeChanged);

    public static readonly BindableProperty LockedAnchorProperty = BindableProperty.Create(
        nameof(LockedAnchor),
        typeof(PageAnchor),
        typeof(PageTurnView),
        PageAnchor.Auto,
        validateValue: static (_, value) => value is PageAnchor anchor
            && Enum.IsDefined(anchor),
        propertyChanged: OnLockedAnchorChanged);

    public static readonly BindableProperty PageChangeThresholdProperty = BindableProperty.Create(
        nameof(PageChangeThreshold),
        typeof(float),
        typeof(PageTurnView),
        0.5f,
        validateValue: static (_, value) => value is float threshold
            && float.IsFinite(threshold)
            && threshold is >= 0 and <= 1,
        propertyChanged: OnInteractionOptionChanged);

    public static readonly BindableProperty FlickEnabledProperty = BindableProperty.Create(
        nameof(FlickEnabled),
        typeof(bool),
        typeof(PageTurnView),
        true,
        propertyChanged: OnInteractionOptionChanged);

    public static readonly BindableProperty FlickVelocityThresholdProperty = BindableProperty.Create(
        nameof(FlickVelocityThreshold),
        typeof(float),
        typeof(PageTurnView),
        1.5f,
        validateValue: static (_, value) => value is float threshold
            && float.IsFinite(threshold)
            && threshold > 0,
        propertyChanged: OnInteractionOptionChanged);

    public static readonly BindableProperty PageAspectRatioProperty = BindableProperty.Create(
        nameof(PageAspectRatio),
        typeof(float),
        typeof(PageTurnView),
        0f,
        validateValue: static (_, value) => value is float ratio
            && float.IsFinite(ratio)
            && ratio >= 0,
        propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty PageHorizontalPlacementProperty = BindableProperty.Create(
        nameof(PageHorizontalPlacement),
        typeof(PageHorizontalPlacement),
        typeof(PageTurnView),
        PageHorizontalPlacement.Center,
        validateValue: static (_, value) => value is PageHorizontalPlacement placement
            && Enum.IsDefined(placement),
        propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty BacksideModeProperty = BindableProperty.Create(
        nameof(BacksideMode),
        typeof(PageBacksideMode),
        typeof(PageTurnView),
        PageBacksideMode.MirroredFront,
        validateValue: static (_, value) => value is PageBacksideMode mode
            && Enum.IsDefined(mode),
        propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty BacksideColorProperty = BindableProperty.Create(
        nameof(BacksideColor),
        typeof(Color),
        typeof(PageTurnView),
        Colors.White,
        propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty BacksideDimFactorProperty = BindableProperty.Create(
        nameof(BacksideDimFactor),
        typeof(float),
        typeof(PageTurnView),
        0.82f,
        coerceValue: static (_, value) => Math.Clamp((float)value, 0, 1),
        propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty ContentScaleProperty = BindableProperty.Create(
        nameof(ContentScale),
        typeof(PageContentScale),
        typeof(PageTurnView),
        PageContentScale.Fill,
        propertyChanged: OnVisualPropertyChanged);

    public static readonly BindableProperty SurfaceColorProperty = BindableProperty.Create(
        nameof(SurfaceColor),
        typeof(Color),
        typeof(PageTurnView),
        Color.FromArgb("#EBEBEB"),
        propertyChanged: OnVisualPropertyChanged);

    private readonly PageCurlOptions _options;
    private readonly PageMeshBuffer _meshBuffer;
    private static readonly SKSamplingOptions ImageSampling = new(SKFilterMode.Linear, SKMipmapMode.Linear);
    private SKPaint? _frontPaint;
    private SKPaint? _backPaint;
    private SKPaint? _imagePaint;
    private SKPaint? _placeholderPaint;
    private readonly SKPoint[] _frontPoints;
    private readonly SKPoint[] _frontTexturePoints;
    private readonly SKColor[] _frontColors;
    private readonly SKPoint[] _backPoints;
    private readonly SKPoint[] _backTexturePoints;
    private readonly SKColor[] _backColors;
    private SKShader? _frontShader;
    private SKShader? _backShader;
    private SKImage? _frontShaderImage;
    private SKImage? _backShaderImage;
    private Vector2 _canvasSize;
    private PageViewportRect _contentViewport;
    private readonly PageTurnPointerCapture _pointerCapture = new();
    private bool _animationLoopActive;
    private int _animationGeneration;
    private bool _eventsAttached;
    private bool _handlerDisconnected;
    private bool _syncingAnchor;
    private int _surfaceGeneration;
    private int _previousFrontVertexCount;
    private int _previousBackVertexCount;
    private long _frameCount;
    private double _lastFrameMilliseconds;
    private double _totalFrameMilliseconds;
    private long _lastFrameAllocatedBytes;
    private long _maxFrameAllocatedBytes;

    public PageTurnView()
        : this(new PageCurlOptions())
    {
    }

    public PageTurnView(PageCurlOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
        _meshBuffer = new PageMeshBuffer(options);
        Controller = new PageTurnController(options);
        _frontPoints = new SKPoint[_meshBuffer.FrontVertices.Length];
        _frontTexturePoints = new SKPoint[_meshBuffer.FrontVertices.Length];
        _frontColors = new SKColor[_meshBuffer.FrontVertices.Length];
        _backPoints = new SKPoint[_meshBuffer.BackVertices.Length];
        _backTexturePoints = new SKPoint[_meshBuffer.BackVertices.Length];
        _backColors = new SKColor[_meshBuffer.BackVertices.Length];

        PageChangeThreshold = options.PageChangeThreshold;
        FlickEnabled = options.FlickEnabled;
        FlickVelocityThreshold = options.FlickVelocityThreshold;

        EnableTouchEvents = true;
        AttachViewHandlers();
        EnsureGraphicsResources();
    }

    public SKImage? FrontImage
    {
        get => (SKImage?)GetValue(FrontImageProperty);
        set => SetValue(FrontImageProperty, value);
    }

    public SKImage? BackImage
    {
        get => (SKImage?)GetValue(BackImageProperty);
        set => SetValue(BackImageProperty, value);
    }

    public SKImage? UnderPageImage
    {
        get => (SKImage?)GetValue(UnderPageImageProperty);
        set => SetValue(UnderPageImageProperty, value);
    }

    public bool ReduceMotion
    {
        get => (bool)GetValue(ReduceMotionProperty);
        set => SetValue(ReduceMotionProperty, value);
    }

    /// <summary>Locks a physical edge as the stationary hinge for gestures.</summary>
    public PageEdge LockedEdge
    {
        get => (PageEdge)GetValue(LockedEdgeProperty);
        set => SetValue(LockedEdgeProperty, value);
    }

    /// <summary>Locks an edge or pins a corner for gestures.</summary>
    public PageAnchor LockedAnchor
    {
        get => (PageAnchor)GetValue(LockedAnchorProperty);
        set => SetValue(LockedAnchorProperty, value);
    }

    /// <summary>Normalized page travel required to make release commit.</summary>
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

    /// <summary>Page width divided by height. Zero uses image dimensions.</summary>
    public float PageAspectRatio
    {
        get => (float)GetValue(PageAspectRatioProperty);
        set => SetValue(PageAspectRatioProperty, value);
    }

    public PageHorizontalPlacement PageHorizontalPlacement
    {
        get => (PageHorizontalPlacement)GetValue(PageHorizontalPlacementProperty);
        set => SetValue(PageHorizontalPlacementProperty, value);
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

    /// <summary>Controls whether page images fill or preserve their aspect ratio.</summary>
    public PageContentScale ContentScale
    {
        get => (PageContentScale)GetValue(ContentScaleProperty);
        set => SetValue(ContentScaleProperty, value);
    }

    public Color SurfaceColor
    {
        get => (Color)GetValue(SurfaceColorProperty);
        set => SetValue(SurfaceColorProperty, value);
    }

    /// <summary>Optional page revealed while the user turns backward.</summary>
    public static readonly BindableProperty PreviousPageImageProperty = BindableProperty.Create(
        nameof(PreviousPageImage),
        typeof(SKImage),
        typeof(PageTurnView),
        default(SKImage),
        propertyChanged: OnVisualPropertyChanged);

    public SKImage? PreviousPageImage
    {
        get => (SKImage?)GetValue(PreviousPageImageProperty);
        set => SetValue(PreviousPageImageProperty, value);
    }

    public PageTurnController Controller { get; }

    public PageCurlOptions Options => _options;

    /// <summary>
    /// Gets the pixel dimensions of the most recently painted surface. It is
    /// zero until the view has produced its first paint frame.
    /// </summary>
    public Vector2 LastCanvasSize => _canvasSize;

    /// <summary>Gets the most recently painted page-content rectangle.</summary>
    public PageViewportRect LastContentViewport => _contentViewport;

    public bool PerformanceDiagnosticsEnabled { get; set; }

    public PageTurnPerformanceSnapshot PerformanceSnapshot => new(
        _frameCount,
        _lastFrameMilliseconds,
        _frameCount == 0 ? 0 : _totalFrameMilliseconds / _frameCount,
        _lastFrameAllocatedBytes,
        _maxFrameAllocatedBytes);

    public void ResetPerformanceSnapshot()
    {
        _frameCount = 0;
        _lastFrameMilliseconds = 0;
        _totalFrameMilliseconds = 0;
        _lastFrameAllocatedBytes = 0;
        _maxFrameAllocatedBytes = 0;
    }

    public event EventHandler<PageTurnCompletedEventArgs>? PageTurnCompleted
    {
        add => Controller.PageTurnCompleted += value;
        remove => Controller.PageTurnCompleted -= value;
    }

    public event EventHandler<PageChangeThresholdCrossedEventArgs>? PageChangeThresholdCrossed
    {
        add => Controller.PageChangeThresholdCrossed += value;
        remove => Controller.PageChangeThresholdCrossed -= value;
    }

    public bool TryTurn(PageTurnDirection direction)
    {
        var pageSize = _contentViewport.Size;
        if (!IsValidPageSize(pageSize))
        {
            var canvasSize = new Vector2((float)Math.Max(Width, 0), (float)Math.Max(Height, 0));
            pageSize = GetContentViewport(canvasSize).Size;
        }

        return TryTurn(direction, pageSize);
    }

    internal bool TryTurn(PageTurnDirection direction, Vector2 pageSize)
    {
        if (_handlerDisconnected) return false;

        if (!Controller.BeginProgrammaticTurn(pageSize, direction, animate: !ReduceMotion))
        {
            return false;
        }

        StartAnimationLoop();
        InvalidateSurface();
        return true;
    }

    public void CancelInteraction()
    {
        _pointerCapture.Cancel();
        StopAnimationLoop();
        Controller.Cancel(animate: false);
        InvalidateSurface();
    }

    internal bool TryBeginInteraction(
        Vector2 pointer,
        Vector2 pageSize,
        PageAnchor lockedAnchor)
    {
        if (_handlerDisconnected
            || Controller.Snapshot.State != PageTurnState.Idle
            || !Controller.BeginDrag(pointer, pageSize, lockedAnchor))
        {
            return false;
        }

        InvalidateSurface();
        return true;
    }

    internal void UpdateInteraction(Vector2 pointer)
    {
        Controller.UpdateDrag(
            pointer,
            Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency);
        InvalidateSurface();
    }

    internal void EndInteraction(bool cancelled)
    {
        if (cancelled)
        {
            Controller.Cancel();
        }
        else
        {
            Controller.EndDrag(animate: !ReduceMotion);
        }

        StartAnimationLoop();
        InvalidateSurface();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        _surfaceGeneration++;
        if (Handler is null)
        {
            _handlerDisconnected = true;
            _pointerCapture.Cancel();
            StopAnimationLoop();
            Controller.Cancel(animate: false);
            DetachViewHandlers();
            ReleaseGraphicsResources();
            return;
        }

        _handlerDisconnected = false;
        AttachViewHandlers();
        EnsureGraphicsResources();
        StartAnimationLoop();
        InvalidateSurface();
        QueueSurfaceRecovery();
    }

    private static void OnVisualPropertyChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        var view = (PageTurnView)bindable;
        if (!view._handlerDisconnected)
        {
            view.InvalidateSurface();
        }
    }

    private static void OnInteractionOptionChanged(
        BindableObject bindable,
        object? oldValue,
        object? newValue)
    {
        var view = (PageTurnView)bindable;
        view._options.PageChangeThreshold = view.PageChangeThreshold;
        view._options.FlickEnabled = view.FlickEnabled;
        view._options.FlickVelocityThreshold = view.FlickVelocityThreshold;
    }

    private static void OnLockedEdgeChanged(
        BindableObject bindable,
        object? oldValue,
        object? newValue)
    {
        var view = (PageTurnView)bindable;
        if (!view._syncingAnchor)
        {
            view._syncingAnchor = true;
            view.LockedAnchor = PageAnchorExtensions.FromEdge((PageEdge)newValue!);
            view._syncingAnchor = false;
        }

        OnVisualPropertyChanged(bindable, oldValue, newValue);
    }

    private static void OnLockedAnchorChanged(
        BindableObject bindable,
        object? oldValue,
        object? newValue)
    {
        var view = (PageTurnView)bindable;
        if (!view._syncingAnchor)
        {
            view._syncingAnchor = true;
            var anchor = (PageAnchor)newValue!;
            view.LockedEdge = anchor.TryGetEdge(out var edge) ? edge : PageEdge.Auto;
            view._syncingAnchor = false;
        }

        OnVisualPropertyChanged(bindable, oldValue, newValue);
    }

    private void OnPaintSurface(object? sender, SKPaintGLSurfaceEventArgs e)
    {
        if (_handlerDisconnected)
        {
            return;
        }

        var measure = PerformanceDiagnosticsEnabled;
        var startTimestamp = measure ? Stopwatch.GetTimestamp() : 0;
        var startAllocatedBytes = measure ? GC.GetAllocatedBytesForCurrentThread() : 0;
        try
        {
            EnsureGraphicsResources();
            var canvas = e.Surface.Canvas;
            var info = e.Info;
            _canvasSize = new Vector2(MathF.Max(info.Width, 0), MathF.Max(info.Height, 0));
            canvas.Clear(ToSkColor(SurfaceColor));

            _contentViewport = GetContentViewport(_canvasSize);
            var pageRect = ToSkRect(_contentViewport);
            var pageSize = _contentViewport.Size;
            var snapshot = Controller.Snapshot;
            var underPageImage = snapshot.Direction == PageTurnDirection.Backward
                ? PreviousPageImage ?? UnderPageImage
                : UnderPageImage;
            DrawImage(canvas, underPageImage, pageRect);

            if (snapshot.State == PageTurnState.Idle)
            {
                DrawImage(canvas, FrontImage, pageRect);
                return;
            }

            var pose = Controller.GetCurlPose(pageSize);
            PageCurlGeometry.Build(_meshBuffer, pageSize, pose, _options);
            var offset = new SKPoint(_contentViewport.Origin.X, _contentViewport.Origin.Y);
            DrawMesh(
                canvas,
                _meshBuffer.FrontVertices,
                _meshBuffer.FrontVertexCount,
                FrontImage,
                PageBacksideMode.CustomContent,
                1,
                SKColors.White,
                _frontPaint!,
                ref _frontShader,
                ref _frontShaderImage,
                _frontPoints,
                _frontTexturePoints,
                _frontColors,
                offset,
                PageEdge.Auto,
                ref _previousFrontVertexCount);
            var backsideImage = BacksideMode switch
            {
                PageBacksideMode.MirroredFront => FrontImage,
                PageBacksideMode.CustomContent => BackImage ?? FrontImage,
                _ => null,
            };
            DrawMesh(
                canvas,
                _meshBuffer.BackVertices,
                _meshBuffer.BackVertexCount,
                backsideImage,
                BacksideMode,
                BacksideDimFactor,
                ToSkColor(BacksideColor),
                _backPaint!,
                ref _backShader,
                ref _backShaderImage,
                _backPoints,
                _backTexturePoints,
                _backColors,
                offset,
                snapshot.FreeEdge,
                ref _previousBackVertexCount);
        }
        finally
        {
            if (measure)
            {
                var elapsedMilliseconds = (Stopwatch.GetTimestamp() - startTimestamp) * 1000d / Stopwatch.Frequency;
                var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - startAllocatedBytes;
                _frameCount++;
                _lastFrameMilliseconds = elapsedMilliseconds;
                _totalFrameMilliseconds += elapsedMilliseconds;
                _lastFrameAllocatedBytes = allocatedBytes;
                _maxFrameAllocatedBytes = Math.Max(_maxFrameAllocatedBytes, allocatedBytes);
            }
        }
    }

    private void OnTouch(object? sender, SKTouchEventArgs e)
    {
        if (_handlerDisconnected)
        {
            return;
        }

        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                if (!TryGetPagePointer(e.Location, out var pressedPointer, out var pageSize))
                {
                    break;
                }

                if (!_pointerCapture.TryCapture(e.Id))
                {
                    e.Handled = true;
                    break;
                }

                if (Controller.BeginDrag(pressedPointer, pageSize, LockedAnchor))
                {
                    e.Handled = true;
                    InvalidateSurface();
                }
                else
                {
                    _pointerCapture.Cancel();
                }

                break;
            case SKTouchAction.Moved:
                if (_pointerCapture.Owns(e.Id))
                {
                    UpdateInteraction(ToPagePointer(e.Location));
                    e.Handled = true;
                }

                break;
            case SKTouchAction.Released:
            case SKTouchAction.Cancelled:
                if (_pointerCapture.Release(e.Id))
                {
                    EndInteraction(cancelled: e.ActionType == SKTouchAction.Cancelled);
                    e.Handled = true;
                }

                break;
        }
    }

    private void StartAnimationLoop()
    {
        if (_handlerDisconnected || _animationLoopActive || Controller.Snapshot.State != PageTurnState.Settling || Dispatcher is null)
        {
            return;
        }

        _animationLoopActive = true;
        var generation = ++_animationGeneration;
        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(16), () =>
        {
            if (_handlerDisconnected || Handler is null || generation != _animationGeneration)
            {
                if (generation == _animationGeneration)
                {
                    _animationLoopActive = false;
                }

                return false;
            }

            var active = Controller.Tick(1.0 / 60.0);
            InvalidateSurface();
            if (generation == _animationGeneration)
            {
                _animationLoopActive = active;
            }

            return active;
        });
    }

    private void DrawImage(SKCanvas canvas, SKImage? image, SKRect destination)
    {
        if (image is null)
        {
            canvas.DrawRect(destination, _placeholderPaint!);
            return;
        }

        canvas.DrawImage(image, destination, ImageSampling, _imagePaint!);
    }

    private static void DrawMesh(
        SKCanvas canvas,
        MeshVertex[] vertices,
        int count,
        SKImage? image,
        PageBacksideMode backsideMode,
        float dimFactor,
        SKColor solidColor,
        SKPaint paint,
        ref SKShader? shader,
        ref SKImage? shaderImage,
        SKPoint[] points,
        SKPoint[] texturePoints,
        SKColor[] colors,
        SKPoint offset,
        PageEdge freeEdge,
        ref int previousCount)
    {
        if (count < 3)
        {
            return;
        }

        EnsureShader(image, ref shader, ref shaderImage);
        paint.Shader = shader;
        var imageWidth = image?.Width ?? 1;
        var imageHeight = image?.Height ?? 1;
        for (var i = 0; i < count; i++)
        {
            var vertex = vertices[i];
            points[i] = new SKPoint(vertex.Position.X + offset.X, vertex.Position.Y + offset.Y);
            var texture = vertex.TextureCoordinate;
            if (backsideMode == PageBacksideMode.CustomContent)
            {
                texture = freeEdge switch
                {
                    PageEdge.Left or PageEdge.Right => new(1 - texture.X, texture.Y),
                    PageEdge.Top or PageEdge.Bottom => new(texture.X, 1 - texture.Y),
                    _ => texture,
                };
            }
            var sample = PageBacksideMapping.Map(
                backsideMode,
                texture,
                vertex.Shade,
                dimFactor);
            texturePoints[i] = new SKPoint(
                sample.TextureCoordinate.X * imageWidth,
                sample.TextureCoordinate.Y * imageHeight);
            var shade = Math.Clamp(sample.Shade, 0, 1);
            colors[i] = sample.UsesTexture && image is not null
                ? ShadeColor(SKColors.White, shade)
                : ShadeColor(solidColor, shade);
        }

        // The public Skia API consumes whole arrays. Only the previously used
        // tail needs clearing; untouched storage is already transparent.
        for (var i = count; i < previousCount; i++)
        {
            points[i] = default;
            texturePoints[i] = default;
            colors[i] = SKColors.Transparent;
        }

        previousCount = count;
        // The array draw overload leaves the native vertex copy to finalization.
        // Own that copy explicitly so large frames release it promptly.
        using var mesh = SKVertices.CreateCopy(SKVertexMode.Triangles, points, texturePoints, colors);
        canvas.DrawVertices(mesh, SKBlendMode.Modulate, paint);
        paint.Shader = null;
    }

    private static void EnsureShader(SKImage? image, ref SKShader? shader, ref SKImage? shaderImage)
    {
        if (ReferenceEquals(image, shaderImage))
        {
            return;
        }

        shader?.Dispose();
        shader = image?.ToShader(SKShaderTileMode.Clamp, SKShaderTileMode.Clamp);
        shaderImage = image;
    }

    private void AttachViewHandlers()
    {
        if (_eventsAttached || _handlerDisconnected)
        {
            return;
        }

        PaintSurface += OnPaintSurface;
        Touch += OnTouch;
        SizeChanged += OnSizeChanged;
        _eventsAttached = true;
    }

    private void DetachViewHandlers()
    {
        if (!_eventsAttached)
        {
            return;
        }

        PaintSurface -= OnPaintSurface;
        Touch -= OnTouch;
        SizeChanged -= OnSizeChanged;
        _eventsAttached = false;
    }

    private void OnSizeChanged(object? sender, EventArgs e) => InvalidateSurface();

    private void QueueSurfaceRecovery()
    {
        var generation = _surfaceGeneration;
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(1500), () =>
        {
            if (generation == _surfaceGeneration
                && !_handlerDisconnected
                && Handler is not null)
            {
                // An iOS GL surface can reject its first draw while the native
                // window is still becoming active. One on-demand retry avoids
                // a blank idle page without enabling a permanent render loop.
                InvalidateSurface();
            }
        });
    }

    private void EnsureGraphicsResources()
    {
        _frontPaint ??= new SKPaint { IsAntialias = true };
        _backPaint ??= new SKPaint { IsAntialias = true };
        _imagePaint ??= new SKPaint { Color = SKColors.White, IsAntialias = true };
        _placeholderPaint ??= new SKPaint { Color = new SKColor(250, 250, 250), IsAntialias = true };
    }

    private void ReleaseGraphicsResources()
    {
        _frontShader?.Dispose();
        _frontShader = null;
        _frontShaderImage = null;
        _backShader?.Dispose();
        _backShader = null;
        _backShaderImage = null;
        _frontPaint?.Dispose();
        _frontPaint = null;
        _backPaint?.Dispose();
        _backPaint = null;
        _imagePaint?.Dispose();
        _imagePaint = null;
        _placeholderPaint?.Dispose();
        _placeholderPaint = null;
    }

    private void StopAnimationLoop()
    {
        _animationGeneration++;
        _animationLoopActive = false;
    }

    private PageViewportRect GetContentViewport(Vector2 canvasSize)
    {
        if (!IsValidPageSize(canvasSize))
        {
            return default;
        }

        var image = FrontImage ?? UnderPageImage;
        if (PageAspectRatio > 0 || ContentScale == PageContentScale.AspectFit)
        {
            var intrinsicSize = image is null
                ? Vector2.Zero
                : new Vector2(image.Width, image.Height);
            var fitted = PageAspectLayout.Fit(canvasSize, PageAspectRatio, intrinsicSize);
            return PageAspectLayout.PlaceHorizontal(
                fitted,
                canvasSize.X,
                PageHorizontalPlacement);
        }

        return new PageViewportRect(Vector2.Zero, canvasSize);
    }

    private bool TryGetPagePointer(SKPoint location, out Vector2 pointer, out Vector2 pageSize)
    {
        if (!_contentViewport.IsValid)
        {
            var canvasSize = new Vector2((float)Math.Max(Width, 0), (float)Math.Max(Height, 0));
            _contentViewport = GetContentViewport(canvasSize);
        }

        pageSize = _contentViewport.Size;
        if (!IsValidPageSize(pageSize))
        {
            pointer = default;
            return false;
        }

        var raw = new Vector2(location.X, location.Y);
        var right = _contentViewport.Origin.X + pageSize.X;
        var bottom = _contentViewport.Origin.Y + pageSize.Y;
        if (ContentScale == PageContentScale.AspectFit
            && (raw.X < _contentViewport.Origin.X
                || raw.X > right
                || raw.Y < _contentViewport.Origin.Y
                || raw.Y > bottom))
        {
            pointer = default;
            return false;
        }

        pointer = raw - _contentViewport.Origin;
        return true;
    }

    private Vector2 ToPagePointer(SKPoint location)
    {
        var viewport = _contentViewport.IsValid
            ? _contentViewport
            : GetContentViewport(new Vector2((float)Math.Max(Width, 0), (float)Math.Max(Height, 0)));
        return new Vector2(location.X - viewport.Origin.X, location.Y - viewport.Origin.Y);
    }

    private static SKRect ToSkRect(PageViewportRect viewport) => new(
        viewport.Origin.X,
        viewport.Origin.Y,
        viewport.Origin.X + viewport.Size.X,
        viewport.Origin.Y + viewport.Size.Y);

    private static bool IsValidPageSize(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && value.X > 0 && value.Y > 0;

    private static SKColor ToSkColor(Color color) => new(
        ToByte(color.Red),
        ToByte(color.Green),
        ToByte(color.Blue),
        ToByte(color.Alpha));

    private static byte ToByte(float component) =>
        (byte)Math.Clamp((int)MathF.Round(component * 255), 0, 255);

    private static SKColor ShadeColor(SKColor color, float shade) => new(
        (byte)Math.Clamp((int)MathF.Round(color.Red * shade), 0, 255),
        (byte)Math.Clamp((int)MathF.Round(color.Green * shade), 0, 255),
        (byte)Math.Clamp((int)MathF.Round(color.Blue * shade), 0, 255),
        color.Alpha);
}
