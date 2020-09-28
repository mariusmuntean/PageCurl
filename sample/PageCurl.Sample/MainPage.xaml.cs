using PageCurl.Sample.media;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;
using SkiaSharp;
using System.IO;
using System;

namespace PageCurl.Sample
{
    public partial class MainPage : ContentPage
    {
        private readonly MediaTool _mediaTool;

        private SKBitmap _pageBitmap;
        private readonly SKCanvasView _sKCanvasView;
        private Switch _fileSwitch;
        private readonly Slider _slider;

        private const float PI = (float) Math.PI;

        private readonly SKPaint _pagePaint = new SKPaint
        {
            // ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
            // {
            //     0.21f, 0.72f, 0.07f, 0, 0,
            //     0.21f, 0.72f, 0.07f, 0, 0,
            //     0.21f, 0.72f, 0.07f, 0, 0,
            //     0, 0, 0, 1, 0
            // }),
        };

        private float d = 0.0f;
        private readonly int sliceCount = 10;
        private float s = 0.0f;
        private readonly float r = 120.0f;

        public MainPage()
        {
            InitializeComponent();

            _mediaTool = new MediaTool();

            _sKCanvasView = new SKCanvasView();
            _sKCanvasView.PaintSurface += Paint;

            var grid = new Grid();
            grid.Children.Add(_sKCanvasView);
            _slider = new Slider
            {
                VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(10.0d)
            };
            _slider.ValueChanged += (object sender, ValueChangedEventArgs e) =>
            {
                d = (float) e.NewValue;
                _sKCanvasView.InvalidateSurface();
            };
            grid.Children.Add(_slider);
            _fileSwitch = new Switch();
            _fileSwitch.Toggled += (sender, args) => ReloadImage();
            _fileSwitch.VerticalOptions = LayoutOptions.Start;
            _fileSwitch.HorizontalOptions = LayoutOptions.End;
            _fileSwitch.Margin = new Thickness(22.0f);
            grid.Children.Add(_fileSwitch);
            Content = grid;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            ReloadImage();
        }

        private void ReloadImage()
        {
            var fileName = _fileSwitch.IsToggled ? "page3.jpeg" : "page2.png";
            using var imageStream = _mediaTool.GetMedia(fileName);
            _pageBitmap = SKBitmap.Decode(imageStream ?? Stream.Null);
            _slider.Maximum = _pageBitmap.Width;
            _slider.Value = _pageBitmap.Width / 2.0f;

            _sKCanvasView.InvalidateSurface();
        }

