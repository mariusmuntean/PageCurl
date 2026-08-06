namespace PageCurl.Maui.Tests;

internal static class Program
{
    public static async Task<int> Main()
    {
        try
        {
            await PageContentSnapshotterTests.RunAllAsync();
            Console.WriteLine($"PASS {nameof(PageContentSnapshotterTests)}");
            PageSnapshotStateTests.RunAll();
            Console.WriteLine($"PASS {nameof(PageSnapshotStateTests)}");
            await PageCurlViewTests.RunAllAsync();
            Console.WriteLine($"PASS {nameof(PageCurlViewTests)}");
            Console.WriteLine("All 3 MAUI test groups passed.");
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
