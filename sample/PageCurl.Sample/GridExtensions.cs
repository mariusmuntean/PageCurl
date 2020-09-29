using System;
using System.Globalization;
using System.IO;
using PageCurl.Sample.media;
using Xamarin.Forms;

namespace PageCurl.Sample
{
    public static class GridExtensions
    {
        public static void AddImages(this StackLayout grid, MyFile[] imageNames, Action<object, SelectionChangedEventArgs> selectionAction)
        {
            var collectionView = new CollectionView
            {
                ItemsSource = imageNames,
                ItemsLayout = LinearItemsLayout.Horizontal,
                SelectionMode = SelectionMode.Single,
                VerticalOptions = LayoutOptions.Start,
                HorizontalOptions = LayoutOptions.CenterAndExpand,
                Margin = new Thickness(20.0d),
                HeightRequest = 100.0d
            };
            collectionView.SelectionChanged += (sender, args) => selectionAction?.Invoke(sender, args);
            collectionView.ItemTemplate = new DataTemplate(() =>
                {
                    var image = new Image
                    {
                        WidthRequest = 100.0d,
                        HeightRequest = 100.0d,
                        Aspect = Aspect.AspectFill
                    };
                    image.SetBinding(Image.SourceProperty, new Binding("Name", BindingMode.Default, new ByteArrayToImageSourceConverter()));

                    return image;
                }
            );

            grid.Children.Add(collectionView);
        }
    }

    public class MyFile
    {
        public string Name { get; set; }
    }

    public class ByteArrayToImageSourceConverter : IValueConverter
    {
        private readonly MediaTool _mediaTool;

        public ByteArrayToImageSourceConverter()
        {
            _mediaTool = new MediaTool();
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            using var stream = _mediaTool.GetMedia(value as string);
            using var ms = new MemoryStream();
            stream.CopyToAsync(ms);
            var bytes = ms.ToArray();

            return ImageSource.FromStream(() => new MemoryStream(bytes));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}