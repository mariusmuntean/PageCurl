namespace PageCurl.Maui.Tests;

internal static class PageSnapshotStateTests
{
    public static void RunAll()
    {
        New_capture_cancels_the_previous_generation();
        Stale_results_are_disposed_without_replacing_last_good_frames();
        Front_and_back_snapshots_replace_and_dispose_independently();
        Disposing_state_cancels_capture_before_disposing_frames();
    }

    private static void New_capture_cancels_the_previous_generation()
    {
        using var state = new PageSnapshotState<TrackedSnapshot>();
        var first = state.BeginCapture(CancellationToken.None);
        var second = state.BeginCapture(CancellationToken.None);

        TestAssert.True(first.Token.IsCancellationRequested);
        TestAssert.False(second.Token.IsCancellationRequested);
        TestAssert.True(second.Generation > first.Generation);
    }

    private static void Stale_results_are_disposed_without_replacing_last_good_frames()
    {
        using var state = new PageSnapshotState<TrackedSnapshot>();
        var firstCapture = state.BeginCapture(CancellationToken.None);
        var current = new TrackedSnapshot();
        TestAssert.True(state.TryReplaceFront(firstCapture.Generation, current));
        var nextCapture = state.BeginCapture(CancellationToken.None);
        var stale = new TrackedSnapshot();

        TestAssert.False(state.TryReplaceFront(firstCapture.Generation, stale));

        TestAssert.True(stale.IsDisposed);
        TestAssert.Same(current, state.Front!);
        TestAssert.False(current.IsDisposed);
        TestAssert.False(nextCapture.Token.IsCancellationRequested);
    }

    private static void Front_and_back_snapshots_replace_and_dispose_independently()
    {
        using var state = new PageSnapshotState<TrackedSnapshot>();
        var capture = state.BeginCapture(CancellationToken.None);
        var firstFront = new TrackedSnapshot();
        var firstBack = new TrackedSnapshot();
        state.TryReplaceFront(capture.Generation, firstFront);
        state.TryReplaceBack(capture.Generation, firstBack);
        var nextFront = new TrackedSnapshot();

        state.TryReplaceFront(capture.Generation, nextFront);

        TestAssert.True(firstFront.IsDisposed);
        TestAssert.False(firstBack.IsDisposed);
        TestAssert.Same(nextFront, state.Front!);
        TestAssert.Same(firstBack, state.Back!);
    }

    private static void Disposing_state_cancels_capture_before_disposing_frames()
    {
        var state = new PageSnapshotState<TrackedSnapshot>();
        var capture = state.BeginCapture(CancellationToken.None);
        var front = new TrackedSnapshot(() =>
            TestAssert.True(capture.Token.IsCancellationRequested));
        var back = new TrackedSnapshot(() =>
            TestAssert.True(capture.Token.IsCancellationRequested));
        state.TryReplaceFront(capture.Generation, front);
        state.TryReplaceBack(capture.Generation, back);

        state.Dispose();

        TestAssert.True(front.IsDisposed);
        TestAssert.True(back.IsDisposed);
    }

    private sealed class TrackedSnapshot(Action? onDispose = null) : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            onDispose?.Invoke();
            IsDisposed = true;
        }
    }
}
