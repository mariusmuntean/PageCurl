using CoreGraphics;
using Microsoft.Maui.Controls;
using SkiaSharp;
using UIKit;

namespace PageCurl.Maui;

public sealed class PageContentSnapshotter : IPageContentSnapshotter
{
    public Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        cancellationToken.ThrowIfCancellationRequested();
        if (content.Handler?.PlatformView is not UIView nativeView)
        {
            throw new InvalidOperationException("The content must have an attached Apple handler.");
        }

        if (nativeView.Bounds.Width <= 0 || nativeView.Bounds.Height <= 0)
        {
            throw new InvalidOperationException("The content must be laid out before capture.");
        }

        // Copy pixels directly instead of round-tripping through a PNG:
        // capturing the same content repeatedly stays cheap and avoids
        // large transient encode and decode buffers.
        nativeView.LayoutIfNeeded();
        using var renderer = new UIGraphicsImageRenderer(nativeView.Bounds.Size);
        using var nativeImage = renderer.CreateImage(_ =>
            nativeView.DrawViewHierarchy(nativeView.Bounds, true));
        cancellationToken.ThrowIfCancellationRequested();
        var cgImage = nativeImage.CGImage
            ?? throw new InvalidOperationException("Apple could not rasterize the content snapshot.");
        // UIKit can return grayscale, RGBA or extended-color pixels. Draw
        // into explicit sRGB, premultiplied BGRA storage before copying to Skia.
        var width = checked((int)cgImage.Width);
        var height = checked((int)cgImage.Height);
        var rowBytes = checked(width * 4);
        using var colorSpace = CGColorSpace.CreateSrgb();
        using var context = new CGBitmapContext(
            IntPtr.Zero, width, height, 8, rowBytes, colorSpace,
            (CGImageAlphaInfo)(CGBitmapFlags.PremultipliedFirst | CGBitmapFlags.ByteOrder32Little));
        context.DrawImage(new CGRect(0, 0, width, height), cgImage);
        cancellationToken.ThrowIfCancellationRequested();
        using var skColorSpace = SKColorSpace.CreateSrgb();
        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul, skColorSpace);
        var image = SKImage.FromPixelCopy(info, context.Data, rowBytes)
            ?? throw new InvalidOperationException("SkiaSharp could not copy the content snapshot.");
        if (cancellationToken.IsCancellationRequested)
        {
            image.Dispose();
            cancellationToken.ThrowIfCancellationRequested();
        }
        return Task.FromResult(image);
    }
}
