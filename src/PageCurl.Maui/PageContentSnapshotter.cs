using Microsoft.Maui.Controls;
using SkiaSharp;

namespace PageCurl.Maui;

/// <summary>The neutral target reports that native view capture is unavailable.</summary>
public sealed class PageContentSnapshotter : IPageContentSnapshotter
{
    public Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromException<SKImage>(new PlatformNotSupportedException(
            "MAUI content capture requires a native target."));
    }
}
