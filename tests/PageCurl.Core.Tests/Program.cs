namespace PageCurl.Core.Tests;

internal static class Program
{
    public static int Main()
    {
        var tests = new (string Name, Action Run)[]
        {
            (nameof(PageTurnControllerTests), PageTurnControllerTests.RunAll),
            (nameof(PageCurlGeometryTests), PageCurlGeometryTests.RunAll),
            (nameof(PageTurnPointerCaptureTests), PageTurnPointerCaptureTests.RunAll),
            (nameof(DemoNavigationStateTests), DemoNavigationStateTests.RunAll),
        };

        try
        {
            foreach (var test in tests)
            {
                test.Run();
                Console.WriteLine($"PASS {test.Name}");
            }

            Console.WriteLine($"All {tests.Length} test groups passed.");
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
