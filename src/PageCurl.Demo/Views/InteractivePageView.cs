namespace PageCurl.Demo.Views;

internal sealed class InteractivePageView : ContentView
{
    private readonly Label _countLabel;
    private int _count;

    public InteractivePageView()
    {
        BackgroundColor = Color.FromArgb("#FAF7EE");
        Padding = new Thickness(32, 28, 32, 24);

        _countLabel = new Label
        {
            Text = "0",
            FontSize = 52,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#3157D5"),
            HorizontalTextAlignment = TextAlignment.Center,
        };
        var button = new Button
        {
            Text = "Add a mark",
            HeightRequest = 46,
            CornerRadius = 7,
            BackgroundColor = Color.FromArgb("#3157D5"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
        };
        button.Clicked += (_, _) =>
        {
            _count++;
            _countLabel.Text = _count.ToString();
            ContentChanged?.Invoke(this, EventArgs.Empty);
        };

        Content = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
            },
            RowSpacing = 18,
            Children =
            {
                new Label
                {
                    Text = "CONTENTVIEW",
                    FontSize = 10,
                    FontAttributes = FontAttributes.Bold,
                    CharacterSpacing = 1.7,
                    TextColor = Color.FromArgb("#3157D5"),
                },
                new Label
                {
                    Text = "Alive while flat.",
                    FontSize = 30,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Color.FromArgb("#1C211F"),
                },
                _countLabel,
                button,
                new Label
                {
                    Text = "Tap the button, then curl. The live view becomes a cached texture only for the turn.",
                    FontSize = 13,
                    LineHeight = 1.2,
                    TextColor = Color.FromArgb("#666B67"),
                },
            },
        };
        var grid = (Grid)Content;
        grid.SetRow(grid.Children[1], 1);
        grid.SetRow(_countLabel, 2);
        grid.SetRow(button, 3);
        grid.SetRow(grid.Children[4], 4);
        SemanticProperties.SetDescription(button, "Increment the live content counter");
    }

    public event EventHandler? ContentChanged;
}
