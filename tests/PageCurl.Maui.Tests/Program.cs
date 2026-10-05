using System.Collections.Concurrent;

namespace PageCurl.Maui.Tests;

internal static class Program
{
    public static int Main()
    {
        var previous = SynchronizationContext.Current;
        using var context = new TestUiContext();
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            var tests = RunAsync();
            _ = tests.ContinueWith(
                _ => context.Complete(),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            context.Run();
            return tests.GetAwaiter().GetResult();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }
    }

    private static async Task<int> RunAsync()
    {
        try
        {
            PublicApiTests.RunAll();
            Console.WriteLine($"PASS {nameof(PublicApiTests)}");
            await PageContentSnapshotterTests.RunAllAsync();
            Console.WriteLine($"PASS {nameof(PageContentSnapshotterTests)}");
            PageSnapshotStateTests.RunAll();
            Console.WriteLine($"PASS {nameof(PageSnapshotStateTests)}");
            await PageCurlViewTests.RunAllAsync();
            Console.WriteLine($"PASS {nameof(PageCurlViewTests)}");
            await PageSpreadViewTests.RunAllAsync();
            Console.WriteLine($"PASS {nameof(PageSpreadViewTests)}");
            Console.WriteLine("All 5 MAUI test groups passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"FAIL: {exception.Message}");
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    // These controls have a UI-thread contract. Pump captured continuations
    // on the runner thread, as a native dispatcher would, rather than racing
    // captures and gestures on unrelated thread-pool threads.
    private sealed class TestUiContext : SynchronizationContext, IDisposable
    {
        private readonly BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue = new();

        public override void Post(SendOrPostCallback callback, object? state) =>
            _queue.Add((callback, state));

        public void Run()
        {
            foreach (var (callback, state) in _queue.GetConsumingEnumerable())
            {
                callback(state);
            }
        }

        public void Complete() => _queue.CompleteAdding();

        public void Dispose() => _queue.Dispose();
    }
}
