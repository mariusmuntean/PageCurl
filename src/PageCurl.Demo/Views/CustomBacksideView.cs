namespace PageCurl.Demo.Views;

internal sealed class CustomBacksideView : ContentView
{
    public CustomBacksideView()
    {
        BackgroundColor = Color.FromArgb("#173D36");
        Padding = new Thickness(32);
        Content = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto),
            },
            Children =
            {
                new Label
                {
                    Text = "B",
                    FontSize = 116,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#D9E9C7"),
                    Opacity = 0.22,
                    HorizontalOptions = LayoutOptions.End,
                    VerticalOptions = LayoutOptions.Start,
                },
                new VerticalStackLayout
                {
                    Spacing = 8,
                    VerticalOptions = LayoutOptions.End,
                    Children =
                    {
                        new Label
                        {
                            Text = "CUSTOM BACKSIDE",
                            FontSize = 10,
                            FontAttributes = FontAttributes.Bold,
                            CharacterSpacing = 1.7,
                            TextColor = Color.FromArgb("#A9C79B"),
                        },
                        new Label
                        {
                            Text = "Notes, credits, or an entirely different MAUI view.",
                            FontSize = 25,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#F4F3E7"),
                        },
                    },
                },
            },
        };
        var grid = (Grid)Content;
        grid.SetRow(grid.Children[1], 1);
    }
}
