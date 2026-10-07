using CommunityToolkit.Maui.Views;

namespace PageCurl.Demo.Views;

internal sealed class VideoPosterPageView : ContentView, IDisposable
{
    private readonly MediaElement? _media;

    public VideoPosterPageView()
    {
        BackgroundColor = Color.FromArgb("#E9E4D8");

        var poster = new Grid
        {
            BackgroundColor = Colors.Black,
            Children =
            {
                new Image
                {
                    Source = "big_buck_bunny_still.jpg",
                    Aspect = Aspect.AspectFit,
                },
                new Label
                {
                    Text = "▶",
                    FontSize = 34,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    BackgroundColor = Color.FromArgb("#803157D5"),
                    Padding = new Thickness(15, 8),
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                },
            },
        };
        var usesPosterFallback = !SupportsVideoPlayback();
        if (!usesPosterFallback)
        {
            _media = new MediaElement
            {
                Source = MediaSource.FromResource("big_buck_bunny_demo.mp4"),
                ShouldAutoPlay = true,
                ShouldLoopPlayback = true,
                ShouldShowPlaybackControls = false,
                Aspect = Aspect.AspectFit,
                InputTransparent = true,
            };
        }

        var caption = new Border
        {
            Margin = new Thickness(18),
            Padding = new Thickness(14, 10),
            BackgroundColor = Color.FromArgb("#E6FAF8EF"),
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 7 },
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.End,
            Content = new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    new Label
                    {
                        Text = "VIDEO CONTENT",
                        FontSize = 9,
                        FontAttributes = FontAttributes.Bold,
                        CharacterSpacing = 1.5,
                        TextColor = Color.FromArgb("#3157D5"),
                    },
                    new Label
                    {
                        Text = usesPosterFallback
                            ? "This device uses the bundled still when video playback is unavailable."
                            : "Big Buck Bunny · Blender Foundation · CC BY 3.0",
                        FontSize = 13,
                        TextColor = Color.FromArgb("#1C211F"),
                    },
                },
            },
        };

        var layers = new Grid { Children = { poster } };
        if (_media is not null)
        {
            layers.Children.Add(_media);
        }

        layers.Children.Add(caption);
        Content = layers;
        SemanticProperties.SetDescription(this, "Looping Big Buck Bunny excerpt with a still-image fallback");
    }

    public void Dispose()
    {
        _media?.Stop();
        _media?.Dispose();
    }

    public void SetActive(bool active)
    {
        if (_media is null)
        {
            return;
        }

        _media.ShouldAutoPlay = active;
        if (_media.Handler is null)
        {
            return;
        }

        if (active)
        {
            _media.Play();
        }
        else
        {
            _media.Pause();
        }
    }

    private static bool SupportsVideoPlayback()
    {
#if ANDROID
        return OperatingSystem.IsAndroidVersionAtLeast(26);
#else
        return true;
#endif
    }
}
