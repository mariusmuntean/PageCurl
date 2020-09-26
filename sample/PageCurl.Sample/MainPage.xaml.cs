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
        readonly MediaTool _mediaTool;

        private SKBitmap _pageBitmap;
        readonly SKCanvasView _sKCanvasView;

        public MainPage()
        {
            InitializeComponent();

            _mediaTool = new MediaTool();

            _sKCanvasView = new SKCanvasView();
            _sKCanvasView.PaintSurface += Paint;

            Content = _sKCanvasView;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            using var imageStream = _mediaTool.GetMedia("page.jpg");
            _pageBitmap = SKBitmap.Decode(imageStream ?? Stream.Null);
            _sKCanvasView.InvalidateSurface();
        }

        private void Paint(object sender, SKPaintSurfaceEventArgs e)
        {
            SKImageInfo info = e.Info;
            SKSurface surface = e.Surface;
            SKCanvas canvas = surface.Canvas;

            canvas.Clear();

            canvas.Translate(info.Width / 2.0f, info.Height / 2.0f);

            if (_pageBitmap != null)
            {
                var hScale = info.Width / (float)_pageBitmap.Width;
                var vScale = info.Height/ (float)_pageBitmap.Height;
                canvas.Scale(Math.Min(hScale, vScale));

                var x = _pageBitmap.Width / 2.0f;
                var y = _pageBitmap.Height / 2.0f;

                canvas.DrawBitmap(_pageBitmap, -x, -y);
            }
        }
    }
}
