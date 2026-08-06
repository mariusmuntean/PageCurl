using Android.Graphics;
using Microsoft.Maui.Controls;
using SkiaSharp;

namespace PageCurl.Maui;

public sealed class PageContentSnapshotter : IPageContentSnapshotter
{
    public Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        cancellationToken.ThrowIfCancellationRequested();
        if (content.Handler?.PlatformView is not Android.Views.View nativeView)
        {
            throw new InvalidOperationException("The content must have an attached Android handler.");
        }

        if (nativeView.Width <= 0 || nativeView.Height <= 0)
        {
            throw new InvalidOperationException("The content must be laid out before capture.");
        }

        using var bitmap = Bitmap.CreateBitmap(
            nativeView.Width,
            nativeView.Height,
            Bitmap.Config.Argb8888!);
        using var canvas = new Canvas(bitmap);
        nativeView.Draw(canvas);
        cancellationToken.ThrowIfCancellationRequested();

        using var encoded = new MemoryStream();
        if (!bitmap.Compress(Bitmap.CompressFormat.Png!, 100, encoded))
        {
            throw new InvalidOperationException("Android could not encode the content snapshot.");
        }

        encoded.Position = 0;
        var image = SKImage.FromEncodedData(encoded)
            ?? throw new InvalidOperationException("SkiaSharp could not decode the content snapshot.");
        return Task.FromResult(image);
    }
}
