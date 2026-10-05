# Quick start

You need a .NET 10 MAUI application and the workload for your target platform.
The repository pins the SDK and workload set in `global.json`.

## Reference the library

The source repository currently requires access. Until a published package is verified, add a project reference to
`src/PageCurl.Maui/PageCurl.Maui.csproj` from the cloned repository. You can also
pack the projects into a local NuGet feed. The MAUI package depends on Core;
your application does not need a separate Core reference.

## Register SkiaSharp

Add the handler registration in `MauiProgram.cs`:

```csharp
using SkiaSharp.Views.Maui.Controls.Hosting;

var builder = MauiApp.CreateBuilder();
builder.UseMauiApp<App>().UseSkiaSharp();
```

## Display a live page

Add the namespace and control to a XAML page:

```xml
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:curl="clr-namespace:PageCurl.Maui;assembly=PageCurl.Maui">
    <curl:PageCurlView x:Name="Reader" PageAspectRatio="0.75">
        <curl:PageCurlView.FrontContent>
            <Grid BackgroundColor="#FAF8F1" Padding="24">
                <Label Text="First page" TextColor="#1C211F" />
            </Grid>
        </curl:PageCurlView.FrontContent>
    </curl:PageCurlView>
</ContentPage>
```

The view stays interactive while flat. The control captures it after layout
and uses that image during the turn. Give the control a nonzero layout size.

## Connect navigation

Maintain the current index in your application. Change it only after a committed
completion; cancelled turns leave it unchanged.

```csharp
using PageCurl.Core;

Reader.PageTurnCompleted += (_, e) =>
{
    if (!e.Committed) return;
    pageIndex += e.Direction == PageTurnDirection.Forward ? 1 : -1;
    Reader.FrontContent = CreatePage(pageIndex);
};
```

Use `PageNavigationState` to enforce first/last boundaries and connect labeled
Previous/Next buttons through `TryTurn`. See the sample for complete wiring,
including readiness and scene changes. Do not start another turn while required
snapshots are unavailable.

Read [images and live content](content.md) for backside and refresh behavior.
