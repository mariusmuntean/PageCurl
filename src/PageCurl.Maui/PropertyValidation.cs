using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace PageCurl.Maui;

internal static class PropertyValidation
{
    public static bool IsColor(BindableObject bindable, object? value) => value is Color;
}
