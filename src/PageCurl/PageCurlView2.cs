using System;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;

namespace PageCurl
{
    public class PageCurlView2 : SKCanvasView
    {
        private readonly SKCanvasView _sKCanvasView;
        private readonly Slider _slider;

        private const float PI = (float) Math.PI;
        private const double MaxAbsoluteTheta = 15.0d;
        private const double MinDragVectorLength = 30.0d;

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
        private float sliceWidth = 0.0f;
        private readonly float r = 120.0f;


        // new approach
        private SKPoint S;
        private SKPoint C;
        private float ah = 0.0f;
        private float theta = 0.0f;

        public PageCurlView2()
        {
        }

        public SKBitmap PageBitmap { get; set; }
        public float D { get; set; } = 0.0f;
        public float G { get; set; } = 0.0f;

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
                var cylinderCircumference = 2 * PI * r;
                sliceWidth = cylinderCircumference * 0.5f / sliceCount;

                S = new SKPoint(PageBitmap.Width / 2.0f, PageBitmap.Height / 2.0f);
                C = new SKPoint(D, G);
                ah = S.X - C.X;

                if (ah < MinDragVectorLength)
                {
                    theta = 0.0f;
                }
                else
                {
                    theta = AngleToOxDegrees(S, C);
                    Console.WriteLine($"Raw theta: {theta}");
                    theta = theta >= 0.0f ? 180.0f - theta : -(180 + theta);
                    // theta = theta % 180.0f;
                    theta = (float) (Math.Sign(theta) * Math.Min(Math.Abs(theta), MaxAbsoluteTheta));
                }


                // Draw image
                var pageHalfWidth = PageBitmap.Width / 2.0f;
                var pageHalfHeight = PageBitmap.Height / 2.0f;

                var canvasHalfWidth = info.Width / 2.0f;
                var canvasHalfHeight = info.Height / 2.0f;

                var hScale = info.Width / (float) PageBitmap.Width;
                var vScale = info.Height / (float) PageBitmap.Height;
                var scale = Math.Min(hScale, vScale);

                // a
                canvas.Save();

                canvas.Translate(canvasHalfWidth, canvasHalfHeight);

                canvas.Scale(scale);

                var bottomRegion = GetBottomRegion(PageBitmap, S, ah, theta);
                canvas.DrawBitmap(
                    bottomRegion,
                    // SKRect.Create(D - (sliceWidth / 2.0f), PageBitmap.Height),
                    SKRect.Create(-pageHalfWidth, -pageHalfHeight, bottomRegion.Width, bottomRegion.Height));

                // Debug
                canvas.DrawCircle(-pageHalfWidth + S.X, -pageHalfHeight + S.Y, 60, new SKPaint()
                {
                    Color = SKColors.Green,
                    Style = SKPaintStyle.Fill
                });
                canvas.DrawCircle(-pageHalfWidth + C.X, -pageHalfHeight + C.Y, 30, new SKPaint()
                {
                    Color = SKColors.Red,
                    Style = SKPaintStyle.Fill
                });

                canvas.Restore();

                // b
                // var rotationStepDegrees = 180.0f / sliceCount;
                // for (var i = 0; i < sliceCount + 1; i++)
                // {
                //     // 3D stuff
                //     var transform = SKMatrix44.CreateIdentity();
                //
                //     // Translation to cylinder center
                //     transform.PostConcat(SKMatrix44.CreateTranslate(0, 0, r));
                //
                //     // Rotate
                //     transform.PostConcat(SKMatrix44.CreateRotationDegrees(0, 1, 0, i * rotationStepDegrees));
                //
                //     // Translate back into XY plane
                //     transform.PostConcat(SKMatrix44.CreateTranslate(0, 0, -r));
                //
                //     // Scale
                //     transform.PostConcat(SKMatrix44.CreateScale(scale, scale, 1));
                //
                //     // Translate to curl point
                //     transform.PostConcat(SKMatrix44.CreateTranslate(canvasHalfWidth - (pageCenterX * scale) + (D * scale) - (sliceWidth * scale / 2.0f), canvasHalfHeight, 0.0f));
                //
                //     // Perspective?
                //     // var perspectiveMatrix = SKMatrix44.CreateIdentity();
                //     // perspectiveMatrix[3, 2] = -1 / 1500.0f;
                //     // m44rot.PostConcat(perspectiveMatrix);
                //
                //     canvas.Save();
                //
                //     // Apply transform
                //     canvas.SetMatrix(canvas.TotalMatrix.PostConcat(transform.Matrix));
                //     _currentPaint = (i * rotationStepDegrees) <= 90.0f ? _topSidePagePaint : _backSidePagePaint;
                //     // Draw
                //
                //     canvas.DrawBitmap(PageBitmap,
                //         source: SKRect.Create(D - (sliceWidth / 2.0f) + (sliceWidth * (i - 0.5f)), 0, sliceWidth, PageBitmap.Height),
                //         dest: SKRect.Create(
                //             -(sliceWidth / 2.0f),
                //             -(PageBitmap.Height / 2.0f),
                //             sliceWidth * 1.01f, // 1% wider to cover up the gaps in between the slices
                //             PageBitmap.Height),
                //         paint: _currentPaint);
                //
                //     canvas.Restore();
                // }

