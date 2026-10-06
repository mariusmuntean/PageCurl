using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using PageCurl.Core;
using PageCurl.Demo;
using PageCurl.Maui;
using System.Diagnostics;

namespace PageCurl.Apple.Tests;

internal static class DemoWindowLifecycleTests
{
    public static async Task RunAsync(Window window, List<string> results)
    {
        var page = new MainPage();
        window.Page = page;
        var renderer = page.FindByName<PageTurnView>("SinglePageView");
        var next = page.FindByName<Button>("NextButton");
        var counter = page.FindByName<Label>("PageCounterLabel");
        await WaitUntilAsync(() => renderer.LastContentViewport.IsValid && next.IsEnabled);
        var before = counter.Text;
        ((IButtonController)next).SendClicked();
        Require(renderer.Controller.Snapshot.State == PageTurnState.Settling, "Background probe did not start a turn.");
        try
        {
            ((IWindow)window).Stopped();
            Require(renderer.Controller.Snapshot.State == PageTurnState.Idle,
                "Stopping the window left a page turn settling in the background.");
            await Task.Delay(50);
            Require(counter.Text == before, "The background turn advanced navigation.");
            results.Add("PASS window stop immediately cancels a turn without advancing navigation");
        }
        finally { ((IWindow)window).Resumed(); }

        await WaitUntilAsync(() => next.IsEnabled && renderer.FrontImage is not null);
        ((IButtonController)next).SendClicked();
        await WaitUntilAsync(() => renderer.Controller.Snapshot.State == PageTurnState.Idle);
        Require(counter.Text == "02 / 08", "Resuming the window did not restore navigation.");
        results.Add("PASS window resume restores animated navigation");

        ((IButtonController)page.FindByName<Button>("ContentSceneButton")).SendClicked();
        ((IButtonController)page.FindByName<Button>("VideoContentButton")).SendClicked();
        var content = page.FindByName<PageCurlView>("ContentPageView");
        var layers = (Grid)((ContentView)content.FrontContent!).Content;
        var media = layers.Children.OfType<MediaElement>().Single();
        await WaitUntilAsync(() => media.CurrentState == MediaElementState.Playing);
        try
        {
            ((IWindow)window).Stopped();
            await WaitUntilAsync(() => media.CurrentState == MediaElementState.Paused);
            results.Add("PASS window stop pauses native video playback");
        }
        finally { ((IWindow)window).Resumed(); }
        await WaitUntilAsync(() => media.CurrentState == MediaElementState.Playing);
        results.Add("PASS window resume restores active native video playback");

        ((IButtonController)page.FindByName<Button>("SingleSceneButton")).SendClicked();
        await WaitUntilAsync(() => media.CurrentState == MediaElementState.Paused);
        ((IWindow)window).Stopped();
        ((IWindow)window).Resumed();
        await Task.Delay(50);
        Require(media.CurrentState == MediaElementState.Paused,
            "Resuming an inactive video scene restarted playback.");
        results.Add("PASS window resume leaves inactive video paused");
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var watch = Stopwatch.StartNew();
        while (!condition() && watch.Elapsed < TimeSpan.FromSeconds(3)) await Task.Delay(10);
        Require(condition(), "Window lifecycle probe timed out.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
