namespace PageCurl.Core.Tests;

internal static class Program
{
    public static int Main()
    {
        var tests = new (string Name, Action Run)[]
        {
            (nameof(PageTurnControllerTests), PageTurnControllerTests.RunAll),
            (nameof(PageCurlGeometryTests), PageCurlGeometryTests.RunAll),
            (nameof(PageEdgeTests), PageEdgeTests.RunAll),
            (nameof(PageTurnPointerCaptureTests), PageTurnPointerCaptureTests.RunAll),
            (nameof(PageNavigationStateTests), PageNavigationStateTests.RunAll),
            (nameof(PageTurnPerformanceTests), PageTurnPerformanceTests.RunAll),
            (nameof(PageViewportTests), PageViewportTests.RunAll),
            (nameof(PageAnchorTests), PageAnchorTests.RunAll),
            (nameof(PageAspectLayoutTests), PageAspectLayoutTests.RunAll),
            (nameof(PageBacksideMappingTests), PageBacksideMappingTests.RunAll),
            (nameof(PageSpreadNavigationStateTests), PageSpreadNavigationStateTests.RunAll),
            (nameof(PageTurnFeedbackPolicyTests), PageTurnFeedbackPolicyTests.RunAll),
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
