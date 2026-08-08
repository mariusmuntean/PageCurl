using Microsoft.Maui.Controls;
using PageCurl.Core;
using SkiaSharp;

namespace PageCurl.Maui.Tests;

internal static class PageCurlViewTests
{
    public static async Task RunAllAsync()
    {
        await Front_and_back_content_are_captured_independently();
        await Failed_refresh_keeps_the_last_good_snapshot();
        Interaction_properties_forward_to_the_renderer();
        Activation_layer_preserves_live_content_input();
        Flat_content_hides_renderer_outside_turns();
    }

    private static async Task Front_and_back_content_are_captured_independently()
    {
        var snapshotter = new FakeSnapshotter();
        snapshotter.EnqueueImage(SKColors.Red);
        snapshotter.EnqueueImage(SKColors.Blue);
        var view = new PageCurlView(snapshotter)
        {
            FrontContent = new Grid(),
            BackContent = new Grid(),
        };

        await view.RefreshSnapshotAsync();

        TestAssert.NotNull(view.Renderer.FrontImage);
        TestAssert.NotNull(view.Renderer.BackImage);
        TestAssert.False(ReferenceEquals(view.Renderer.FrontImage, view.Renderer.BackImage));
    }

    private static async Task Failed_refresh_keeps_the_last_good_snapshot()
    {
        var snapshotter = new FakeSnapshotter();
        snapshotter.EnqueueImage(SKColors.Red);
        snapshotter.EnqueueImage(SKColors.Blue);
        var view = new PageCurlView(snapshotter)
        {
            FrontContent = new Grid(),
            BackContent = new Grid(),
        };
        await view.RefreshSnapshotAsync();
        var firstFront = view.Renderer.FrontImage;
        var firstBack = view.Renderer.BackImage;
        PageSnapshotFailedEventArgs? failed = null;
        view.SnapshotFailed += (_, args) => failed = args;
        snapshotter.EnqueueFailure(new InvalidOperationException("front failed"));
        snapshotter.EnqueueImage(SKColors.Green);

        await view.RefreshSnapshotAsync();

        TestAssert.Same(firstFront!, view.Renderer.FrontImage!);
        TestAssert.False(ReferenceEquals(firstBack, view.Renderer.BackImage));
        TestAssert.NotNull(failed);
        TestAssert.False(failed!.IsBackside);
        TestAssert.Equal("front failed", failed.Exception.Message);
    }

    private static void Interaction_properties_forward_to_the_renderer()
    {
        var view = new PageCurlView(new FakeSnapshotter())
        {
            LockedAnchor = PageAnchor.BottomRight,
            PageChangeThreshold = 0.42f,
            FlickEnabled = false,
            FlickVelocityThreshold = 2.25f,
            PageAspectRatio = 0.68f,
            BacksideMode = PageBacksideMode.SolidColor,
            BacksideDimFactor = 0.7f,
            ReduceMotion = true,
        };

        TestAssert.Equal(PageAnchor.BottomRight, view.Renderer.LockedAnchor);
        TestAssert.Equal(0.42f, view.Renderer.PageChangeThreshold);
        TestAssert.False(view.Renderer.FlickEnabled);
        TestAssert.Equal(2.25f, view.Renderer.FlickVelocityThreshold);
        TestAssert.Equal(PageBacksideMode.SolidColor, view.Renderer.BacksideMode);
        TestAssert.Equal(0.7f, view.Renderer.BacksideDimFactor);
        TestAssert.True(view.Renderer.ReduceMotion);
    }

    private static void Activation_layer_preserves_live_content_input()
    {
        var view = new PageCurlView(new FakeSnapshotter());
        var pageFrame = (Grid)view.Children[0];
        var activationLayer = (Grid)pageFrame.Children[^1];

        TestAssert.True(activationLayer.InputTransparent);
        TestAssert.False(activationLayer.CascadeInputTransparent);
        TestAssert.True(activationLayer.Children.All(child => !child.InputTransparent));
    }

    private static void Flat_content_hides_renderer_outside_turns()
    {
        var view = new PageCurlView(new FakeSnapshotter());

        TestAssert.False(view.Renderer.IsVisible);

        view.Renderer.IsVisible = true;
        view.CancelInteraction();

        TestAssert.False(view.Renderer.IsVisible);
    }

    private sealed class FakeSnapshotter : IPageContentSnapshotter
    {
        private readonly Queue<Func<SKImage>> _captures = new();

        public void EnqueueImage(SKColor color) => _captures.Enqueue(() =>
        {
            using var surface = SKSurface.Create(new SKImageInfo(4, 4));
            surface.Canvas.Clear(color);
            return surface.Snapshot();
        });

        public void EnqueueFailure(Exception exception) =>
            _captures.Enqueue(() => throw exception);

        public Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_captures.Dequeue()());
        }
    }
}
