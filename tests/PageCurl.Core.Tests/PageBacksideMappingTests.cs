using System.Numerics;
using PageCurl.Core;

namespace PageCurl.Core.Tests;

internal static class PageBacksideMappingTests
{
    public static void RunAll()
    {
        Mirrored_front_flips_horizontal_texture_and_dims_shade();
        Dim_factor_is_clamped_to_a_valid_multiplier();
        Solid_color_does_not_request_a_texture();
        Custom_content_keeps_the_supplied_texture_coordinates();
    }

    private static void Mirrored_front_flips_horizontal_texture_and_dims_shade()
    {
        var result = PageBacksideMapping.Map(
            PageBacksideMode.MirroredFront,
            new Vector2(0.8f, 0.25f),
            shade: 0.75f,
            dimFactor: 0.82f);

        TestAssert.True(
            Vector2.Distance(new Vector2(0.2f, 0.25f), result.TextureCoordinate) <= 0.0001f);
        TestAssert.Equal(0.615f, result.Shade, 3);
        TestAssert.True(result.UsesTexture);
    }

    private static void Dim_factor_is_clamped_to_a_valid_multiplier()
    {
        var dark = PageBacksideMapping.Map(
            PageBacksideMode.MirroredFront,
            Vector2.Zero,
            shade: 0.8f,
            dimFactor: -1);
        var unchanged = PageBacksideMapping.Map(
            PageBacksideMode.MirroredFront,
            Vector2.Zero,
            shade: 0.8f,
            dimFactor: 2);

        TestAssert.Equal(0f, dark.Shade);
        TestAssert.Equal(0.8f, unchanged.Shade);
    }

    private static void Solid_color_does_not_request_a_texture()
    {
        var result = PageBacksideMapping.Map(
            PageBacksideMode.SolidColor,
            new Vector2(0.3f, 0.6f),
            shade: 0.7f,
            dimFactor: 0.2f);

        TestAssert.False(result.UsesTexture);
        TestAssert.Equal(new Vector2(0.3f, 0.6f), result.TextureCoordinate);
        TestAssert.Equal(0.7f, result.Shade);
    }

    private static void Custom_content_keeps_the_supplied_texture_coordinates()
    {
        var result = PageBacksideMapping.Map(
            PageBacksideMode.CustomContent,
            new Vector2(0.3f, 0.6f),
            shade: 0.7f,
            dimFactor: 0.2f);

        TestAssert.True(result.UsesTexture);
        TestAssert.Equal(new Vector2(0.3f, 0.6f), result.TextureCoordinate);
        TestAssert.Equal(0.7f, result.Shade);
    }
}
