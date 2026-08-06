namespace PageCurl.Maui.Tests;

internal static class Program
{
    public static async Task<int> Main()
    {
        try
        {
            await PageContentSnapshotterTests.RunAllAsync();
            Console.WriteLine($"PASS {nameof(PageContentSnapshotterTests)}");
            Console.WriteLine("All 1 MAUI test groups passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"FAIL: {exception.Message}");
            Console.Error.WriteLine(exception);
            return 1;
        }
    }
}
