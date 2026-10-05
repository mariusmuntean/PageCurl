using Microsoft.Maui.Controls;
using SkiaSharp;

namespace PageCurl.Maui;

/// <summary>Captures a laid-out MAUI view as a caller-owned Skia image.</summary>
public interface IPageContentSnapshotter
{
    /// <summary>Captures on the UI thread and transfers ownership of the returned image.</summary>
    /// <remarks>
    /// Implementations must complete their task and cooperate with cancellation.
    /// Spread captures are serialized; an unfinished capture delays later requests,
    /// including refresh after hiding and showing the control.
    /// </remarks>
    Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken);
}
