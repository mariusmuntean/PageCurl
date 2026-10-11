using Microsoft.Maui.Controls;
using System.Reflection;
using CoreGraphics;
using PageCurl.Demo;
using PageCurl.Maui;
using SkiaSharp;
using UIKit;
using static PageCurl.Apple.Tests.NativeTestSupport;

namespace PageCurl.Apple.Tests;

internal static class DemoResizeTests
{
    public static async Task RunAsync(Window window, List<string> results)
    {
        var page = new MainPage();
        window.Page = page;
        await WaitUntilAsync(() => page.FindByName<Grid>("StageHost").Width > 0);
        Click(page, "ContentSceneButton");
        Click(page, "InteractiveContentButton");
        var root = page.FindByName<Grid>("RootGrid");
        root.HorizontalOptions = LayoutOptions.Start;
        root.VerticalOptions = LayoutOptions.Start;
        var stage = page.FindByName<Grid>("StageHost");
        var content = page.FindByName<PageCurlView>("ContentPageView");
        foreach (var size in new[] { (Width: 1152, Height: 900), (Width: 600, Height: 680), (Width: 1152, Height: 900) })
        {
            // Root constraints exercise the actual native layout; route the same
            // width to the responsive handler because desktop Window setters do not resize.
            typeof(MainPage).GetMethod("UpdateResponsiveLayout", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(page, new object[] { (double)size.Width });
            root.WidthRequest = size.Width;
            root.HeightRequest = size.Height;
            await WaitUntilAsync(() => Math.Abs(root.Width - size.Width) < 1 && Math.Abs(root.Height - size.Height) < 1
                && content.FrontContent?.Handler is not null && !AutomaticRefreshQueued(content));
            await content.RefreshSnapshotAsync();
            var frame = (Grid)content.Children[0];
            var native = (UIView)frame.Handler!.PlatformView!;
            var availableWidth = stage.Width - stage.Padding.HorizontalThickness;
            var availableHeight = stage.Height - stage.Padding.VerticalThickness;
            results.Add($"RESIZE root={root.Bounds} stage={stage.Bounds} wrapper={content.Bounds} page={frame.Bounds} request={frame.WidthRequest}x{frame.HeightRequest} native={native.Frame}");
            await WaitUntilAsync(() => Math.Abs(frame.Width - frame.WidthRequest) < 1
                && Math.Abs(frame.Height - frame.HeightRequest) < 1);
            Require(Math.Abs(content.Width - availableWidth) < 1 && Math.Abs(content.Height - availableHeight) < 1,
                "Resized live wrapper escaped stage padding.");
            Require(frame.Bounds.Left >= -1 && frame.Bounds.Top >= -1
                && frame.Bounds.Right <= content.Width + 1 && frame.Bounds.Bottom <= content.Height + 1,
                "Resized live page escaped wrapper bounds.");
            Require(Math.Abs(frame.Width / frame.Height - .75) < .002,
                "Resized live page lost its aspect ratio.");
            Require(Math.Abs((double)native.Bounds.Width - frame.Width) < 1
                && Math.Abs((double)native.Bounds.Height - frame.Height) < 1,
                $"Resized native page disagreed with managed frame: native={native.Bounds}, managed={frame.Bounds}.");
            var live = (ContentView)content.FrontContent!;
            var liveGrid = (Grid)live.Content;
            var counter = (Label)liveGrid.Children[2];
            var button = liveGrid.Children.OfType<Button>().Single();
            Require(counter.Height >= counter.FontSize && button.Width >= 44 && button.Height >= 44,
                "Resized live page lost its counter or action target.");
            var nativeLive = (UIView)live.Handler!.PlatformView!;
            var nativeButton = (UIView)button.Handler!.PlatformView!;
            var buttonOrigin = nativeButton.ConvertPointToView(new CGPoint(nativeButton.Bounds.Width * .75,
                nativeButton.Bounds.Height * .5), nativeLive);
            Require(buttonOrigin.X > 0 && buttonOrigin.Y > 0 && buttonOrigin.X < nativeLive.Bounds.Width
                && buttonOrigin.Y < nativeLive.Bounds.Height, "Resized live action was clipped.");
            using var image = await new PageContentSnapshotter().CaptureAsync(live, default);
            using var pixels = SKBitmap.FromImage(image);
            var pixel = pixels.GetPixel((int)(buttonOrigin.X / nativeLive.Bounds.Width * image.Width),
                (int)(buttonOrigin.Y / nativeLive.Bounds.Height * image.Height));
            Require(pixel.Blue > pixel.Red + 80 && pixel.Blue > pixel.Green + 60 && pixel.Alpha > 240,
                $"Resized live action did not render: {pixel}.");
        }
        results.Add("PASS live page preserves padding and pixels through shrink and expand");
        Click(page, "SpreadSceneButton");
        var spread = page.FindByName<PageSpreadView>("SpreadPageView");
        foreach (var size in new[] { (Width: 1152, Height: 900), (Width: 600, Height: 680), (Width: 1152, Height: 900) })
        {
            typeof(MainPage).GetMethod("UpdateResponsiveLayout", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(page, new object[] { (double)size.Width });
            root.WidthRequest = size.Width;
            root.HeightRequest = size.Height;
            await WaitUntilAsync(() => Math.Abs(root.Width - size.Width) < 1 && Math.Abs(root.Height - size.Height) < 1
                && spread.LeftContent?.Handler is not null && spread.RightContent?.Handler is not null);
            await spread.RefreshSnapshotAsync();
            var frame = (Grid)spread.Children[0];
            var native = (UIView)frame.Handler!.PlatformView!;
            results.Add($"SPREAD_RESIZE root={root.Bounds} stage={stage.Bounds} wrapper={spread.Bounds} page={frame.Bounds} request={frame.WidthRequest}x{frame.HeightRequest} native={native.Frame}");
            await WaitUntilAsync(() => Math.Abs(frame.Width - frame.WidthRequest) < 1
                && Math.Abs(frame.Height - frame.HeightRequest) < 1);
            Require(Math.Abs(spread.Width - (stage.Width - stage.Padding.HorizontalThickness)) < 1
                && Math.Abs(spread.Height - (stage.Height - stage.Padding.VerticalThickness)) < 1,
                "Resized spread wrapper escaped stage padding.");
            Require(frame.Bounds.Left >= -1 && frame.Bounds.Top >= -1
                && frame.Bounds.Right <= spread.Width + 1 && frame.Bounds.Bottom <= spread.Height + 1,
                "Resized spread escaped wrapper bounds.");
            Require(Math.Abs(frame.Width / frame.Height - spread.PageAspectRatio * 2) < .002,
                "Resized spread lost its two-page aspect ratio.");
            Require(Math.Abs((double)native.Bounds.Width - frame.Width) < 1
                && Math.Abs((double)native.Bounds.Height - frame.Height) < 1,
                "Resized native spread disagreed with managed frame.");
            foreach (var sheet in new[] { spread.LeftContent!, spread.RightContent! })
                Require(Math.Abs(sheet.Width * 2 - frame.Width) < 1 && Math.Abs(sheet.Height - frame.Height) < 1,
                    "Resized spread sheet did not fill half of its fitted frame.");
        }
        results.Add("PASS spread preserves padding and page aspect through shrink and expand");
        foreach (var scene in new[] { "ContentSceneButton", "SpreadSceneButton" })
        {
            Click(page, scene);
            var wrapper = scene == "ContentSceneButton" ? (Grid)content : spread;
            wrapper.Padding = new Thickness(18, 24, 30, 36);
            var frame = (Grid)wrapper.Children[0];
            var ratio = scene == "ContentSceneButton" ? content.PageAspectRatio : spread.PageAspectRatio * 2;
            var width = Math.Min(wrapper.Width - wrapper.Padding.HorizontalThickness,
                (wrapper.Height - wrapper.Padding.VerticalThickness) * ratio);
            var height = width / ratio;
            results.Add($"PADDED_RESIZE scene={scene} wrapper={wrapper.Bounds} padding={wrapper.Padding} expected={width}x{height} page={frame.Bounds}");
            await WaitUntilAsync(() => Math.Abs(frame.Width - width) < 1 && Math.Abs(frame.Height - height) < 1);
            Require(frame.Bounds.Left >= wrapper.Padding.Left - 1 && frame.Bounds.Top >= wrapper.Padding.Top - 1
                && frame.Bounds.Right <= wrapper.Width - wrapper.Padding.Right + 1
                && frame.Bounds.Bottom <= wrapper.Height - wrapper.Padding.Bottom + 1,
                "Padded page escaped its layout interior.");
            var native = (UIView)frame.Handler!.PlatformView!;
            Require(Math.Abs((double)native.Bounds.Width - width) < 1
                && Math.Abs((double)native.Bounds.Height - height) < 1,
                "Padded native page did not preserve its fitted size.");
            wrapper.Padding = Thickness.Zero;
        }
        results.Add("PASS live and spread pages fit asymmetric wrapper padding");
    }
}
