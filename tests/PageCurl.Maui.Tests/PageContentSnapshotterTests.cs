using Microsoft.Maui.Controls;

namespace PageCurl.Maui.Tests;

internal static class PageContentSnapshotterTests
{
    public static async Task RunAllAsync()
    {
        await Neutral_snapshotter_reports_platform_unavailability();
        await Cancelled_capture_stops_before_platform_work();
    }

    private static async Task Neutral_snapshotter_reports_platform_unavailability()
    {
        IPageContentSnapshotter snapshotter = new PageContentSnapshotter();

        await ThrowsAsync<PlatformNotSupportedException>(() =>
            snapshotter.CaptureAsync(new Grid(), CancellationToken.None));
    }

    private static async Task Cancelled_capture_stops_before_platform_work()
    {
        IPageContentSnapshotter snapshotter = new PageContentSnapshotter();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await ThrowsAsync<OperationCanceledException>(() =>
            snapshotter.CaptureAsync(new Grid(), cancellation.Token));
    }

    private static async Task ThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }
}
