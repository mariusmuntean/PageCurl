using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using PageCurl.Maui;
using SkiaSharp;
using UIKit;

namespace PageCurl.Apple.Tests;

public sealed class FixturePage : ContentPage
{
    private readonly Grid _capture = new() { WidthRequest = 100, HeightRequest = 100 };
    private readonly List<string> _results = new();
    private bool _started;

    public FixturePage()
    {
        Content = _capture;
        Loaded += Run;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Run(this, EventArgs.Empty);
    }

    private async void Run(object? sender, EventArgs args)
    {
        if (_started) return;
        _started = true;
        if (Environment.GetEnvironmentVariable("PAGECURL_PREVIEW_DIR") is { } signals)
        {
            try
            {
                await MotionPreview.RunAsync(Window!, signals);
                Environment.Exit(0);
            }
            catch (Exception exception)
            {
                File.WriteAllText(Path.Combine(signals, "error"), exception.ToString());
                Environment.Exit(1);
            }
            return;
        }
        await Task.Delay(300);
        try
        {
            foreach (var item in new[]
            {
                (Name: "red", Color: Colors.Red, Expected: SKColors.Red),
                (Name: "blue", Color: Colors.Blue, Expected: SKColors.Blue),
                (Name: "white", Color: Colors.White, Expected: SKColors.White),
                (Name: "gray", Color: Colors.Gray, Expected: SKColors.Gray),
                (Name: "half-red", Color: new Color(1f, 0f, 0f, 0.5f), Expected: new SKColor(255, 0, 0, 128)),
            })
            {
                _capture.BackgroundColor = item.Color;
                await Task.Delay(50);
                using var image = await new PageContentSnapshotter().CaptureAsync(_capture, default);
                using var bitmap = SKBitmap.FromImage(image);
                var pixel = bitmap.GetPixel(image.Width / 2, image.Height / 2);
                using var format = UIGraphicsImageRendererFormat.DefaultFormat;
                var native = (UIView)_capture.Handler!.PlatformView!;
                Require(image.Width == (int)Math.Round(native.Bounds.Width * format.Scale)
                    && image.Height == (int)Math.Round(native.Bounds.Height * format.Scale), item.Name + " dimensions");
                // Native window appearance and color conversion can round
                // channels slightly; swapped channels/stride/alpha still fail.
                Require(Math.Abs(pixel.Red - item.Expected.Red) <= 8 && Math.Abs(pixel.Blue - item.Expected.Blue) <= 8
                    && Math.Abs(pixel.Green - item.Expected.Green) <= 8 && Math.Abs(pixel.Alpha - item.Expected.Alpha) <= 2,
                    $"{item.Name}: expected {item.Expected}, got {pixel}");
                _results.Add($"PASS {item.Name} {image.Width}x{image.Height} {pixel}");
            }
            ((UIView)_capture.Handler!.PlatformView!).BackgroundColor = UIColor.FromDisplayP3(1, 0, 0, 1);
            using var p3 = await new PageContentSnapshotter().CaptureAsync(_capture, default);
            using var p3Bitmap = SKBitmap.FromImage(p3);
            var p3Pixel = p3Bitmap.GetPixel(p3.Width / 2, p3.Height / 2);
            Require(p3Pixel.Red >= 247 && p3Pixel.Blue <= 8 && p3Pixel.Green <= 8, $"P3: {p3Pixel}");
            _results.Add("PASS Display-P3 red");
            _results.Add("PASS all native snapshot tests");
        }
        catch (Exception exception)
        {
            _results.Add("FAIL " + exception);
        }
        try
        {
            var window = Window!;
            NativeMeshTests.Run(_results);
            await DemoWindowLifecycleTests.RunAsync(window, _results);
            await InteractiveRefreshTests.RunAsync(window, _results);
            await SceneSwitchTimingTests.RunAsync(window, _results);
            await DemoRegressionTests.RunAsync(window, _results);
            await ReaderExampleTests.RunAsync(window, _results);
        }
        catch (Exception exception) { _results.Add("FAIL demo " + exception); }
        if (!_results.Any(line => line.StartsWith("FAIL", StringComparison.Ordinal)))
            _results.Add("PASS all native suites");
        File.WriteAllLines(Environment.GetEnvironmentVariable("PAGECURL_TEST_OUTPUT")
            ?? Path.Combine(Path.GetTempPath(), "pagecurl-native-tests.log"), _results);
        Environment.Exit(_results.Any(line => line.StartsWith("FAIL", StringComparison.Ordinal)) ? 1 : 0);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
