using System.Numerics;
using PageCurl.Core;

namespace PageCurl.Core.Tests;

internal static class PageViewportTests
{
    public static void RunAll()
    {
        Aspect_fit_centers_a_portrait_page_without_distorting_it();
        Aspect_fit_uses_the_full_viewport_when_ratios_match();
        Invalid_sizes_return_an_empty_viewport();
    }

    private static void Aspect_fit_centers_a_portrait_page_without_distorting_it()
    {
        var result = PageViewportRect.AspectFit(new Vector2(400, 300), new Vector2(900, 1200));

        TestAssert.Equal(new Vector2(87.5f, 0), result.Origin, 3);
        TestAssert.Equal(new Vector2(225, 300), result.Size, 3);
    }

    private static void Aspect_fit_uses_the_full_viewport_when_ratios_match()
    {
        var result = PageViewportRect.AspectFit(new Vector2(400, 300), new Vector2(800, 600));

        TestAssert.Equal(Vector2.Zero, result.Origin, 3);
        TestAssert.Equal(new Vector2(400, 300), result.Size, 3);
    }

    private static void Invalid_sizes_return_an_empty_viewport()
    {
        var result = PageViewportRect.AspectFit(new Vector2(400, 300), Vector2.Zero);

        TestAssert.Equal(Vector2.Zero, result.Origin);
        TestAssert.Equal(Vector2.Zero, result.Size);
        TestAssert.False(result.IsValid);
    }
}
