using PageCurl.Sample.media;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;
using SkiaSharp;
using System.IO;
using System;
using System.Numerics;

namespace PageCurl.Sample
{
    public partial class MainPage : ContentPage
    {
        readonly MediaTool _mediaTool;

        private SKBitmap _pageBitmap;
        readonly SKCanvasView _sKCanvasView;
        Slider _slider;

        private const float PI = (float) Math.PI;

        private SKPaint _pagePaint = new SKPaint
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
            {
                0.21f, 0.72f, 0.07f, 0, 0,
                0.21f, 0.72f, 0.07f, 0, 0,
                0.21f, 0.72f, 0.07f, 0, 0,
                0, 0, 0, 1, 0
            })
        };

        float d = 0.0f;
        int sliceCount = 10;
        float s = 0.0f;
        float r = 120.0f;

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
            Content = grid;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // using var imageStream = _mediaTool.GetMedia("page2.png");
            using var imageStream = _mediaTool.GetMedia("page.jpg");
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
                //d = _pageBitmap.Width / 2.0f;
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

                canvas.DrawBitmap(_pageBitmap, SKRect.Create(d, _pageBitmap.Height), SKRect.Create(-x, -y, d, _pageBitmap.Height));

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
                    transform.PostConcat(SKMatrix44.CreateTranslate(cHalfWidth - (x * scale) + (d * scale) - (s / 2.0f), cHalfHeight, 0.0f));

                    // Perspective?
                    // var perspectiveMatrix = SKMatrix44.CreateIdentity();
                    // perspectiveMatrix[3, 2] = -1 / 1500.0f;
                    // m44rot.PostConcat(perspectiveMatrix);

                    canvas.Save();

                    // Apply transform
                    canvas.SetMatrix(canvas.TotalMatrix.PostConcat(transform.Matrix));

                    // Draw
                    canvas.DrawBitmap(_pageBitmap,
                        source: SKRect.Create(d - (s / 2.0f) + (s * i), 0, s, _pageBitmap.Height),
                        dest: SKRect.Create(-(s / 2.0f), -(_pageBitmap.Height / 2.0f), s, _pageBitmap.Height),
                        paint: _pagePaint);

                    canvas.Restore();
                }
            }
        }
    }
}