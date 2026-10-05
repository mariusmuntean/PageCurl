global using Microsoft.Maui.Controls;
using System.Numerics;
using System.Reflection;
using PageCurl.Core;
using PageCurl.Maui;
using SkiaSharp;

// Evidence harness: reports observations, not a replacement for regression tests.
var options = new PageCurlOptions { FlickEnabled = false };
var controller = new PageTurnController(options);
var size = new Vector2(400, 500);
controller.BeginDrag(new Vector2(200, 250), size, PageAnchor.Left);
controller.UpdateDrag(new Vector2(150, 250));
var pose = controller.GetCurlPose(size);
Console.WriteLine($"GRAB: pointer={controller.Snapshot.PointerPosition}, pose={pose.Position}, progress={pose.Progress}");

var capture = new DelayedSnapshotter();
var spread = CreateSpread(capture);
await spread.RefreshSnapshotAsync();
var before = capture.Count;
((Label)spread.RightContent!).Text = "updated in place";
await spread.RefreshSnapshotAsync();
Console.WriteLine($"IN_PLACE: captures before={before}, after={capture.Count}");

var oldRight = new Label { Text = "old", WidthRequest = 11 };
spread.RightContent = oldRight;
var pending = capture.DelayNext();
var oldRefresh = spread.RefreshSnapshotAsync();
spread.RightContent = new Label { Text = "new", WidthRequest = 22 };
var newRefresh = spread.RefreshSnapshotAsync();
pending.SetResult(MakeImage(11));
await Task.WhenAll(oldRefresh, newRefresh);
var current = typeof(PageSpreadView).GetField("_currentSnapshots", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(spread)!;
var image = (SKImage)current.GetType().GetProperty("Back")!.GetValue(current)!;
Console.WriteLine($"STALE: current content width={spread.RightContent.WidthRequest}, stored image width={image.Width}, ready={spread.CanTurnForward}");

capture = new DelayedSnapshotter();
spread = CreateSpread(capture);
await spread.RefreshSnapshotAsync();
spread.LeftContent = new Label { Text = "new left" };
pending = capture.DelayNext();
var refresh = DrainAsync(spread);
var prepared = (bool)typeof(PageSpreadView).GetMethod("PrepareTurn", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(spread, [PageTurnDirection.Forward])!;
spread.Overlay.Controller.BeginProgrammaticTurn(size, PageTurnDirection.Forward);
var turningImage = spread.Overlay.FrontImage!;
spread.RightContent = new Label { Text = "new right" };
pending.SetResult(MakeImage(4));
await Task.Yield();
Console.WriteLine($"DURING_TURN: prepared={prepared}, state={spread.Overlay.Controller.Snapshot.State}, front handle={turningImage.Handle}, overlay retains image={ReferenceEquals(turningImage, spread.Overlay.FrontImage)}");
while (spread.Overlay.Controller.Tick(0.1)) { }
await refresh;

capture = new DelayedSnapshotter();
spread = CreateSpread(capture);
spread.NextLeftContent = new Label { Text = "optional back" };
await spread.RefreshSnapshotAsync();
spread.LeftContent = new Label { Text = "replacement left" };
pending = capture.DelayNext();
refresh = DrainAsync(spread);
spread.NextLeftContent = null;
var queuedRemoval = DrainAsync(spread);
prepared = (bool)typeof(PageSpreadView).GetMethod("PrepareTurn", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(spread, [PageTurnDirection.Forward])!;
Console.WriteLine($"REMOVED_BACK: prepared={prepared}, content null={spread.NextLeftContent is null}, overlay back nonnull={spread.Overlay.BackImage is not null}, mode={spread.Overlay.BacksideMode}");
pending.SetResult(MakeImage(4));
while (spread.Overlay.Controller.Tick(0.1)) { }
// PrepareTurn alone does not start the controller in this observation.
typeof(PageSpreadView).GetMethod("RestoreStaticPages", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(spread, null);
await Task.WhenAll(refresh, queuedRemoval);

var watch = System.Diagnostics.Stopwatch.StartNew();
var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
using (var pageSet = PageCurl.Demo.DemoPageSet.Create())
{
    Console.WriteLine($"DEMO_STARTUP: elapsed_ms={watch.Elapsed.TotalMilliseconds:F1}, allocated_bytes={GC.GetAllocatedBytesForCurrentThread() - allocatedBefore}, pages={pageSet.Pages.Count}");
}
var mesh = new PageMeshBuffer(options);
for (var i = 0; i < 100; i++) PageCurlGeometry.Build(mesh, size, pose, options);
allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
watch.Restart();
for (var i = 0; i < 1000; i++) PageCurlGeometry.Build(mesh, size, pose, options);
var allocationDelta = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
Console.WriteLine($"CORE_WARMED: iterations=1000, mean_ms={watch.Elapsed.TotalMilliseconds / 1000:F3}, allocated_bytes={allocationDelta}");

static Task DrainAsync(PageSpreadView spread) =>
    (Task)typeof(PageSpreadView).GetMethod("DrainSnapshotRefreshAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
        .Invoke(spread, [CancellationToken.None])!;

static PageSpreadView CreateSpread(IPageContentSnapshotter snapshotter) => new(snapshotter)
{
    LeftContent = new Label { Text = "left" },
    RightContent = new Label { Text = "right" },
    PreviousLeftContent = new Label { Text = "previous" },
    NextRightContent = new Label { Text = "next" },
};
static SKImage MakeImage(int width)
{
    using var surface = SKSurface.Create(new SKImageInfo(width, 4));
    return surface.Snapshot();
}

sealed class DelayedSnapshotter : IPageContentSnapshotter
{
    private TaskCompletionSource<SKImage>? _next;
    public int Count { get; private set; }
    public TaskCompletionSource<SKImage> DelayNext() => _next = new();
    public Task<SKImage> CaptureAsync(View view, CancellationToken token)
    {
        Count++;
        if (_next is { } next)
        {
            _next = null;
            return next.Task;
        }
        using var surface = SKSurface.Create(new SKImageInfo(view.WidthRequest > 0 ? (int)view.WidthRequest : 4, 4));
        return Task.FromResult(surface.Snapshot());
    }
}
