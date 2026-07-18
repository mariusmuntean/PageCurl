using System.Numerics;

namespace PageCurl.Core;

/// <summary>A reusable content rectangle inside a renderer viewport.</summary>
public readonly record struct PageViewportRect(Vector2 Origin, Vector2 Size)
{
    public bool IsValid =>
        float.IsFinite(Origin.X)
        && float.IsFinite(Origin.Y)
        && float.IsFinite(Size.X)
        && float.IsFinite(Size.Y)
        && Size.X > 0
        && Size.Y > 0;

    public Vector2 BottomRight => Origin + Size;

    /// <summary>Centers content in the viewport without changing its aspect ratio.</summary>
    public static PageViewportRect AspectFit(Vector2 viewport, Vector2 content)
    {
        if (!IsValidSize(viewport) || !IsValidSize(content))
        {
            return default;
        }

        var scale = MathF.Min(viewport.X / content.X, viewport.Y / content.Y);
        var size = content * scale;
        return new((viewport - size) * 0.5f, size);
    }

    private static bool IsValidSize(Vector2 value) =>
        float.IsFinite(value.X) && float.IsFinite(value.Y) && value.X > 0 && value.Y > 0;
}
