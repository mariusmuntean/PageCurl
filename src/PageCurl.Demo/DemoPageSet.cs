using SkiaSharp;

namespace PageCurl.Demo;

public sealed class DemoPageSet : IDisposable
{
    private readonly IReadOnlyList<byte[]> _encodedPages;
    private bool _disposed;

    private DemoPageSet(IReadOnlyList<SKImage> pages, IReadOnlyList<byte[]> encodedPages)
    {
        Pages = pages;
        _encodedPages = encodedPages;
    }

    public IReadOnlyList<SKImage> Pages { get; }

    public static DemoPageSet Create()
    {
        var definitions = new PageDefinition[]
        {
            new("A page should feel certain.", "THE MATERIAL", "It follows the hand, keeps its length, and returns without a wrinkle.", 1, 900, 1200, "01"),
            new("Turn from anywhere.", "EIGHT ANCHORS", "Lock an edge or pin a corner. The same completion path handles touch, mouse, and commands.", 2, 900, 1200, "02"),
            new("One book. Two directions.", "NAVIGATION", "A page reveals what is underneath while it crosses the gutter, then commits once.", 3, 900, 1200, "03"),
            new("Content stays content.", "MAUI VIEWS", "Keep controls and media live while flat. Snapshot only for the physical turn.", 4, 900, 1200, "04"),
            new("The back is yours.", "PAGE BACKSIDE", "Mirror the front, choose a paper color, or render another view entirely.", 5, 900, 1200, "05"),
            new("Fast hands welcome.", "FLICK + THRESHOLD", "Set a clear travel threshold, then let a deliberate flick finish the gesture sooner.", 6, 900, 1200, "06"),
            new("Pixels under pressure.", "HIGH RESOLUTION", "A larger texture checks the seam, shadow, and page edge without changing the API.", 7, 1800, 2400, "07"),
            new("Build something tactile.", "PAGECURL.MAUI", "The geometry is tested. The surface is yours.", 8, 900, 1200, "08"),
        };

        var pages = new List<SKImage>(definitions.Length);
        var encoded = new List<byte[]>(definitions.Length);
        try
        {
            foreach (var definition in definitions)
            {
                var page = CreatePage(definition);
                pages.Add(page);
                using var data = page.Encode(SKEncodedImageFormat.Png, 92);
                encoded.Add(data.ToArray());
            }

            return new DemoPageSet(pages, encoded);
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

    public ImageSource GetImageSource(int pageIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var bytes = _encodedPages[pageIndex];
        return ImageSource.FromStream(() => new MemoryStream(bytes, writable: false));
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
        var scale = definition.Width / 900f;
        var paper = (definition.Number % 3) switch
        {
            1 => new SKColor(251, 248, 239),
            2 => new SKColor(244, 240, 230),
            _ => new SKColor(248, 244, 234),
        };
        var ink = new SKColor(28, 33, 31);
        var muted = new SKColor(103, 108, 104);
        var rule = new SKColor(200, 195, 182);
        var cobalt = new SKColor(49, 87, 213);
        canvas.Clear(paper);

        using var cobaltPaint = new SKPaint { Color = cobalt, IsAntialias = true };
        using var inkPaint = new SKPaint { Color = ink, IsAntialias = true };
        using var mutedPaint = new SKPaint { Color = muted, IsAntialias = true };
        using var rulePaint = new SKPaint
        {
            Color = rule,
            IsAntialias = true,
            StrokeWidth = 2 * scale,
        };
        using var eyebrowFont = new SKFont(SKTypeface.Default, 24 * scale);
        using var titleFont = new SKFont(SKTypeface.Default, 68 * scale);
        using var bodyFont = new SKFont(SKTypeface.Default, 31 * scale);
        using var numberFont = new SKFont(SKTypeface.Default, 28 * scale);

        var left = 84 * scale;
        var right = definition.Width - (84 * scale);
        canvas.DrawRect(left, 72 * scale, 54 * scale, 8 * scale, cobaltPaint);
        canvas.DrawText(definition.Eyebrow, left, 130 * scale, SKTextAlign.Left, eyebrowFont, cobaltPaint);
        DrawWrappedText(canvas, definition.Title, left, 255 * scale, right - left, 78 * scale, titleFont, inkPaint, 3);
        canvas.DrawLine(left, 515 * scale, right, 515 * scale, rulePaint);
        DrawWrappedText(canvas, definition.Body, left, 585 * scale, right - left, 48 * scale, bodyFont, mutedPaint, 5);

        var markerY = 900 * scale;
        for (var i = 0; i < 3; i++)
        {
            var width = (i == definition.Number % 3 ? 285 : 170) * scale;
            canvas.DrawRoundRect(
                new SKRect(left, markerY + (i * 42 * scale), left + width, markerY + (i * 42 * scale) + (7 * scale)),
                4 * scale,
                4 * scale,
                i == 0 ? cobaltPaint : rulePaint);
        }

        canvas.DrawText("PAGECURL.MAUI", left, definition.Height - (72 * scale), SKTextAlign.Left, eyebrowFont, mutedPaint);
        canvas.DrawText(definition.Folio, right, definition.Height - (72 * scale), SKTextAlign.Right, numberFont, inkPaint);
        canvas.DrawCircle(right - (24 * scale), 72 * scale, 7 * scale, cobaltPaint);
        canvas.DrawLine(right - (72 * scale), 72 * scale, right, 144 * scale, cobaltPaint);

        return SKImage.FromBitmap(bitmap);
    }

    private static void DrawWrappedText(
        SKCanvas canvas,
        string text,
        float x,
        float y,
        float width,
        float lineHeight,
        SKFont font,
        SKPaint paint,
        int maxLines)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var line = string.Empty;
        var lineIndex = 0;
        foreach (var word in words)
        {
            var candidate = line.Length == 0 ? word : $"{line} {word}";
            if (font.MeasureText(candidate) <= width || line.Length == 0)
            {
                line = candidate;
                continue;
            }

            canvas.DrawText(line, x, y + (lineIndex * lineHeight), SKTextAlign.Left, font, paint);
            lineIndex++;
            if (lineIndex >= maxLines)
            {
                return;
            }

            line = word;
        }

        if (lineIndex < maxLines && line.Length > 0)
        {
            canvas.DrawText(line, x, y + (lineIndex * lineHeight), SKTextAlign.Left, font, paint);
        }
    }

    private readonly record struct PageDefinition(
        string Title,
        string Eyebrow,
        string Body,
        int Number,
        int Width,
        int Height,
        string Folio);
}
