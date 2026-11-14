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

        var info = new SKImageInfo(
            bitmap.Width,
            bitmap.Height,
            SKColorType.Rgba8888,
            bitmap.IsPremultiplied ? SKAlphaType.Premul : SKAlphaType.Unpremul);
        var pixels = bitmap.LockPixels();
        SKImage image;
        try
        {
            // FromPixelCopy copies into Skia-owned memory, so the image outlives the bitmap.
            image = SKImage.FromPixelCopy(info, pixels, bitmap.RowBytes)
                ?? throw new InvalidOperationException("SkiaSharp could not copy the content snapshot.");
        }
        finally
        {
            bitmap.UnlockPixels();
        }

        return Task.FromResult(image);
    }
}
