using System;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;

namespace PageCurl
{
    public class PageCurlView : SKCanvasView
    {
        private readonly SKCanvasView _sKCanvasView;
        private readonly Slider _slider;

        private const float PI = (float) Math.PI;

        private readonly SKPaint _topSidePagePaint = new SKPaint
        {
            // ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
            // {
            //     0.21f, 0.72f, 0.07f, 0, 0,
            //     0.21f, 0.72f, 0.07f, 0, 0,
            //     0.21f, 0.72f, 0.07f, 0, 0,
            //     0, 0, 0, 1, 0
            // }),
        };

        private readonly SKPaint _backSidePagePaint = new SKPaint
        {
            ColorFilter = SKColorFilter.CreateColorMatrix(new[]
            {
                0.75f, 0.25f, 0.25f, 0, 0,
                0.25f, 0.75f, 0.25f, 0, 0,
                0.25f, 0.25f, 0.75f, 0, 0,
                0, 0, 0, 1, 0
            })
        };


        private SKPaint _currentPaint = new SKPaint();

        private readonly int sliceCount = 10;
        private float s = 0.0f;
        private readonly float r = 120.0f;

        public PageCurlView()
        {
        }

        public SKBitmap PageBitmap { get; set; }
        public float D { get; set; } = 0.0f;

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);

            var info = e.Info;
            var surface = e.Surface;
            var canvas = surface.Canvas;

            canvas.Clear();

            if (PageBitmap != null && D > 0.0f)
            {
                // Compute curl parameters
                var cylinderLength = 2 * PI * r;
                s = cylinderLength * 0.5f / sliceCount;

                // Draw image
                var x = PageBitmap.Width / 2.0f;
                var y = PageBitmap.Height / 2.0f;

                var cHalfWidth = info.Width / 2.0f;
                var cHalfHeight = info.Height / 2.0f;

                var hScale = info.Width / (float) PageBitmap.Width;
                var vScale = info.Height / (float) PageBitmap.Height;
                var scale = Math.Min(hScale, vScale);

                // a
                canvas.Save();

                canvas.Translate(cHalfWidth, cHalfHeight);

                canvas.Scale(scale);

                canvas.DrawBitmap(PageBitmap, SKRect.Create(D - (s / 2.0f), PageBitmap.Height), SKRect.Create(-x, -y, D - (s / 2.0f), PageBitmap.Height));

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
                    transform.PostConcat(SKMatrix44.CreateTranslate(cHalfWidth - (x * scale) + (D * scale) - (s * scale / 2.0f), cHalfHeight, 0.0f));

                    // Perspective?
                    // var perspectiveMatrix = SKMatrix44.CreateIdentity();
                    // perspectiveMatrix[3, 2] = -1 / 1500.0f;
                    // m44rot.PostConcat(perspectiveMatrix);

                    canvas.Save();

                    // Apply transform
                    canvas.SetMatrix(canvas.TotalMatrix.PostConcat(transform.Matrix));
                    _currentPaint = (i * rotationStepDegrees) <= 90.0f ? _topSidePagePaint : _backSidePagePaint;
                    // Draw

                    canvas.DrawBitmap(PageBitmap,
                        source: SKRect.Create(D - (s / 2.0f) + (s * (i - 0.5f)), 0, s, PageBitmap.Height),
                        dest: SKRect.Create(
                            -(s / 2.0f),
                            -(PageBitmap.Height / 2.0f),
                            s * 1.01f, // 1% wider to cover up the gaps in between the slices
                            PageBitmap.Height),
                        paint: _currentPaint);

                    canvas.Restore();
                }

                // c
                canvas.Save();

                var restTransform = SKMatrix44.CreateIdentity();
                // restTransform.PostConcat(SKMatrix44.CreateRotationDegrees(0,1,0,180.0f));
                restTransform.PostConcat(SKMatrix44.CreateScale(scale, scale, 1));
                restTransform.PostConcat(SKMatrix44.CreateTranslate(cHalfWidth - (x * scale) + (D * scale) - (s * scale / 2.0f), cHalfHeight, 0.0f));

                canvas.SetMatrix(canvas.TotalMatrix.PostConcat(restTransform.Matrix));

                var restImageWidth = Math.Max(0.0f, PageBitmap.Width - D - (cylinderLength / 2.0f) + (s / 2.0f));
                var mirrored = GetMirrored(PageBitmap);
                var restImage = GetRegion(mirrored, SKRectI.Create(0, 0, (int) restImageWidth, mirrored.Height));

                canvas.DrawBitmap(restImage,
                    SKRect.Create(-restImageWidth, -restImage.Height / 2.0f, restImageWidth, restImage.Height),
                    _backSidePagePaint);

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