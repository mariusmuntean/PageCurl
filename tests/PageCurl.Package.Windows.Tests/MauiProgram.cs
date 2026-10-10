using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Hosting;
using PageCurl.Core;
using PageCurl.Maui;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace PageCurl.Package.Windows.Tests;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp() => MauiApp.CreateBuilder()
        .UseMauiApp<ConsumerApp>()
        .UseSkiaSharp()
        .Build();
}

public sealed class ConsumerApp : Application
{
    protected override Window CreateWindow(IActivationState? activationState) => new(new ContentPage
    {
        Content = new PageCurlView
        {
            LockedAnchor = PageAnchor.TopLeft,
            FrontContent = new Label { Text = "Windows package consumer" },
        },
    });
}
