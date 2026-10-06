using System.Diagnostics;
using System.Reflection;
using Microsoft.Maui.Controls;
using PageCurl.Demo;
using PageCurl.Maui;
using SkiaSharp;

namespace PageCurl.Apple.Tests;

internal static class InteractiveRefreshTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly FieldInfo SnapshotterField = typeof(PageCurlView).GetField("_snapshotter", PrivateInstance)!;
    private static readonly FieldInfo AutomaticQueueField = typeof(PageCurlView).GetField("_refreshQueued", PrivateInstance)!;
    private static readonly FieldInfo InteractiveTaskField = typeof(MainPage).GetField("_interactiveRefreshTask", PrivateInstance)!;

    public static async Task RunAsync(Window window, List<string> results)
    {
        var page = new MainPage();
        var content = page.FindByName<PageCurlView>("ContentPageView");
        var original = SnapshotterField.GetValue(content);
        var captures = new ObservedSnapshotter();
        SnapshotterField.SetValue(content, captures);
        window.Page = page;
        try
        {
            await WaitUntilAsync(() => page.FindByName<PageTurnView>("SinglePageView").LastContentViewport.IsValid);
            foreach (var kind in new[] { "Image", "Video" })
                await Pending_update_ignores_replacement_content(page, content, captures, kind, results);
            await Pending_update_ignores_content_kind_reentry(page, content, captures, results);
            await Pending_update_ignores_scene_reentry(page, content, captures, results);
            await Pending_update_ignores_window_stop(window, page, content, captures, results);
            await Input_during_capture_refreshes_the_latest_counter(page, content, captures, results);
            await Content_replacement_clears_an_obsolete_explicit_capture_status(page, content, captures, results);
        }
        finally
        {
            captures.FinishOutstandingCapture();
            AutomaticQueueField.SetValue(content, false);
            Click(page, "SingleSceneButton");
            SnapshotterField.SetValue(content, original);
        }
    }

    private static async Task Pending_update_ignores_replacement_content(MainPage page,
        PageCurlView content, ObservedSnapshotter captures, string kind, List<string> results)
    {
        var live = await SelectInteractiveAsync(page, content, captures);
        HoldAutomaticQueue(content);
        try
        {
            AddOne(live);
            var queued = InteractiveTask(page);
            Click(page, kind + "ContentButton");
            var replacement = content.FrontContent;
            Require(!ReferenceEquals(live, replacement), "The content-kind command did not replace the live page.");
            await queued.WaitAsync(TimeSpan.FromSeconds(3));
            Require(captures.Captures.Count == 0,
                $"A pending live update captured the incoming {kind} page.");
            Require(page.FindByName<Label>("StatusLabel").Text != "Live content updated",
                $"A pending live update published its status in the incoming {kind} page.");
            results.Add($"PASS pending Add one ignores Interactive-to-{kind} replacement");
        }
        finally { AutomaticQueueField.SetValue(content, false); }
    }

    private static async Task Pending_update_ignores_content_kind_reentry(MainPage page,
        PageCurlView content, ObservedSnapshotter captures, List<string> results)
    {
        var live = await SelectInteractiveAsync(page, content, captures);
        HoldAutomaticQueue(content);
        try
        {
            AddOne(live);
            var queued = InteractiveTask(page);
            Click(page, "ImageContentButton");
            Click(page, "InteractiveContentButton");
            Require(ReferenceEquals(live, content.FrontContent), "Content-kind re-entry did not reuse the live page.");
            await queued.WaitAsync(TimeSpan.FromSeconds(3));
            Require(captures.Captures.Count == 0,
                "A pending live update survived content-kind re-entry with a reused view.");
            Require(page.FindByName<Label>("StatusLabel").Text == "Ready",
                "A pending live update changed the re-entered content status.");
            results.Add("PASS pending Add one ignores Interactive-to-Image-to-Interactive re-entry");
        }
        finally { AutomaticQueueField.SetValue(content, false); }
    }

    private static async Task Pending_update_ignores_scene_reentry(MainPage page,
        PageCurlView content, ObservedSnapshotter captures, List<string> results)
    {
        var live = await SelectInteractiveAsync(page, content, captures);
        HoldAutomaticQueue(content);
        try
        {
            AddOne(live);
            var queued = InteractiveTask(page);
            Click(page, "SingleSceneButton");
            Click(page, "ContentSceneButton");
            await queued.WaitAsync(TimeSpan.FromSeconds(3));
            Require(captures.Captures.Count == 0, "A pending live update captured after scene re-entry.");
            Require(page.FindByName<Label>("StatusLabel").Text == "Ready",
                "A pending live update changed the re-entered scene status.");
            results.Add("PASS pending Add one ignores hidden scene re-entry");
        }
        finally { AutomaticQueueField.SetValue(content, false); }
    }

    private static async Task Pending_update_ignores_window_stop(Window window, MainPage page,
        PageCurlView content, ObservedSnapshotter captures, List<string> results)
    {
        var live = await SelectInteractiveAsync(page, content, captures);
        HoldAutomaticQueue(content);
        try
        {
            AddOne(live);
            var queued = InteractiveTask(page);
            ((IWindow)window).Stopped();
            await queued.WaitAsync(TimeSpan.FromSeconds(3));
            Require(captures.Captures.Count == 0, "A pending live update captured while the window was stopped.");
            Require(page.FindByName<Label>("StatusLabel").Text == "Ready",
                "A pending live update published status while the window was stopped.");
            results.Add("PASS pending Add one ignores window stop");
        }
        finally
        {
            ((IWindow)window).Resumed();
            AutomaticQueueField.SetValue(content, false);
        }
    }

    private static async Task Input_during_capture_refreshes_the_latest_counter(MainPage page,
        PageCurlView content, ObservedSnapshotter captures, List<string> results)
    {
        var live = await SelectInteractiveAsync(page, content, captures);
        HoldAutomaticQueue(content);
        var gate = captures.SuspendNextCapture();
        try
        {
            var before = Counter(live);
            AddOne(live);
            var first = InteractiveTask(page);
            await gate.Started.WaitAsync(TimeSpan.FromSeconds(3));
            Require(captures.Captures.Count == 1 && captures.Captures[0].Counter == before + 1,
                "The suspended capture did not observe the first counter input.");
            for (var click = 0; click < 3; click++) AddOne(live);
            var latest = InteractiveTask(page);
            gate.Finish();
            await Task.WhenAll(first, latest).WaitAsync(TimeSpan.FromSeconds(3));
            Require(Counter(live) == before + 4, "Input during capture dropped counter increments.");
            Require(captures.Captures.Count == 2 && captures.Captures[1].Counter == before + 4,
                "Input during capture did not coalesce a snapshot of the latest counter.");
            Require(content.Renderer.FrontImage?.Width == before + 8,
                "The latest captured counter was not published to the renderer.");
            Require(page.FindByName<Label>("StatusLabel").Text == "Live content updated",
                "The latest counter refresh did not report completion.");
            Require(gate.Image!.Handle == IntPtr.Zero, "The superseded capture retained its independently owned image.");
            results.Add("PASS counter input during suspended capture coalesces and publishes the latest value");
        }
        finally
        {
            gate.Finish();
            AutomaticQueueField.SetValue(content, false);
        }
    }

    private static async Task Content_replacement_clears_an_obsolete_explicit_capture_status(MainPage page,
        PageCurlView content, ObservedSnapshotter captures, List<string> results)
    {
        var live = await SelectInteractiveAsync(page, content, captures);
        HoldAutomaticQueue(content);
        var gate = captures.SuspendNextCapture();
        var refresh = page.FindByName<Button>("RefreshSnapshotButton");
        var status = page.FindByName<Label>("StatusLabel");
        try
        {
            Click(page, "RefreshSnapshotButton");
            await gate.Started.WaitAsync(TimeSpan.FromSeconds(3));
            Require(!refresh.IsEnabled && status.Text == "Capturing",
                "The explicit refresh did not enter its pending capture state.");
            Click(page, "InteractiveContentButton");
            Require(ReferenceEquals(live, content.FrontContent) && status.Text == "Capturing",
                "Selecting the existing content kind cleared an active capture status.");
            Click(page, "ImageContentButton");
            Require(status.Text == "Ready",
                "Replacing content left the incoming page displaying the obsolete Capturing status.");
            gate.Finish();
            await WaitUntilAsync(() => refresh.IsEnabled);
            Require(status.Text == "Ready", "The old explicit capture overwrote the replacement page's status.");
            Require(gate.Image!.Handle == IntPtr.Zero, "The obsolete explicit capture retained its owned image.");
            Require(captures.Captures.Count == 1,
                "The obsolete explicit refresh captured the incoming replacement content.");
            results.Add("PASS content replacement clears pending explicit capture status and disposes obsolete image");
        }
        finally
        {
            gate.Finish();
            try { await WaitUntilAsync(() => refresh.IsEnabled); }
            finally { AutomaticQueueField.SetValue(content, false); }
        }
    }

    private static async Task<ContentView> SelectInteractiveAsync(MainPage page,
        PageCurlView content, ObservedSnapshotter captures)
    {
        Click(page, "SingleSceneButton");
        Click(page, "ContentSceneButton");
        Click(page, "InteractiveContentButton");
        await WaitUntilAsync(() => content.FrontContent?.Handler is not null
            && content.FrontContent.Width > 0 && content.FrontContent.Height > 0
            && !(bool)AutomaticQueueField.GetValue(content)!);
        await content.RefreshSnapshotAsync().WaitAsync(TimeSpan.FromSeconds(3));
        var live = (ContentView)content.FrontContent!;
        captures.LiveContent = live;
        captures.Captures.Clear();
        return live;
    }

    private static void HoldAutomaticQueue(PageCurlView content)
    {
        // Replacement content has its own valid automatic refresh. Isolate the
        // demo's Add one coalescer so that work cannot hide an obsolete request.
        AutomaticQueueField.SetValue(content, true);
    }

    private static Task InteractiveTask(MainPage page) =>
        (Task)(InteractiveTaskField.GetValue(page)
            ?? throw new InvalidOperationException("Add one did not schedule its live refresh."));

    private static int Counter(ContentView live) => int.Parse(((Label)((Grid)live.Content).Children[2]).Text);
    private static void AddOne(ContentView live) =>
        ((IButtonController)((Grid)live.Content).Children.OfType<Button>().Single()).SendClicked();
    private static void Click(MainPage page, string name) => ((IButtonController)page.FindByName<Button>(name)).SendClicked();

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var watch = Stopwatch.StartNew();
        while (!condition() && watch.Elapsed < TimeSpan.FromSeconds(3)) await Task.Delay(10);
        Require(condition(), "Interactive refresh setup timed out.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class ObservedSnapshotter : IPageContentSnapshotter
    {
        private SuspendedCapture? _next;
        private SuspendedCapture? _outstanding;
        public ContentView? LiveContent { get; set; }
        public List<(View Content, int? Counter)> Captures { get; } = new();
        public SuspendedCapture SuspendNextCapture() => _next = new();
        public void FinishOutstandingCapture() => _outstanding?.Finish();

        public Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken)
        {
            var count = ReferenceEquals(content, LiveContent) ? Counter(LiveContent!) : (int?)null;
            Captures.Add((content, count));
            using var surface = SKSurface.Create(new SKImageInfo((count ?? 0) + 4, 4));
            var image = surface.Snapshot();
            if (_next is { } gate)
            {
                _next = null;
                _outstanding = gate;
                return gate.Begin(image);
            }
            return Task.FromResult(image);
        }
    }

    private sealed class SuspendedCapture
    {
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<SKImage> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task Started => _started.Task;
        public SKImage? Image { get; private set; }
        public Task<SKImage> Begin(SKImage image)
        {
            Image = image;
            _started.TrySetResult();
            return _completion.Task;
        }
        public void Finish()
        {
            if (Image is not null) _completion.TrySetResult(Image);
        }
    }
}
