using Foundation;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using SkiaSharp.Views.Maui.Controls.Hosting;
using UIKit;
using CommunityToolkit.Maui;

namespace PageCurl.Apple.Tests;

public static class Program
{
    public static void Main(string[] args) => UIApplication.Main(args, null, typeof(TestDelegate));
}

[Register("TestDelegate")]
public sealed class TestDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiApp.CreateBuilder()
        .UseMauiApp<TestApp>().UseSkiaSharp().UseMauiCommunityToolkitMediaElement().Build();
}

public sealed class TestApp : PageCurl.Demo.App
{
    protected override Window CreateWindow(IActivationState? activationState) =>
        Environment.GetEnvironmentVariable("PAGECURL_READER_PREVIEW") is not null
            ? new Window(new ReaderApp.ReaderPage()) { Title = "Small reader", Width = 1000, Height = 800 }
            : new Window(new FixturePage());
}

[Register("FixtureSceneDelegate")]
public sealed class FixtureSceneDelegate : MauiUISceneDelegate { }
