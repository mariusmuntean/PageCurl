using Microsoft.Maui.Controls;
using SkiaSharp;

namespace PageCurl.Maui;

/// <summary>Captures a laid-out MAUI view as a caller-owned Skia image.</summary>
public interface IPageContentSnapshotter
{
    Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken);
}
