using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace PageCurl.Demo;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp();

#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            builder.UseMauiCommunityToolkitMediaElement();
        }
#else
        builder.UseMauiCommunityToolkitMediaElement();
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
