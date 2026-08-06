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

        using var renderer = new UIGraphicsImageRenderer(nativeView.Bounds.Size);
        using var nativeImage = renderer.CreateImage(_ =>
            nativeView.DrawViewHierarchy(nativeView.Bounds, true));
        cancellationToken.ThrowIfCancellationRequested();
        using var encoded = nativeImage.AsPNG()
            ?? throw new InvalidOperationException("Apple could not encode the content snapshot.");
        using var stream = encoded.AsStream();
        var image = SKImage.FromEncodedData(stream)
            ?? throw new InvalidOperationException("SkiaSharp could not decode the content snapshot.");
        return Task.FromResult(image);
    }
}
