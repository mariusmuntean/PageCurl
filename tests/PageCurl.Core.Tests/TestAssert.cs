namespace PageCurl.Core.Tests;

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

    public static void NotNull(object? value, string? message = null) =>
        True(value is not null, message ?? "Expected a non-null value.");

    public static void Same(object expected, object actual, string? message = null) =>
        True(ReferenceEquals(expected, actual), message ?? "Expected the same object instance.");

    public static void Equal<T>(T expected, T actual, int precision = -1)
    {
        if (precision >= 0 && expected is IConvertible && actual is IConvertible)
        {
            var expectedValue = Convert.ToDouble(expected, System.Globalization.CultureInfo.InvariantCulture);
            var actualValue = Convert.ToDouble(actual, System.Globalization.CultureInfo.InvariantCulture);
            if (Math.Round(actualValue, precision) != Math.Round(expectedValue, precision))
            {
                throw new InvalidOperationException($"Expected {expected}, but was {actual} (precision {precision}).");
            }

            return;
        }

        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"Expected {expected}, but was {actual}.");
        }
    }

    public static void InRange(float value, float minimum, float maximum)
    {
        if (!float.IsFinite(value) || value < minimum || value > maximum)
        {
            throw new InvalidOperationException($"Expected {value} in range [{minimum}, {maximum}].");
        }
    }

    public static void Throws<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }
}
