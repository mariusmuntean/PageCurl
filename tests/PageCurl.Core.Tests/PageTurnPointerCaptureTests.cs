namespace PageCurl.Core.Tests;

internal static class PageTurnPointerCaptureTests
{
    public static void RunAll()
    {
        var capture = new PageTurnPointerCapture();

        TestAssert.True(capture.TryCapture(17));
        TestAssert.True(capture.Owns(17));
        TestAssert.False(capture.Owns(18));
        TestAssert.False(capture.TryCapture(18));
        TestAssert.True(capture.Release(17));
        TestAssert.False(capture.Release(17));
        TestAssert.True(capture.TryCapture(18));
        capture.Cancel();
        TestAssert.False(capture.Owns(18));
    }
}
