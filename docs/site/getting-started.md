# Quick start

You need a .NET 10 MAUI application and the workload for your target platform.
The repository pins the SDK and workload set in `global.json`.

## Reference the library

The source repository currently requires access. From your application's
directory, add a reference to your local checkout:

```bash
dotnet add ReaderApp.csproj reference ../PageCurl/src/PageCurl.Maui/PageCurl.Maui.csproj
```

Replace the project names and relative path with yours. The MAUI project
references Core, so your application needs only the MAUI reference. You can
also consume locally packed packages; NuGet.org consumption remains unverified.

## Register SkiaSharp

Add the handler registration in `MauiProgram.cs`:

```csharp
using SkiaSharp.Views.Maui.Controls.Hosting;

var builder = MauiApp.CreateBuilder();
builder.UseMauiApp<App>().UseSkiaSharp();
```

## Display a live page

Add the namespace, control and navigation buttons to `MainPage.xaml`. This
example uses an application namespace of `ReaderApp`:

```xml
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:curl="clr-namespace:PageCurl.Maui;assembly=PageCurl.Maui"
    x:Class="ReaderApp.MainPage">
    <Grid RowDefinitions="*,Auto" Padding="16" RowSpacing="12">
        <curl:PageCurlView x:Name="Reader" PageAspectRatio="0.75" />
        <HorizontalStackLayout Grid.Row="1" Spacing="12"
                               HorizontalOptions="Center">
            <Button x:Name="PreviousButton" Text="Previous" />
            <Button x:Name="NextButton" Text="Next" />
        </HorizontalStackLayout>
    </Grid>
</ContentPage>
```

The view stays interactive while flat. The control captures it after layout
and uses that image during the turn. Give the control a nonzero layout size.

## Connect navigation

Use `PageNavigationState` for the page index and first/last boundaries. In
`MainPage.xaml.cs`, update the content only after a committed completion:

```csharp
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using PageCurl.Core;

namespace ReaderApp;

public partial class MainPage : ContentPage
{
    private readonly PageNavigationState _navigation = new(pageCount: 3);

    public MainPage()
    {
        InitializeComponent();
        Reader.PageTurnCompleted += (_, e) =>
        {
            if (e.Committed && _navigation.TryCommit(e.Direction)) ShowPage();
        };
        PreviousButton.Clicked += (_, _) =>
        {
            if (_navigation.CanTurnBackward) Reader.TryTurn(PageTurnDirection.Backward);
        };
        NextButton.Clicked += (_, _) =>
        {
            if (_navigation.CanTurnForward) Reader.TryTurn(PageTurnDirection.Forward);
        };
        ShowPage();
    }

    private void ShowPage()
    {
        Reader.FrontContent = new Grid
        {
            BackgroundColor = Color.FromArgb("#FAF8F1"),
            Padding = new Thickness(24),
            Children =
            {
                new Label
                {
                    Text = $"Page {_navigation.PageIndex + 1} of {_navigation.PageCount}",
                    TextColor = Color.FromArgb("#1C211F"),
                },
            },
        };
        PreviousButton.IsEnabled = _navigation.CanTurnBackward;
        NextButton.IsEnabled = _navigation.CanTurnForward;
    }

    protected override void OnDisappearing()
    {
        Reader.CancelInteraction();
        base.OnDisappearing();
    }
}
```

Gestures and buttons reach the same completion handler. `TryCommit` keeps the
model in range, including a gesture at either endpoint. `TryTurn` returns
`false` when the control cannot start a command; leave the model unchanged.
Call commands after layout and snapshot capture, and handle `SnapshotFailed`
when capture fails.

This example replaces the live page after completion. To reveal destination
images beneath it during a turn, supply `UnderPageImage` for forward turns and
`PreviousPageImage` for backward turns. You own those `SKImage` values: keep
them alive while assigned, then cancel interaction and clear the image
properties before disposal. Use [two-page spreads](spreads.md) when the control
should capture live neighboring pages.

Read [images and live content](content.md) for backside and in-place refresh
behavior, and [runtime rules](runtime.md) for UI-thread and lifecycle details.
