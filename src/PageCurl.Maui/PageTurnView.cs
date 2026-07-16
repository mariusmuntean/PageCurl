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
    private readonly SKPaint _frontPaint = new() { IsAntialias = true };
    private readonly SKPaint _backPaint = new() { IsAntialias = true };
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
    private bool _touchActive;
    private bool _animationLoopActive;

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
        PaintSurface += OnPaintSurface;
        Touch += OnTouch;
        SizeChanged += (_, _) => InvalidateSurface();
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

    public PageTurnController Controller { get; }

    public PageCurlOptions Options => _options;

    public event EventHandler<PageTurnCompletedEventArgs>? PageTurnCompleted
    {
        add => Controller.PageTurnCompleted += value;
        remove => Controller.PageTurnCompleted -= value;
    }

    public void CancelInteraction()
    {
        Controller.Cancel();
        StartAnimationLoop();
        InvalidateSurface();
    }

    private static void OnImageChanged(BindableObject bindable, object? oldValue, object? newValue)
    {
        ((PageTurnView)bindable).InvalidateSurface();
    }

    private void OnPaintSurface(object? sender, SKPaintGLSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        var info = e.Info;
        _canvasSize = new Vector2(MathF.Max(info.Width, 0), MathF.Max(info.Height, 0));
        canvas.Clear(new SKColor(235, 235, 235));

        var pageRect = new SKRect(0, 0, _canvasSize.X, _canvasSize.Y);
        DrawImage(canvas, UnderPageImage, pageRect);

        if (Controller.Snapshot.State == PageTurnState.Idle)
        {
            DrawImage(canvas, FrontImage, pageRect);
            return;
        }

        var pose = Controller.GetCurlPose(_canvasSize);
        PageCurlGeometry.Build(_meshBuffer, _canvasSize, pose, _options);
        DrawMesh(canvas, _meshBuffer.BackVertices, _meshBuffer.BackVertexCount, BackImage ?? FrontImage, _backPaint, ref _backShader, ref _backShaderImage, _backPoints, _backTexturePoints, _backColors);
        DrawMesh(canvas, _meshBuffer.FrontVertices, _meshBuffer.FrontVertexCount, FrontImage, _frontPaint, ref _frontShader, ref _frontShaderImage, _frontPoints, _frontTexturePoints, _frontColors);
    }

    private void OnTouch(object? sender, SKTouchEventArgs e)
    {
        var pageSize = _canvasSize;
        if (!IsValidPageSize(pageSize))
        {
            pageSize = new Vector2((float)Math.Max(Width, 0), (float)Math.Max(Height, 0));
        }

        var pointer = new Vector2(e.Location.X, e.Location.Y);
        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                _touchActive = Controller.BeginDrag(pointer, pageSize);
                if (_touchActive)
                {
                    e.Handled = true;
                    InvalidateSurface();
                }

                break;
            case SKTouchAction.Moved:
                if (_touchActive)
                {
                    Controller.UpdateDrag(pointer);
                    e.Handled = true;
                    InvalidateSurface();
                }

                break;
            case SKTouchAction.Released:
                if (_touchActive)
                {
                    Controller.EndDrag();
                    _touchActive = false;
                    e.Handled = true;
                    StartAnimationLoop();
                    InvalidateSurface();
                }

                break;
            case SKTouchAction.Cancelled:
                if (_touchActive)
                {
                    Controller.Cancel();
                    _touchActive = false;
                    e.Handled = true;
                    StartAnimationLoop();
                    InvalidateSurface();
                }

                break;
        }
    }

    private void StartAnimationLoop()
    {
        if (_animationLoopActive || Controller.Snapshot.State != PageTurnState.Settling || Dispatcher is null)
        {
            return;
        }

        _animationLoopActive = true;
        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(16), () =>
        {
            var active = Controller.Tick(1.0 / 60.0);
            InvalidateSurface();
            _animationLoopActive = active;
            return active;
        });
    }

    private static void DrawImage(SKCanvas canvas, SKImage? image, SKRect destination)
    {
        if (image is null)
        {
            using var placeholder = new SKPaint { Color = new SKColor(250, 250, 250), IsAntialias = true };
            canvas.DrawRect(destination, placeholder);
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

    private static bool IsValidPageSize(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && value.X > 0 && value.Y > 0;
}