                // c
                // canvas.Save();
                //
                // var restTransform = SKMatrix44.CreateIdentity();
                // // restTransform.PostConcat(SKMatrix44.CreateRotationDegrees(0,1,0,180.0f));
                // restTransform.PostConcat(SKMatrix44.CreateScale(scale, scale, 1));
                // restTransform.PostConcat(SKMatrix44.CreateTranslate(canvasHalfWidth - (pageCenterX * scale) + (D * scale) - (sliceWidth * scale / 2.0f), canvasHalfHeight, 0.0f));
                //
                // canvas.SetMatrix(canvas.TotalMatrix.PostConcat(restTransform.Matrix));
                //
                // var restImageWidth = Math.Max(0.0f, PageBitmap.Width - D - (cylinderCircumference / 2.0f) + (sliceWidth / 2.0f));
                // var mirrored = GetMirrored(PageBitmap);
                // var restImage = GetRegion(mirrored, SKRectI.Create(0, 0, (int) restImageWidth, mirrored.Height));
                //
                // canvas.DrawBitmap(restImage,
                //     SKRect.Create(-restImageWidth, -restImage.Height / 2.0f, restImageWidth, restImage.Height),
                //     _backSidePagePaint);
                //
                // // Create linear gradient from left to right
                // // _pagePaint.Shader = SKShader.CreateLinearGradient(
                // //     new SKPoint(-restImageWidth, 0.0f),
                // //     new SKPoint(r, 0.0f),
                // //     new SKColor[] {SKColors.Transparent, SKColors.Black},
                // //     new[] {0.3f, 1.0f},
                // //     SKShaderTileMode.Repeat);
                // // canvas.DrawRect(SKRect.Create(-restImageWidth, -restImage.Height / 2.0f, restImageWidth + r, restImage.Height), _pagePaint);
                //
                // canvas.Restore();
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

        private float AngleToOxDegrees(SKPoint tail, SKPoint head)
        {
            // move tail to origin
            var point = head - tail;

            // mirror w.r.t OY axis

            var angleToOyRadians = (float) Math.Atan2(point.Y, point.X);
            var angleToOyDegrees = angleToOyRadians * (180.0f / PI);

            return angleToOyDegrees;
        }

        private SKBitmap GetBottomRegion(SKBitmap original, SKPoint S, float ah, float theta)
        {
            var flippedBitmap = new SKBitmap(original.Width, original.Height);

            using (var canvas = new SKCanvas(flippedBitmap))
            {
                canvas.Clear();
                using (var clipPath = new SKPath())
                {
                    var pivotPoint = new SKPoint(original.Width - ah, S.Y);
                    clipPath.AddRect(SKRect.Create(pivotPoint.X, -original.Height / 2.0f, original.Width * 2.0f, original.Height * 2.0f));
                    clipPath.Transform(SKMatrix.CreateRotationDegrees(-theta, pivotPoint.X, pivotPoint.Y));

                    // clipPath.AddRect(SKRect.Create(200.0f, 200.0f));
                    // clipPath.Transform(SKMatrix.CreateRotationDegrees(30.0f, 100.0f, 100.0f));

                    canvas.ClipPath(clipPath, SKClipOperation.Difference);
                    canvas.DrawBitmap(original, 0.0f, 0.0f);
                }
            }

            return flippedBitmap;
        }
    }
}