        private void Paint(object sender, SKPaintSurfaceEventArgs e)
        {
            var info = e.Info;
            var surface = e.Surface;
            var canvas = surface.Canvas;

            canvas.Clear();

            if (_pageBitmap != null && d > 0.0f)
            {
                // Compute curl parameters
                var cylinderLength = 2 * PI * r;
                s = cylinderLength * 0.5f / sliceCount;

                // Draw image
                var x = _pageBitmap.Width / 2.0f;
                var y = _pageBitmap.Height / 2.0f;

                var cHalfWidth = info.Width / 2.0f;
                var cHalfHeight = info.Height / 2.0f;

                var hScale = info.Width / (float) _pageBitmap.Width;
                var vScale = info.Height / (float) _pageBitmap.Height;
                var scale = Math.Min(hScale, vScale);

                // a
                canvas.Save();

                canvas.Translate(cHalfWidth, cHalfHeight);

                canvas.Scale(scale);

                canvas.DrawBitmap(_pageBitmap, SKRect.Create(d - (s / 2.0f), _pageBitmap.Height), SKRect.Create(-x, -y, d - (s / 2.0f), _pageBitmap.Height));

                canvas.Restore();

                // b
                var rotationStepDegrees = 180.0f / sliceCount;
                for (var i = 0; i < sliceCount + 1; i++)
                {
                    // 3D stuff
                    var transform = SKMatrix44.CreateIdentity();

                    // Translation to cylinder center
                    transform.PostConcat(SKMatrix44.CreateTranslate(0, 0, r));

                    // Rotate
                    transform.PostConcat(SKMatrix44.CreateRotationDegrees(0, 1, 0, i * rotationStepDegrees));

                    // Translate back into XY plane
                    transform.PostConcat(SKMatrix44.CreateTranslate(0, 0, -r));

                    // Scale
                    transform.PostConcat(SKMatrix44.CreateScale(scale, scale, 1));

                    // Translate to curl point
                    transform.PostConcat(SKMatrix44.CreateTranslate(cHalfWidth - (x * scale) + (d * scale) - (s * scale / 2.0f), cHalfHeight, 0.0f));

                    // Perspective?
                    // var perspectiveMatrix = SKMatrix44.CreateIdentity();
                    // perspectiveMatrix[3, 2] = -1 / 1500.0f;
                    // m44rot.PostConcat(perspectiveMatrix);

                    canvas.Save();

                    // Apply transform
                    canvas.SetMatrix(canvas.TotalMatrix.PostConcat(transform.Matrix));

                    // Draw
                    canvas.DrawBitmap(_pageBitmap,
                        source: SKRect.Create(d - (s / 2.0f) + (s * (i - 0.5f)), 0, s, _pageBitmap.Height),
                        dest: SKRect.Create(
                            -(s / 2.0f),
                            -(_pageBitmap.Height / 2.0f),
                            s * 1.01f, // 1% wider to cover up the gaps in between the slices
                            _pageBitmap.Height),
                        paint: _pagePaint);

                    canvas.Restore();
                }

                // c
                canvas.Save();

                var restTransform = SKMatrix44.CreateIdentity();
                // restTransform.PostConcat(SKMatrix44.CreateRotationDegrees(0,1,0,180.0f));
                restTransform.PostConcat(SKMatrix44.CreateScale(scale, scale, 1));
                restTransform.PostConcat(SKMatrix44.CreateTranslate(cHalfWidth - (x * scale) + (d * scale) - (s * scale / 2.0f), cHalfHeight, 0.0f));

                canvas.SetMatrix(canvas.TotalMatrix.PostConcat(restTransform.Matrix));

                var restImageWidth = Math.Max(0.0f, _pageBitmap.Width - d - (cylinderLength / 2.0f) + (s / 2.0f));
                var mirrored = GetMirrored(_pageBitmap);
                var restImage = GetRegion(mirrored, SKRectI.Create(0, 0, (int) restImageWidth, mirrored.Height));

                canvas.DrawBitmap(restImage,
                    SKRect.Create(-restImageWidth, -restImage.Height / 2.0f, restImageWidth, restImage.Height));

                // Create linear gradient from left to right
                // _pagePaint.Shader = SKShader.CreateLinearGradient(
                //     new SKPoint(-restImageWidth, 0.0f),
                //     new SKPoint(r, 0.0f),
                //     new SKColor[] {SKColors.Transparent, SKColors.Black},
                //     new[] {0.3f, 1.0f},
                //     SKShaderTileMode.Repeat);
                // canvas.DrawRect(SKRect.Create(-restImageWidth, -restImage.Height / 2.0f, restImageWidth + r, restImage.Height), _pagePaint);

                canvas.Restore();
            }
        }

        private SKBitmap GetMirrored(SKBitmap original)
        {
            var flippedBitmap = new SKBitmap(original.Width, original.Height);

            using (var canvas = new SKCanvas(flippedBitmap))
            {
                canvas.Clear();
                canvas.Scale(-1, 1, original.Width / 2.0f, 0.0f);
                canvas.DrawBitmap(original, new SKPoint());
            }

            return flippedBitmap;
        }

        private SKBitmap GetRegion(SKBitmap original, SKRectI rect)
        {
            var flippedBitmap = new SKBitmap(rect.Width, rect.Height);

            using (var canvas = new SKCanvas(flippedBitmap))
            {
                canvas.Clear();
                canvas.DrawBitmap(original, rect, SKRect.Create(0, 0, rect.Width, rect.Height));
            }

            return flippedBitmap;
        }
    }
}