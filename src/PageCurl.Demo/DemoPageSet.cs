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
            new("The first fold", "MOTION STUDY", 1, 900, 1200, new SKColor(255, 248, 237), new SKColor(118, 88, 216)),
            new("A quiet radius", "CURL GEOMETRY", 2, 900, 1200, new SKColor(233, 242, 255), new SKColor(48, 123, 183)),
            new("Light on paper", "SURFACE + SHADOW", 3, 900, 1200, new SKColor(235, 247, 235), new SKColor(62, 146, 81)),
            new("Open at the edge", "LARGE PAGE", 4, 1800, 2400, new SKColor(244, 233, 255), new SKColor(125, 76, 170)),
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
        using var kickerFont = new SKFont(SKTypeface.Default, definition.Width >= 1600 ? 42 : 30);
        using var kickerPaint = new SKPaint
        {
            Color = definition.Accent,
            IsAntialias = true,
        };
        canvas.DrawText(
            definition.Kicker,
            definition.Width >= 1600 ? 184 : 92,
            definition.Width >= 1600 ? 220 : 158,
            SKTextAlign.Left,
            kickerFont,
            kickerPaint);
        canvas.DrawText(
            definition.Title,
            definition.Width >= 1600 ? 180 : 90,
            definition.Width >= 1600 ? 390 : 310,
            SKTextAlign.Left,
            titleFont,
            titlePaint);

        using var bodyPaint = new SKPaint
        {
            Color = new SKColor(38, 32, 53, 76),
            IsAntialias = true,
        };
        var left = definition.Width >= 1600 ? 184 : 92;
        var right = definition.Width >= 1600 ? definition.Width - 200 : definition.Width - 100;
        var firstLine = definition.Width >= 1600 ? 600 : 460;
        var spacing = definition.Width >= 1600 ? 72 : 54;
        var lineCount = definition.Width >= 1600 ? 18 : 12;
        for (var line = 0; line < lineCount; line++)
        {
            var y = firstLine + (line * spacing);
            var lineRight = line % 4 == 3 ? right - (definition.Width >= 1600 ? 260 : 140) : right;
            canvas.DrawLine(left, y, lineRight, y, bodyPaint);
        }

        using var accentPaint = new SKPaint
        {
            Color = definition.Accent,
            IsAntialias = true,
        };
        canvas.DrawRoundRect(
            new SKRect(left, definition.Width >= 1600 ? 500 : 380, left + (definition.Width >= 1600 ? 220 : 150), definition.Width >= 1600 ? 536 : 404),
            18,
            18,
            accentPaint);
        canvas.DrawCircle(inset + (definition.Width >= 1600 ? 62 : 34), inset + (definition.Width >= 1600 ? 62 : 34), definition.Width >= 1600 ? 16 : 10, accentPaint);
        canvas.DrawLine(
            definition.Width - inset - (definition.Width >= 1600 ? 150 : 86),
            definition.Height - inset,
            definition.Width - inset,
            definition.Height - inset - (definition.Width >= 1600 ? 150 : 86),
            accentPaint);

        using var numberFont = new SKFont(SKTypeface.Default, definition.Width >= 1600 ? 48 : 36);
        using var numberPaint = new SKPaint
        {
            Color = new SKColor(38, 32, 53, 150),
            IsAntialias = true,
        };
        canvas.DrawText(
            $"{definition.Number:00} / 04",
            definition.Width - (definition.Width >= 1600 ? 200 : 100),
            definition.Height - (definition.Width >= 1600 ? 150 : 82),
            SKTextAlign.Right,
            numberFont,
            numberPaint);

        return SKImage.FromBitmap(bitmap);
    }

    private readonly record struct PageDefinition(
        string Title,
        string Kicker,
        int Number,
        int Width,
        int Height,
        SKColor Background,
        SKColor Accent);
}
