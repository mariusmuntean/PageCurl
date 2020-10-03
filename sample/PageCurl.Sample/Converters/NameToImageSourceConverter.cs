using System;
using System.Globalization;
using System.IO;
using PageCurl.Sample.media;
using Xamarin.Forms;

namespace PageCurl.Sample.Converters
{
    public class NameToImageSourceConverter : IValueConverter
    {
        private readonly MediaTool _mediaTool;

        public NameToImageSourceConverter()
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