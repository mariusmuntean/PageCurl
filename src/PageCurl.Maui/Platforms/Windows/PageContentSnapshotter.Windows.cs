using Microsoft.Maui.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using SkiaSharp;
using System.Runtime.InteropServices.WindowsRuntime;

namespace PageCurl.Maui;

public sealed class PageContentSnapshotter : IPageContentSnapshotter
{
    public async Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        cancellationToken.ThrowIfCancellationRequested();
        if (content.Handler?.PlatformView is not UIElement nativeView)
        {
            throw new InvalidOperationException("The content must have an attached Windows handler.");
        }

        var renderTarget = new RenderTargetBitmap();
        await renderTarget.RenderAsync(nativeView);
        cancellationToken.ThrowIfCancellationRequested();
        if (renderTarget.PixelWidth <= 0 || renderTarget.PixelHeight <= 0)
        {
            throw new InvalidOperationException("The content must be laid out before capture.");
        }

        var pixelBuffer = await renderTarget.GetPixelsAsync();
        using var pixelStream = pixelBuffer.AsStream();
        var pixels = new byte[pixelStream.Length];
        await pixelStream.ReadExactlyAsync(pixels, cancellationToken);
        using var bitmap = new SKBitmap(new SKImageInfo(
            renderTarget.PixelWidth,
            renderTarget.PixelHeight,
            SKColorType.Bgra8888,
            SKAlphaType.Premul));
        System.Runtime.InteropServices.Marshal.Copy(
            pixels,
            0,
            bitmap.GetPixels(),
            pixels.Length);
        return SKImage.FromBitmap(bitmap);
    }
}
