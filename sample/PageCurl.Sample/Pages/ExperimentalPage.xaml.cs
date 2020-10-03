using System;
using SkiaSharp;
using SkiaSharp.Views.Forms;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace PageCurl.Sample.Pages
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ExperimentalPage : ContentPage
    {
        public ExperimentalPage()
        {
            InitializeComponent();

            var canvasView = new SKCanvasView();
            canvasView.PaintSurface += CanvasViewOnPaintSurface;
            Content = canvasView;
        }

        private void CanvasViewOnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            var info = e.Info;
            var surface = e.Surface;
            var canvas = surface.Canvas;


            var r = 300.0f;
            var side = 100.0f;
            var sliceCount = (float) Math.PI * r / side;
            var stepSizeDegrees = 180.0f / sliceCount;

            for (var i = 1; i < sliceCount; i++)
            {
                var transform = new SKMatrix44();
                transform.SetIdentity();
                
                transform.PostConcat(SKMatrix44.CreateTranslate(0.0f, 0.0f, r));
                transform.PostConcat(SKMatrix44.CreateRotationDegrees(0, 1, 0, i * stepSizeDegrees));
                transform.PostConcat(SKMatrix44.CreateTranslate(0f, 0.0f, -r));
                transform.PostConcat(SKMatrix44.CreateScale(1.1f, 1.1f, 1.1f));
                transform.PostConcat(SKMatrix44.CreateTranslate(info.Width / 2.0f, info.Height / 2.0f, 0.0f));

                canvas.Save();
                canvas.SetMatrix(canvas.TotalMatrix.PostConcat(transform.Matrix));

                var x = -side / 2.0f;
                var y = -side / 2.0f;
                var innerPaint = new SKPaint
                {
                    Color = SKColors.Gold,
                    Style = SKPaintStyle.Fill
                };
                var outerPaint = new SKPaint
                {
                    Color = SKColors.Red,
                    StrokeWidth = 5,
                    Style = SKPaintStyle.Stroke
                };
                canvas.DrawRect(SKRect.Create(x, y, side, side), innerPaint);
                canvas.DrawRect(SKRect.Create(x, y, side, side), outerPaint);

                canvas.Restore();
            }
        }
    }
}