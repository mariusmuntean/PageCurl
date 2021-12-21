using System;
using PageCurl.Sample.media;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PageCurl.Sample.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ExperimentalPage : ContentPage
    {
        private readonly MediaTool _mediaTool = new MediaTool();

        readonly SKBitmap _origBitmap;
        private double _d = 0.0d;

        Slider xSlider = new Slider(0, 180, 0);
        Slider ySlider = new Slider(0, 180, 0);
        Slider zSlider = new Slider(0, 180, 0);

        public ExperimentalPage()
        {
            InitializeComponent();

            var canvasView = new SKCanvasView()
            {
                Margin = new Thickness(15.0d),
                VerticalOptions = LayoutOptions.FillAndExpand
            };
            canvasView.PaintSurface += CanvasViewOnPaintSurface;
            xSlider = new Slider(0, 180, 0);
            ySlider = new Slider(0, 180, 0);
            zSlider = new Slider(0, 180, 0);

            var stackLayout = new StackLayout()
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
                Margin = new Thickness(5)
            };
            stackLayout.Children.Add(canvasView);
            stackLayout.Children.Add(xSlider);
            stackLayout.Children.Add(ySlider);
            stackLayout.Children.Add(zSlider);
            Content = stackLayout;

            _origBitmap = SKBitmap.Decode(_mediaTool.GetMedia("page3.jpeg"));

            Animation a = new Animation(d =>
            {
                _d = d;
                canvasView.InvalidateSurface();
            }, easing: Easing.Linear);
            a.Commit(this, "LinearDoubleAnimation", length: 20000, repeat: () => true);
        }

        private void CanvasViewOnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var info = e.Info;
            var surface = e.Surface;
            var canvas = surface.Canvas;

            canvas.Clear();

            var scale = Math.Min((float)info.Width / _origBitmap.Width, (float)info.Height / _origBitmap.Height);

            // Transform2D(scale, canvas, origBitmap);

            // Transform3D(canvas, _origBitmap, scale);

            Cylinder3D(canvas, _origBitmap, scale);
        }

        private void Cylinder3D(SKCanvas canvas, SKBitmap origBitmap, float scale)
        {
            SKBitmap GetCurrentSliceOld(SKBitmap parentBitmap, int sliceIndex, int sliceCount)
            {
                var sliceWidth = parentBitmap.Width / sliceCount;
                var sliceBitmap = new SKBitmap(sliceWidth, origBitmap.Height, parentBitmap.ColorType, parentBitmap.AlphaType);
                var result = parentBitmap.ExtractSubset(sliceBitmap, SKRectI.Create(sliceIndex * sliceWidth, 0, sliceWidth, parentBitmap.Height));

                if (!result)
                {
                    Console.WriteLine($"Extracting slice result failed");
                }

                return sliceBitmap;
            }
            SKColor NewRandomColor(int sliceIndex)
            {
                var rand = new Random(sliceIndex * 100);
                return new SKColor((byte)rand.Next(255), (byte)rand.Next(255), (byte)rand.Next(255));
            }

            SKBitmap GetCurrentSlice(SKBitmap parentBitmap, int sliceIndex, int sliceCount)
            {
                var sliceWidth = parentBitmap.Width / sliceCount;
                var sliceBitmap = new SKBitmap(sliceWidth, origBitmap.Height, parentBitmap.ColorType, parentBitmap.AlphaType);
                var result = parentBitmap.ExtractSubset(sliceBitmap, SKRectI.Create(sliceIndex * sliceWidth, 0, sliceWidth, parentBitmap.Height));
                sliceBitmap.Erase(NewRandomColor(sliceIndex));
                return sliceBitmap;
            }

            var sliceCount = 20;
            var sliceWidth = origBitmap.Width / (float)sliceCount;
            var sliceAngularOffset = 360.0f / sliceCount;
            var radius = origBitmap.Width / (float)(2 * Math.PI);

            canvas.Save();

            var mat = SKMatrix44.CreateIdentity();
            var scaleMat = SKMatrix44.CreateScale(scale, scale, scale);
            mat.PreConcat(scaleMat);
            var translateToSliceLocationMat = SKMatrix44.CreateTranslate(origBitmap.Width / 2.0f, origBitmap.Height / 2.0f, 0);
            mat.PreConcat(translateToSliceLocationMat);

            // Tilt forward
            var translateToTiltLocationMat = SKMatrix44.CreateTranslate(0, 0, radius);
            mat.PreConcat(translateToTiltLocationMat);
            var tiltMatX = SKMatrix44.CreateRotationDegrees(1.0f, 0.0f, 0.0f, (float)xSlider.Value);
            var tiltMatY = SKMatrix44.CreateRotationDegrees(0.0f, 1.0f, 0.0f, (float)ySlider.Value);
            var tiltMatZ = SKMatrix44.CreateRotationDegrees(0.0f, 0.0f, 1.0f, (float)zSlider.Value);
            mat.PreConcat(tiltMatX);
            mat.PreConcat(tiltMatY);
            mat.PreConcat(tiltMatZ);
            var translateFromTiltLocationMat = SKMatrix44.CreateTranslate(0, 0, -radius);
            mat.PreConcat(translateFromTiltLocationMat);
            canvas.SetMatrix(mat.Matrix);

            // Rotate to animate
            var translateAwayFromDrawLocationMat = SKMatrix44.CreateTranslate(0, 0, radius);
            mat.PreConcat(translateAwayFromDrawLocationMat);
            var rotationMat = SKMatrix44.CreateRotationDegrees(0.0f, 1.0f, 0.0f, (float)(_d * 360.0f));
            mat.PreConcat(rotationMat);
            var translateToDrawLocationMat = SKMatrix44.CreateTranslate(0, 0, -radius);
            mat.PreConcat(translateToDrawLocationMat);
            canvas.SetMatrix(mat.Matrix);

            for (var sliceIndex = 0; sliceIndex < sliceCount; sliceIndex++)
            {
                var currentSlice = GetCurrentSlice(origBitmap, sliceIndex, sliceCount);

                canvas.SetMatrix(mat.Matrix);
                canvas.DrawBitmap(currentSlice, -sliceWidth / 2.0f, 0);

                var translateAwayFromDrawLocationMat2 = SKMatrix44.CreateTranslate(0, 0, radius);
                mat.PreConcat(translateAwayFromDrawLocationMat2);
                var rotationMat2 = SKMatrix44.CreateRotationDegrees(0.0f, 1.0f, 0.0f, -(sliceAngularOffset));
                mat.PreConcat(rotationMat2);
                var translateToDrawLocationMat2 = SKMatrix44.CreateTranslate(0, 0, -radius);
                mat.PreConcat(translateToDrawLocationMat2);
                canvas.SetMatrix(mat.Matrix);
            }

            var translateBackMat = SKMatrix44.CreateTranslate(-origBitmap.Width / 2.0f, -origBitmap.Height / 2.0f, 0);
            mat.PreConcat(translateBackMat);
            canvas.SetMatrix(mat.Matrix);

            canvas.Restore();
        }

        private void Transform3D(SKCanvas canvas, SKBitmap origBitmap, float scale)
        {
            canvas.Save();

            var scaleMat = SKMatrix44.CreateScale(scale, scale, scale);
            var translateForwardMat = SKMatrix44.CreateTranslate(origBitmap.Width / 2.0f, origBitmap.Height / 2.0f, 0);
            var rotateSomeMore = SKMatrix44.CreateRotationDegrees(0, 0, 1, 45);
            var rotationMat = SKMatrix44.CreateRotationDegrees(0.0f, 1.0f, 0.0f, (float)(360.0 * _d));
            var translateBackMat = SKMatrix44.CreateTranslate(-origBitmap.Width / 2.0f, -origBitmap.Height / 2.0f, 0);

            var mat = SKMatrix44.CreateIdentity();
            mat.PreConcat(scaleMat);
            mat.PreConcat(translateForwardMat);
            mat.PreConcat(rotateSomeMore);
            mat.PreConcat(rotationMat);
            mat.PreConcat(translateBackMat);

            canvas.SetMatrix(mat.Matrix);
            canvas.DrawBitmap(origBitmap, 0, 0);
            canvas.Restore();
        }

        private static void Transform2D(float scale, SKCanvas canvas, SKBitmap origBitmap)
        {
            canvas.Save();

            var mat = SKMatrix.Identity;
            var translateMat = SKMatrix.CreateTranslation(200, 400);
            var rotationMat = SKMatrix.CreateRotationDegrees(45);
            var scaleMat = SKMatrix.CreateScale(scale, scale);

            mat = mat.PreConcat(translateMat);
            mat = mat.PreConcat(rotationMat);
            mat = mat.PreConcat(scaleMat);

            canvas.SetMatrix(mat);
            canvas.DrawBitmap(origBitmap, 0, 0);
            canvas.Restore();
        }
    }
}