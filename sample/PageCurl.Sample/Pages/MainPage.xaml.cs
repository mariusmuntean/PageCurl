using System.IO;
using System.Linq;
using PageCurl.Sample.media;
using PageCurl.Sample.Models;
using SkiaSharp;
using Xamarin.Forms;

namespace PageCurl.Sample.Pages
{
    public partial class MainPage : ContentPage
    {
        private readonly MediaTool _mediaTool;

        public MainPage()
        {
            InitializeComponent();

            _mediaTool = new MediaTool();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            ReloadImage(null);
        }

        private void ReloadImage(string imageName)
        {
            var fileName = imageName ?? "simple_page.png";
            using var imageStream = _mediaTool.GetMedia(fileName);
            PageCurlView.PageBitmap = SKBitmap.Decode(imageStream ?? Stream.Null);

            DSlider.Maximum = PageCurlView.PageBitmap.Width;
            DSlider.Value = PageCurlView.PageBitmap.Width / 2.0f;

            GSlider.Maximum = PageCurlView.PageBitmap.Height;
            GSlider.Value = PageCurlView.PageBitmap.Height / 2.0f;

            PageCurlView.InvalidateSurface();
        }

        private void OnDSliderOnValueChanged(object sender, ValueChangedEventArgs e)
        {
            PageCurlView.D = (float) e.NewValue;
            PageCurlView.InvalidateSurface();
        }

        private void OnGSliderOnValueChanged(object sender, ValueChangedEventArgs e)
        {
            PageCurlView.G = (float) e.NewValue;
            PageCurlView.InvalidateSurface();
        }

        private void Images_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var myFile = e.CurrentSelection.FirstOrDefault() as MyFile;
            ReloadImage(myFile.Name);
        }
    }
}