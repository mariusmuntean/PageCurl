using PageCurl.Core;
using PageCurl.Maui;
using SkiaSharp;

namespace PageCurl.Demo;

public partial class MainPage : ContentPage
{
    private readonly SKImage[] _pages;
    private int _pageIndex;

    public MainPage()
    {
        InitializeComponent();
        _pages =
        [
            CreatePage("Page 1", new SKColor(255, 244, 214), new SKColor(213, 137, 48)),
            CreatePage("Page 2", new SKColor(220, 241, 255), new SKColor(48, 123, 183)),
            CreatePage("Page 3", new SKColor(229, 246, 227), new SKColor(62, 146, 81)),
        ];
        PageView.PageTurnCompleted += OnPageTurnCompleted;
        ShowPage();
    }

    private void OnPageTurnCompleted(object? sender, PageTurnCompletedEventArgs args)
    {
        if (args.Committed)
        {
            _pageIndex = args.Direction == PageTurnDirection.Forward
                ? Math.Min(_pageIndex + 1, _pages.Length - 1)
                : Math.Max(_pageIndex - 1, 0);
            ShowPage();
        }

        StatusLabel.Text = args.Committed ? "Turn committed" : "Turn cancelled";
    }

    private void ShowPage()
    {
        PageView.FrontImage = _pages[_pageIndex];
        PageView.BackImage = _pages[_pageIndex];
        PageView.UnderPageImage = _pages[Math.Clamp(_pageIndex + 1, 0, _pages.Length - 1)];
        StatusLabel.Text = $"Page {_pageIndex + 1} of {_pages.Length} · drag from either edge";
    }

    private static SKImage CreatePage(string title, SKColor background, SKColor accent)
    {
        const int width = 900;
        const int height = 1200;
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(background);

        using var border = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 24,
            Color = accent,
            IsAntialias = true,
        };
        canvas.DrawRoundRect(new SKRect(42, 42, width - 42, height - 42), 28, 28, border);

        using var titlePaint = new SKPaint
        {
            Color = accent,
            TextSize = 96,
            Typeface = SKTypeface.Default,
            IsAntialias = true,
        };
        canvas.DrawText(title, 90, 210, titlePaint);

        using var bodyPaint = new SKPaint
        {
            Color = new SKColor(45, 45, 45),
            TextSize = 34,
            IsAntialias = true,
        };
        for (var line = 0; line < 12; line++)
        {
            canvas.DrawLine(92, 360 + (line * 54), width - 100, 360 + (line * 54), bodyPaint);
        }

        return SKImage.FromBitmap(bitmap);
    }
}
