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
            BackgroundColor = Color.FromArgb("#E9E4D8"),
            Children =
            {
                new BoxView
                {
                    Color = Color.FromArgb("#3157D5"),
                    WidthRequest = 96,
                    HeightRequest = 96,
                    CornerRadius = 48,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                },
                new Label
                {
                    Text = "PLAY",
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    CharacterSpacing = 1.4,
                    TextColor = Colors.White,
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
                Source = MediaSource.FromResource("paper-motion.mp4"),
                ShouldAutoPlay = true,
                ShouldLoopPlayback = true,
                ShouldShowPlaybackControls = false,
                Aspect = Aspect.AspectFill,
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
                            ? "Poster fallback on this desktop; the bundled clip plays on mobile."
                            : "A bundled local clip, with a capturable poster underneath.",
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
        SemanticProperties.SetDescription(this, "Looping local video page with snapshot poster fallback");
    }

    public void Dispose()
    {
        _media?.Stop();
        _media?.Dispose();
    }

    public void SetActive(bool active)
    {
        if (_media?.Handler is null)
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
#if MACCATALYST
        return false;
#elif ANDROID
        return OperatingSystem.IsAndroidVersionAtLeast(26);
#else
        return true;
#endif
    }
}
