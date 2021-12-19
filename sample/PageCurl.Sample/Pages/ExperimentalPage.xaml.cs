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

        public ExperimentalPage()
        {
            InitializeComponent();

            var canvasView = new SKCanvasView()
            {
                Margin = new Thickness(15.0d)
            };
            canvasView.PaintSurface += CanvasViewOnPaintSurface;
            Content = canvasView;

            _origBitmap = SKBitmap.Decode(_mediaTool.GetMedia("page3.jpeg"));

            Animation a = new Animation(d =>
            {
                _d = d;
                canvasView.InvalidateSurface();
            }, easing: Easing.Linear);
            a.Commit(this, "LinearDoubleAnimation", length: 10000, repeat: () => true);
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