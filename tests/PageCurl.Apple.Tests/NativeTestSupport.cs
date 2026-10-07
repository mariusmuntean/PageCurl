using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;
using PageCurl.Demo;
using PageCurl.Maui;

namespace PageCurl.Apple.Tests;

internal static class NativeTestSupport
{
    public static readonly TimeSpan UiTimeout = TimeSpan.FromSeconds(3);
    private static readonly FieldInfo InteractiveTaskField = typeof(MainPage)
        .GetField("_interactiveRefreshTask", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo AutomaticQueueField = typeof(PageCurlView)
        .GetField("_refreshQueued", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly MethodInfo SnapshotReadyMethod = typeof(PageCurlView)
        .GetMethod("AreSnapshotsReady", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public static void Click(MainPage page, string name) =>
        ((IButtonController)page.FindByName<Button>(name)).SendClicked();

    public static void AddOne(ContentView live) =>
        ((IButtonController)((Grid)live.Content).Children.OfType<Button>().Single()).SendClicked();

    public static Task InteractiveTask(MainPage page) =>
        (Task)(InteractiveTaskField.GetValue(page)
            ?? throw new InvalidOperationException("Add one did not schedule its live refresh."));

    public static bool AutomaticRefreshQueued(PageCurlView content) => (bool)AutomaticQueueField.GetValue(content)!;

    public static bool SnapshotReady(PageCurlView content) => (bool)SnapshotReadyMethod.Invoke(content, null)!;

    // Isolate demo-triggered refreshes from legitimate wrapper auto-capture.
    public static void HoldAutomaticQueue(PageCurlView content, bool hold = true) => AutomaticQueueField.SetValue(content, hold);

    public static async Task WaitUntilAsync(Func<bool> condition, [CallerArgumentExpression(nameof(condition))] string expression = "")
    {
        var watch = Stopwatch.StartNew();
        while (!condition() && watch.Elapsed < UiTimeout) await Task.Delay(10);
        Require(condition(), $"Timed out waiting for {expression}.");
    }

    public static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
