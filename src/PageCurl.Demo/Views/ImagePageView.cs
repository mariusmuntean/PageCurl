namespace PageCurl.Demo.Views;

internal sealed class ImagePageView : ContentView
{
    public ImagePageView()
    {
        BackgroundColor = Color.FromArgb("#F4F0E6");
        Padding = new Thickness(24);

        var photo = new Border
        {
            BackgroundColor = Colors.Black,
            StrokeThickness = 0,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Content = new Image
            {
                Source = "big_buck_bunny_still.jpg",
                Aspect = Aspect.AspectFit,
            },
        };

        Content = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto),
            },
            RowSpacing = 16,
            Children =
            {
                new VerticalStackLayout
                {
                    Spacing = 3,
                    Children =
                    {
                        new Label
                        {
                            Text = "IMAGE CONTENT",
                            FontSize = 10,
                            FontAttributes = FontAttributes.Bold,
                            CharacterSpacing = 1.7,
                            TextColor = Color.FromArgb("#3157D5"),
                        },
                        new Label
                        {
                            Text = "Image content",
                            FontSize = 25,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#1C211F"),
                        },
                    },
                },
                photo,
                new Label
                {
                    Text = "Big Buck Bunny · Blender Foundation · CC BY 3.0",
                    FontSize = 11,
                    TextColor = Color.FromArgb("#666B67"),
                },
            },
        };

        var grid = (Grid)Content;
        grid.SetRow(photo, 1);
        grid.SetRow(grid.Children[2], 2);
        SemanticProperties.SetDescription(photo, "Big Buck Bunny standing beside a tree");
    }
}
