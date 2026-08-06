namespace PageCurl.Maui.Tests;

internal static class TestAssert
{
    public static void True(bool condition, string? message = null)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message ?? "Expected condition to be true.");
        }
    }

    public static void False(bool condition, string? message = null) =>
        True(!condition, message ?? "Expected condition to be false.");

    public static void Same(object expected, object actual, string? message = null) =>
        True(ReferenceEquals(expected, actual), message ?? "Expected the same object instance.");

    public static void NotNull(object? value, string? message = null) =>
        True(value is not null, message ?? "Expected a non-null value.");

    public static void Equal<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected {expected}, but was {actual}.");
        }
    }
}
