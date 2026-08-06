using System.Numerics;
using PageCurl.Core;

namespace PageCurl.Core.Tests;

internal static class PageAspectLayoutTests
{
    public static void RunAll()
    {
        Explicit_ratio_controls_the_fitted_page_rectangle();
        Zero_ratio_uses_intrinsic_content_dimensions();
        Missing_intrinsic_dimensions_use_the_fallback_ratio();
        Horizontal_placement_aligns_a_fitted_page_for_spread_rendering();
    }

    private static void Explicit_ratio_controls_the_fitted_page_rectangle()
    {
        var result = PageAspectLayout.Fit(
            new Vector2(400, 300),
            pageAspectRatio: 0.5f,
            intrinsicSize: new Vector2(1600, 900));

        TestAssert.Equal(new Vector2(125, 0), result.Origin, 3);
        TestAssert.Equal(new Vector2(150, 300), result.Size, 3);
    }

    private static void Zero_ratio_uses_intrinsic_content_dimensions()
    {
        var result = PageAspectLayout.Fit(
            new Vector2(400, 300),
            pageAspectRatio: 0,
            intrinsicSize: new Vector2(900, 1200));

        TestAssert.Equal(new Vector2(87.5f, 0), result.Origin, 3);
        TestAssert.Equal(new Vector2(225, 300), result.Size, 3);
    }

    private static void Missing_intrinsic_dimensions_use_the_fallback_ratio()
    {
        var result = PageAspectLayout.Fit(
            new Vector2(400, 300),
            pageAspectRatio: 0,
            intrinsicSize: Vector2.Zero,
            fallbackAspectRatio: 0.6f);

        TestAssert.Equal(new Vector2(110, 0), result.Origin, 3);
        TestAssert.Equal(new Vector2(180, 300), result.Size, 3);
    }

    private static void Horizontal_placement_aligns_a_fitted_page_for_spread_rendering()
    {
        var fitted = new PageViewportRect(new Vector2(125, 0), new Vector2(150, 300));

        var left = PageAspectLayout.PlaceHorizontal(
            fitted,
            viewportWidth: 400,
            PageHorizontalPlacement.Left);
        var right = PageAspectLayout.PlaceHorizontal(
            fitted,
            viewportWidth: 400,
            PageHorizontalPlacement.Right);

        TestAssert.Equal(new Vector2(0, 0), left.Origin);
        TestAssert.Equal(new Vector2(250, 0), right.Origin);
        TestAssert.Equal(fitted.Size, left.Size);
        TestAssert.Equal(fitted.Size, right.Size);
    }
}
