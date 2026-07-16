using SkiaSharp;

namespace PageCurl.Demo;

public sealed class DemoPageSet : IDisposable
{
    private bool _disposed;

    private DemoPageSet(IReadOnlyList<SKImage> pages)
    {
        Pages = pages;
    }

    public IReadOnlyList<SKImage> Pages { get; }

    public static DemoPageSet Create()
    {
        var definitions = new PageDefinition[]
        {
            new("Page 1", 900, 1200, new SKColor(255, 244, 214), new SKColor(213, 137, 48)),
            new("Page 2", 900, 1200, new SKColor(220, 241, 255), new SKColor(48, 123, 183)),
            new("Page 3", 900, 1200, new SKColor(229, 246, 227), new SKColor(62, 146, 81)),
            new("Large page", 1800, 2400, new SKColor(244, 229, 255), new SKColor(125, 76, 170)),
        };

        var pages = new List<SKImage>(definitions.Length);
        try
        {
            foreach (var definition in definitions)
            {
                pages.Add(CreatePage(definition));
            }

            return new DemoPageSet(pages);
        }
        catch
        {
            foreach (var page in pages)
            {
                page.Dispose();
            }

            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var page in Pages)
        {
            page.Dispose();
        }
    }

    private static SKImage CreatePage(PageDefinition definition)
    {
        using var bitmap = new SKBitmap(definition.Width, definition.Height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(definition.Background);

        using var border = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = definition.Width >= 1600 ? 36 : 24,
            Color = definition.Accent,
            IsAntialias = true,
        };
        var inset = definition.Width >= 1600 ? 84 : 42;
        canvas.DrawRoundRect(
            new SKRect(inset, inset, definition.Width - inset, definition.Height - inset),
            28,
            28,
            border);

        using var titleFont = new SKFont(SKTypeface.Default, definition.Width >= 1600 ? 132 : 96);
        using var titlePaint = new SKPaint
        {
            Color = definition.Accent,
            IsAntialias = true,
        };
        canvas.DrawText(
            definition.Title,
            definition.Width >= 1600 ? 180 : 90,
            definition.Width >= 1600 ? 290 : 210,
            SKTextAlign.Left,
            titleFont,
            titlePaint);

        using var bodyPaint = new SKPaint
        {
            Color = new SKColor(45, 45, 45),
            IsAntialias = true,
        };
        var left = definition.Width >= 1600 ? 184 : 92;
        var right = definition.Width >= 1600 ? definition.Width - 200 : definition.Width - 100;
        var firstLine = definition.Width >= 1600 ? 500 : 360;
        var spacing = definition.Width >= 1600 ? 72 : 54;
        var lineCount = definition.Width >= 1600 ? 18 : 12;
        for (var line = 0; line < lineCount; line++)
        {
            var y = firstLine + (line * spacing);
            canvas.DrawLine(left, y, right, y, bodyPaint);
        }

        return SKImage.FromBitmap(bitmap);
    }

    private readonly record struct PageDefinition(
        string Title,
        int Width,
        int Height,
        SKColor Background,
        SKColor Accent);
}
