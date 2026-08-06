namespace PageCurl.Demo.Views;

internal sealed class DemoPageView : ContentView
{
    public DemoPageView(
        int pageNumber,
        string eyebrow,
        string title,
        string body,
        bool showAction = false)
    {
        BackgroundColor = Color.FromArgb((pageNumber % 3) switch
        {
            1 => "#FBF8EF",
            2 => "#F4F0E6",
            _ => "#F8F4EA",
        });
        Padding = new Thickness(20, 20, 20, 18);

        var actionLabel = new Label
        {
            Text = showAction ? "LIVE MAUI CONTENT" : "PAGECURL.MAUI",
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            CharacterSpacing = 1.7,
            TextColor = Color.FromArgb("#3157D5"),
        };
        var titleLabel = new Label
        {
            Text = title,
            FontSize = 21,
            FontAttributes = FontAttributes.Bold,
            LineHeight = 1.0,
            MaxLines = 3,
            LineBreakMode = LineBreakMode.WordWrap,
            TextColor = Color.FromArgb("#1C211F"),
        };
        var bodyLabel = new Label
        {
            Text = body,
            FontSize = 12,
            LineHeight = 1.25,
            MaxLines = 4,
            LineBreakMode = LineBreakMode.WordWrap,
            TextColor = Color.FromArgb("#5C625E"),
        };
        var rule = new BoxView
        {
            HeightRequest = 1,
            BackgroundColor = Color.FromArgb("#CAC5B8"),
            VerticalOptions = LayoutOptions.End,
        };
        var footer = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
            },
        };
        footer.Add(new Label
        {
            Text = eyebrow.ToUpperInvariant(),
            FontSize = 10,
            CharacterSpacing = 1.2,
            TextColor = Color.FromArgb("#6E736E"),
            VerticalOptions = LayoutOptions.Center,
        });
        footer.Add(new Label
        {
            Text = pageNumber.ToString("00"),
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#1C211F"),
        }, 1);

        var grid = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
            },
            RowSpacing = 14,
            Children =
            {
                actionLabel,
                titleLabel,
                bodyLabel,
                rule,
                footer,
            },
        };
        Content = grid;
        grid.SetRow(titleLabel, 1);
        grid.SetRow(bodyLabel, 2);
        grid.SetRow(rule, 4);
        grid.SetRow(footer, 5);
        SemanticProperties.SetDescription(this, $"Page {pageNumber}. {title}");
    }
}
