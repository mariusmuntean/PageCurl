using Stopwatch = System.Diagnostics.Stopwatch;
using System.Reflection;
using Microsoft.Maui.Controls;
using PageCurl.Core;
using PageCurl.Demo;
using PageCurl.Maui;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using static PageCurl.Apple.Tests.NativeTestSupport;

namespace PageCurl.Apple.Tests;

// Recording uses the linked demo and its normal input/completion paths.
internal static class MotionPreview
{
    private static readonly MethodInfo TouchMethod = typeof(PageTurnView)
        .GetMethod("OnTouch", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)!;

    public static async Task RunAsync(Window window, string signals)
    {
        var page = new MainPage();
        Application.Current!.UserAppTheme = AppTheme.Light;
        window.Title = "PageCurl.Maui";
        window.Width = 1280;
        window.Height = 850;
        window.Page = page;
        var single = page.FindByName<PageTurnView>("SinglePageView");
        page.FindByName<Switch>("FlickSwitch").IsToggled = false;
        await WaitUntilAsync(() => single.LastContentViewport.IsValid);
        File.WriteAllText(Path.Combine(signals, "ready"), "ready");
        await WaitForSignalAsync(signals, "start");
        var timeline = Stopwatch.StartNew();

        await Task.Delay(900);
        await DragAsync(single, .7f);
        await WaitUntilAsync(() => single.Controller.Snapshot.State == PageTurnState.Idle);
        Require(page.FindByName<Label>("PageCounterLabel").Text == "02 / 08", "Preview drag did not commit.");
        await Task.Delay(1600);
        await DragAsync(single, .2f);
        await WaitUntilAsync(() => single.Controller.Snapshot.State == PageTurnState.Idle);
        Require(page.FindByName<Label>("PageCounterLabel").Text == "02 / 08", "Preview snap-back advanced navigation.");

        Click(page, "SpreadSceneButton");
        var spread = page.FindByName<PageSpreadView>("SpreadPageView");
        await WaitUntilAsync(() => spread.CanTurnForward && IsFittedSpread(spread));
        await Task.Delay(1200);
        Click(page, "NextButton");
        await WaitUntilAsync(() => page.FindByName<Label>("StatusLabel").Text == "Turn complete");
        Require(page.FindByName<Label>("PageCounterLabel").Text == "03–04 / 08", "Preview spread did not complete.");
        await WaitUntilAsync(() => spread.CanTurnForward && IsFittedSpread(spread));
        await Task.Delay(1700);

        Click(page, "ContentSceneButton");
        Click(page, "InteractiveContentButton");
        var content = page.FindByName<PageCurlView>("ContentPageView");
        await WaitUntilAsync(() => SnapshotReady(content));
        await Task.Delay(1500);
        AddOne((ContentView)content.FrontContent!);
        await InteractiveTask(page);
        await WaitUntilAsync(() => SnapshotReady(content));
        var live = (Grid)((ContentView)content.FrontContent!).Content;
        Require(((Label)live.Children[2]).Text == "1", "Preview live input was lost.");
        await Task.Delay(1500);
        Click(page, "NextButton");
        await WaitUntilAsync(() => page.FindByName<Label>("StatusLabel").Text == "Turn complete");
        await Task.Delay(2000);
        Click(page, "NextButton");
        await Task.Delay(80);
        Require(content.Renderer.Controller.Snapshot.State != PageTurnState.Idle, "Preview switch did not interrupt an active turn.");
        Click(page, "SingleSceneButton");
        Require(content.Renderer.Controller.Snapshot.State == PageTurnState.Idle, "Preview switch did not cancel the outgoing turn.");
        await WaitUntilAsync(() => single.LastContentViewport.IsValid);
        if (timeline.Elapsed < TimeSpan.FromSeconds(20)) await Task.Delay(TimeSpan.FromSeconds(20) - timeline.Elapsed);
        File.WriteAllText(Path.Combine(signals, "done"), "PASS native motion preview");
        // Keep the recorded window alive until the recorder has flushed its file.
        await WaitForSignalAsync(signals, "finish");
    }

    private static bool IsFittedSpread(PageSpreadView spread)
    {
        var frame = (Grid)spread.Children[0];
        return frame.Width > 0 && frame.Height > 0
            && Math.Abs(frame.Width / frame.Height - spread.PageAspectRatio * 2) < .002
            && frame.Bounds.Left >= -1 && frame.Bounds.Top >= -1
            && frame.Bounds.Right <= spread.Width + 1 && frame.Bounds.Bottom <= spread.Height + 1;
    }

    private static async Task DragAsync(PageTurnView view, float distance)
    {
        var viewport = view.LastContentViewport;
        var grab = new SKPoint(viewport.Origin.X + viewport.Size.X - 2, viewport.Origin.Y + viewport.Size.Y * .85f);
        Touch(view, SKTouchAction.Pressed, grab);
        Require(view.Controller.Snapshot.State == PageTurnState.Dragging, "Preview did not acquire a page.");
        var watch = Stopwatch.StartNew();
        SKPoint point;
        do
        {
            var t = Math.Min(1f, (float)watch.Elapsed.TotalSeconds / 2f);
            point = new SKPoint(grab.X - viewport.Size.X * distance * t, grab.Y - viewport.Size.Y * .15f * t);
            Touch(view, SKTouchAction.Moved, point);
            await Task.Delay(16);
        } while (watch.Elapsed.TotalSeconds < 2);
        await Task.Delay(250);
        Touch(view, SKTouchAction.Released, point);
    }

    private static void Touch(PageTurnView view, SKTouchAction action, SKPoint point) =>
        TouchMethod.Invoke(view, [view, new SKTouchEventArgs(1, action, point, action != SKTouchAction.Released)]);

    private static async Task WaitForSignalAsync(string folder, string name)
    {
        var watch = Stopwatch.StartNew();
        while (!File.Exists(Path.Combine(folder, name)) && watch.Elapsed.TotalSeconds < 45) await Task.Delay(50);
        Require(File.Exists(Path.Combine(folder, name)), $"Preview recorder did not send {name}.");
    }
}
