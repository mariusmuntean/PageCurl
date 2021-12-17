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
        private MediaTool _mediaTool = new MediaTool();

        public ExperimentalPage()
        {
            InitializeComponent();

            var canvasView = new SKCanvasView()
            {
                Margin = new Thickness(15.0d)
            };
            canvasView.PaintSurface += CanvasViewOnPaintSurface;
            Content = canvasView;
        }

        private void CanvasViewOnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var info = e.Info;
            var surface = e.Surface;
            var canvas = surface.Canvas;

            canvas.Clear();

            var origBitmap = SKBitmap.Decode(_mediaTool.GetMedia("page3.jpeg"));

            var scale = Math.Min((float) info.Width / origBitmap.Width, (float) info.Height / origBitmap.Height);

            var transform = SKMatrix44.CreateIdentity();

            transform.PostConcat(SKMatrix44.CreateTranslate(-origBitmap.Width * 0.5f, -origBitmap.Height * 0.5f, 0.0f));
            transform.PostConcat(SKMatrix44.CreateRotationDegrees(1, 1, 0, 45));
            transform.PostConcat(SKMatrix44.CreateTranslate(origBitmap.Width * 0.5f, origBitmap.Height * 0.5f, 0.0f));

            transform.PostConcat(SKMatrix44.CreateTranslate(info.Width * 0.5f, info.Height * 0.5f, 0.0f));

            transform.PostConcat(SKMatrix44.CreateScale(scale, scale, 1.0f));

            canvas.Save();
            canvas.SetMatrix(transform.Matrix);
            canvas.DrawBitmap(origBitmap, -info.Width * 0.5f, -info.Height * 0.5f);
            canvas.Restore();
            
            var newBitmap = SKBitmap.Decode(_mediaTool.GetMedia("page3.jpeg"));
            transform.SetIdentity();
            
            
        }
    }
}