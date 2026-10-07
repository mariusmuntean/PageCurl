using AVFoundation;
using AVKit;
using Microsoft.Maui.Controls;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using PageCurl.Core;
using PageCurl.Demo;
using PageCurl.Demo.Views;
using PageCurl.Maui;
using UIKit;
using static PageCurl.Apple.Tests.NativeTestSupport;

namespace PageCurl.Apple.Tests;

internal static class DemoWindowLifecycleTests
{
    public static async Task RunAsync(Window window, List<string> results)
    {
        await Inactive_video_does_not_autoplay_after_late_attachment(window, results);
        var page = new MainPage();
        window.Page = page;
        var renderer = page.FindByName<PageTurnView>("SinglePageView");
        var next = page.FindByName<Button>("NextButton");
        var counter = page.FindByName<Label>("PageCounterLabel");
        await WaitUntilAsync(() => renderer.LastContentViewport.IsValid && next.IsEnabled);
        var before = counter.Text;
        Click(page, "NextButton");
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
        Click(page, "NextButton");
        await WaitUntilAsync(() => renderer.Controller.Snapshot.State == PageTurnState.Idle);
        Require(counter.Text == "02 / 08", "Resuming the window did not restore navigation.");
        results.Add("PASS window resume restores animated navigation");

        Click(page, "ContentSceneButton");
        Click(page, "VideoContentButton");
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

        Click(page, "SingleSceneButton");
        await WaitUntilAsync(() => media.CurrentState == MediaElementState.Paused);
        ((IWindow)window).Stopped();
        ((IWindow)window).Resumed();
        await Task.Delay(50);
        Require(media.CurrentState == MediaElementState.Paused,
            "Resuming an inactive video scene restarted playback.");
        results.Add("PASS window resume leaves inactive video paused");
    }

    private static async Task Inactive_video_does_not_autoplay_after_late_attachment(Window window, List<string> results)
    {
        using var video = new VideoPosterPageView();
        var media = ((Grid)video.Content).Children.OfType<MediaElement>().Single();
        var previousPage = window.Page;
        var opened = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var active = false;
        var playedWhileInactive = false;
        EventHandler onOpened = (_, _) => opened.TrySetResult();
        EventHandler<MediaStateChangedEventArgs> onStateChanged = (_, args) =>
        {
            if (!active && args.NewState == MediaElementState.Playing) playedWhileInactive = true;
        };
        media.MediaOpened += onOpened;
        media.StateChanged += onStateChanged;
        try
        {
            Require(media.Handler is null, "Late-attachment video already had a native handler.");
            video.SetActive(false);
            window.Page = new ContentPage { Content = video };
            await opened.Task.WaitAsync(UiTimeout);
            var native = (UIView)media.Handler!.PlatformView!;
            var player = native.Subviews.Select(view => view.NextResponder)
                .OfType<AVPlayerViewController>().Single().Player!;
            await WaitUntilAsync(() => player.Status == AVPlayerStatus.ReadyToPlay);
            Require(player.Rate == 0 && player.TimeControlStatus == AVPlayerTimeControlStatus.Paused
                && !playedWhileInactive && media.CurrentState != MediaElementState.Playing,
                "A video paused before attachment started native playback when its handler arrived.");
            results.Add("PASS pre-attachment pause prevents native video autoplay");
            active = true;
            video.SetActive(true);
            await WaitUntilAsync(() => player.TimeControlStatus == AVPlayerTimeControlStatus.Playing
                && media.CurrentState == MediaElementState.Playing);
            results.Add("PASS explicitly activating a late-attached video plays its loaded source");
            active = false;
            video.SetActive(false);
            await WaitUntilAsync(() => player.Rate == 0 && media.CurrentState == MediaElementState.Paused);
        }
        finally
        {
            media.MediaOpened -= onOpened;
            media.StateChanged -= onStateChanged;
            window.Page = previousPage;
        }
    }
}
