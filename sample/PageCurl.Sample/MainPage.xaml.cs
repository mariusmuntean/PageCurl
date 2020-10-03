using PageCurl.Sample.media;
using Xamarin.Forms;
using SkiaSharp;
using System.IO;
using System.Linq;

namespace PageCurl.Sample
{
    public partial class MainPage : ContentPage
    {
        private readonly MediaTool _mediaTool;

        private readonly PageCurlView _sKCanvasView;
        private readonly Slider _slider;
        
        public MainPage()
        {
            InitializeComponent();

            _mediaTool = new MediaTool();

            _sKCanvasView = new PageCurlView
            {
                VerticalOptions = LayoutOptions.FillAndExpand
            };

            var stackLayout = new StackLayout
            {
                VerticalOptions = LayoutOptions.FillAndExpand,
            };
            stackLayout.AddImages(new[] {new MyFile {Name = "simple_page.png"}, new MyFile {Name = "dark_page.png"}, new MyFile {Name = "page.jpg"}, new MyFile {Name = "page2.png"}, new MyFile {Name = "page3.jpeg"}},
                (o, args) =>
                {
                    var myFile = args.CurrentSelection.FirstOrDefault() as MyFile;
                    ReloadImage(myFile.Name);
                });
            stackLayout.Children.Add(_sKCanvasView);
            _slider = new Slider
            {
                HeightRequest = 30.0d,
                Margin = new Thickness(10.0d)
            };
            _slider.ValueChanged += (sender, e) =>
            {
                _sKCanvasView.D = (float) e.NewValue;
                _sKCanvasView.InvalidateSurface();
            };
            stackLayout.Children.Add(_slider);
            Content = stackLayout;
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
            _sKCanvasView.PageBitmap = SKBitmap.Decode(imageStream ?? Stream.Null);
            _slider.Maximum = _sKCanvasView.PageBitmap.Width;
            _slider.Value = _sKCanvasView.PageBitmap.Width / 2.0f;

            _sKCanvasView.InvalidateSurface();
        }
    }
}