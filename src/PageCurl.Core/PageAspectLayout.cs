using System.Numerics;

namespace PageCurl.Core;

public enum PageHorizontalPlacement
{
    Center,
    Left,
    Right,
}

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

    public static PageViewportRect PlaceHorizontal(
        PageViewportRect page,
        float viewportWidth,
        PageHorizontalPlacement placement)
    {
        if (!page.IsValid || !float.IsFinite(viewportWidth) || viewportWidth < page.Size.X)
        {
            return default;
        }

        var x = placement switch
        {
            PageHorizontalPlacement.Center => (viewportWidth - page.Size.X) * 0.5f,
            PageHorizontalPlacement.Left => 0,
            PageHorizontalPlacement.Right => viewportWidth - page.Size.X,
            _ => throw new ArgumentOutOfRangeException(nameof(placement)),
        };
        return page with { Origin = new Vector2(x, page.Origin.Y) };
    }

    private static bool IsValidSize(Vector2 size) =>
        float.IsFinite(size.X)
        && float.IsFinite(size.Y)
        && size.X > 0
        && size.Y > 0;
}
