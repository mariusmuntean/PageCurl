using System.Numerics;

namespace PageCurl.Core;

/// <summary>Controls how a turned page presents its underside.</summary>
public enum PageBacksideMode
{
    MirroredFront,
    SolidColor,
    CustomContent,
}

/// <summary>The texture and shade decision for one backside mesh vertex.</summary>
public readonly record struct PageBacksideSample(
    Vector2 TextureCoordinate,
    float Shade,
    bool UsesTexture);

/// <summary>Maps platform-neutral backside texture and lighting values.</summary>
public static class PageBacksideMapping
{
    public static PageBacksideSample Map(
        PageBacksideMode mode,
        Vector2 textureCoordinate,
        float shade,
        float dimFactor)
    {
        if (!float.IsFinite(textureCoordinate.X)
            || !float.IsFinite(textureCoordinate.Y)
            || !float.IsFinite(shade))
        {
            return default;
        }

        var clampedShade = Math.Clamp(shade, 0, 1);
        return mode switch
        {
            PageBacksideMode.MirroredFront => new(
                new Vector2(1f - textureCoordinate.X, textureCoordinate.Y),
                clampedShade * Math.Clamp(
                    float.IsFinite(dimFactor) ? dimFactor : 0,
                    0,
                    1),
                true),
            PageBacksideMode.SolidColor => new(
                textureCoordinate,
                clampedShade,
                false),
            PageBacksideMode.CustomContent => new(
                textureCoordinate,
                clampedShade,
                true),
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };
    }
}
