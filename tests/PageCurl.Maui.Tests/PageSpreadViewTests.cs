using Microsoft.Maui.Controls;
using SkiaSharp;

namespace PageCurl.Maui.Tests;

internal static class PageSpreadViewTests
{
    public static async Task RunAllAsync()
    {
        await Refresh_captures_current_and_destination_pages();
        await Destination_content_and_snapshots_control_navigation_availability();
    }

    private static async Task Refresh_captures_current_and_destination_pages()
    {
        var snapshotter = new CountingSnapshotter();
        var spread = new PageSpreadView(snapshotter)
        {
            LeftContent = new Grid(),
            RightContent = new Grid(),
            PreviousLeftContent = new Grid(),
            NextRightContent = new Grid(),
        };

        await spread.RefreshSnapshotAsync();

        TestAssert.Equal(4, snapshotter.CaptureCount);
        TestAssert.True(spread.CanTurnForward);
        TestAssert.True(spread.CanTurnBackward);
    }

    private static async Task Destination_content_and_snapshots_control_navigation_availability()
    {
        var spread = new PageSpreadView(new CountingSnapshotter())
        {
            LeftContent = new Grid(),
            RightContent = new Grid(),
        };

        TestAssert.False(spread.CanTurnForward);
        TestAssert.False(spread.CanTurnBackward);

        spread.NextRightContent = new Grid();
        spread.PreviousLeftContent = new Grid();

        TestAssert.False(spread.CanTurnForward);
        TestAssert.False(spread.CanTurnBackward);

        await spread.RefreshSnapshotAsync();

        TestAssert.True(spread.CanTurnForward);
        TestAssert.True(spread.CanTurnBackward);
    }

    private sealed class CountingSnapshotter : IPageContentSnapshotter
    {
        public int CaptureCount { get; private set; }

        public Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CaptureCount++;
            using var surface = SKSurface.Create(new SKImageInfo(4, 4));
            return Task.FromResult(surface.Snapshot());
        }
    }
}
