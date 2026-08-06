using System.Numerics;

namespace PageCurl.Core;

/// <summary>Resolves a page ratio and fits it into a viewport.</summary>
public static class PageAspectLayout
{
    public static PageViewportRect Fit(
        Vector2 viewport,
        float pageAspectRatio,
        Vector2 intrinsicSize,
        float fallbackAspectRatio = 0.75f)
    {
        if (!float.IsFinite(pageAspectRatio) || pageAspectRatio < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pageAspectRatio));
        }

        if (!float.IsFinite(fallbackAspectRatio) || fallbackAspectRatio <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fallbackAspectRatio));
        }

        var intrinsicRatio = IsValidSize(intrinsicSize)
            ? intrinsicSize.X / intrinsicSize.Y
            : 0;
        var ratio = pageAspectRatio > 0
            ? pageAspectRatio
            : intrinsicRatio > 0 && float.IsFinite(intrinsicRatio)
                ? intrinsicRatio
                : fallbackAspectRatio;

        return PageViewportRect.AspectFit(viewport, new Vector2(ratio, 1));
    }

    private static bool IsValidSize(Vector2 size) =>
        float.IsFinite(size.X)
        && float.IsFinite(size.Y)
        && size.X > 0
        && size.Y > 0;
}
