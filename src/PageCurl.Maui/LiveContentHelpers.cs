using System.Numerics;
using Microsoft.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui.Controls;

namespace PageCurl.Maui;

internal static class LiveContentHelpers
{
    /// <summary>True when the content has a native handler and a non-zero arranged size.</summary>
    public static bool IsArranged(View content) =>
        content.Handler is not null && content.Width > 0 && content.Height > 0;

    /// <summary>Scales a touch location from zone canvas pixels to zone-local device-independent units.</summary>
    public static Vector2 ToZoneLocal(SKCanvasView zone, SKPoint location)
    {
        var zoneCanvas = zone.CanvasSize;
        var localX = zoneCanvas.Width > 0
            ? location.X * (float)(Math.Max(zone.Width, 1) / zoneCanvas.Width)
            : location.X;
        var localY = zoneCanvas.Height > 0
            ? location.Y * (float)(Math.Max(zone.Height, 1) / zoneCanvas.Height)
            : location.Y;
        return new Vector2(localX, localY);
    }
}
