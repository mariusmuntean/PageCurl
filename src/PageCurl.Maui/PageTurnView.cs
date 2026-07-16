using System.Numerics;
using Microsoft.Maui.Controls;
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
        propertyChanged: OnImageChanged);

    public static readonly BindableProperty BackImageProperty = BindableProperty.Create(
        nameof(BackImage),
        typeof(SKImage),
        typeof(PageTurnView),
        default(SKImage),
        propertyChanged: OnImageChanged);

    public static readonly BindableProperty UnderPageImageProperty = BindableProperty.Create(
        nameof(UnderPageImage),
        typeof(SKImage),
        typeof(PageTurnView),
        default(SKImage),
        propertyChanged: OnImageChanged);

    private readonly PageCurlOptions _options;
    private readonly PageMeshBuffer _meshBuffer;
    private SKPaint? _frontPaint;
    private SKPaint? _backPaint;
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
    private readonly PageTurnPointerCapture _pointerCapture = new();
    private bool _animationLoopActive;
    private int _animationGeneration;
    private bool _eventsAttached;
    private bool _disposed;

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

    /// <summary>Optional page revealed while the user turns backward.</summary>
    public static readonly BindableProperty PreviousPageImageProperty = BindableProperty.Create(
        nameof(PreviousPageImage),
        typeof(SKImage),
        typeof(PageTurnView),
        default(SKImage),
        propertyChanged: OnImageChanged);

    public SKImage? PreviousPageImage
    {
        get => (SKImage?)GetValue(PreviousPageImageProperty);
        set => SetValue(PreviousPageImageProperty, value);
    }

    public PageTurnController Controller { get; }

    public PageCurlOptions Options => _options;

    public event EventHandler<PageTurnCompletedEventArgs>? PageTurnCompleted
    {
        add => Controller.PageTurnCompleted += value;
        remove => Controller.PageTurnCompleted -= value;
    }

    public void CancelInteraction()
    {
        _pointerCapture.Cancel();
        Controller.Cancel();
        StartAnimationLoop();
        InvalidateSurface();
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        if (_disposed)
        {
            return;
        }

        if (Handler is null)
        {
            _pointerCapture.Cancel();
            Controller.Cancel();
            StopAnimationLoop();
            DetachViewHandlers();
            ReleaseGraphicsResources();
            return;
        }

        AttachViewHandlers();
        EnsureGraphicsResources();
        StartAnimationLoop();
        InvalidateSurface();
    }

    private static void OnImageChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        var view = (PageTurnView)bindable;
        if (!view._disposed)
        {
            view.InvalidateSurface();
        }
    }

    private void OnPaintSurface(object? sender, SKPaintGLSurfaceEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        EnsureGraphicsResources();
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        _canvasSize = new Vector2(MathF.Max(info.Width, 0), MathF.Max(info.Height, 0));
        canvas.Clear(new SKColor(235, 235, 235));

        var pageRect = new SKRect(0, 0, _canvasSize.X, _canvasSize.Y);
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

        var pose = Controller.GetCurlPose(_canvasSize);
        PageCurlGeometry.Build(_meshBuffer, _canvasSize, pose, _options);
        DrawMesh(canvas, _meshBuffer.BackVertices, _meshBuffer.BackVertexCount, BackImage ?? FrontImage, _backPaint!, ref _backShader, ref _backShaderImage, _backPoints, _backTexturePoints, _backColors);
        DrawMesh(canvas, _meshBuffer.FrontVertices, _meshBuffer.FrontVertexCount, FrontImage, _frontPaint!, ref _frontShader, ref _frontShaderImage, _frontPoints, _frontTexturePoints, _frontColors);
    }

    private void OnTouch(object? sender, SKTouchEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        var pageSize = _canvasSize;
        if (!IsValidPageSize(pageSize))
        {
            pageSize = new Vector2((float)Math.Max(Width, 0), (float)Math.Max(Height, 0));
        }

        var pointer = new Vector2(e.Location.X, e.Location.Y);
        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                if (!_pointerCapture.TryCapture(e.Id))
                {
                    e.Handled = true;
                    break;
                }

                if (Controller.BeginDrag(pointer, pageSize))
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
                    Controller.UpdateDrag(pointer);
                    e.Handled = true;
                    InvalidateSurface();
                }

                break;
            case SKTouchAction.Released:
                if (_pointerCapture.Release(e.Id))
                {
                    Controller.EndDrag();
                    e.Handled = true;
                    StartAnimationLoop();
                    InvalidateSurface();
                }

                break;
            case SKTouchAction.Cancelled:
                if (_pointerCapture.Release(e.Id))
                {
                    Controller.Cancel();
                    e.Handled = true;
                    StartAnimationLoop();
                    InvalidateSurface();
                }

                break;
        }
    }

    private void StartAnimationLoop()
    {
        if (_disposed || _animationLoopActive || Controller.Snapshot.State != PageTurnState.Settling || Dispatcher is null)
        {
            return;
        }

        _animationLoopActive = true;
        var generation = ++_animationGeneration;
        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(16), () =>
        {
            if (_disposed || Handler is null || generation != _animationGeneration)
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

        canvas.DrawImage(image, destination);
    }

    private static void DrawMesh(
        SKCanvas canvas,
        MeshVertex[] vertices,
        int count,
        SKImage? image,
        SKPaint paint,
        ref SKShader? shader,
        ref SKImage? shaderImage,
        SKPoint[] points,
        SKPoint[] texturePoints,
        SKColor[] colors)
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
            points[i] = new SKPoint(vertex.Position.X, vertex.Position.Y);
            texturePoints[i] = new SKPoint(
                vertex.TextureCoordinate.X * imageWidth,
                vertex.TextureCoordinate.Y * imageHeight);
            var shade = (byte)Math.Clamp((int)MathF.Round(vertex.Shade * 255), 25, 255);
            colors[i] = new SKColor(shade, shade, shade, 255);
        }

        // The SkiaSharp array overload uses the complete array length. Fill
        // the unused tail with transparent degenerate vertices so the view can
        // reuse fixed buffers without allocating on every frame.
        for (var i = count; i < points.Length; i++)
        {
            points[i] = default;
            texturePoints[i] = default;
            colors[i] = SKColors.Transparent;
        }

        canvas.DrawVertices(
            SKVertexMode.Triangles,
            points,
            texturePoints,
            colors,
            SKBlendMode.Modulate,
            null!,
            paint);
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
        if (_eventsAttached || _disposed)
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

    private void EnsureGraphicsResources()
    {
        _frontPaint ??= new SKPaint { IsAntialias = true };
        _backPaint ??= new SKPaint { IsAntialias = true };
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
        _placeholderPaint?.Dispose();
        _placeholderPaint = null;
    }

    private void StopAnimationLoop()
    {
        _animationGeneration++;
        _animationLoopActive = false;
    }

    private static bool IsValidPageSize(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && value.X > 0 && value.Y > 0;
}
