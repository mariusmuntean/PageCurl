using Microsoft.Maui.Controls;
using PageCurl.Core;

namespace PageCurl.Maui.Tests;

internal static class PublicApiTests
{
    public static void RunAll()
    {
        Constructor_preserves_supplied_interaction_options();
        Invalid_enum_values_do_not_change_the_control_or_renderer();
        Non_finite_dim_factors_are_rejected_before_publication();
        Finite_dim_factors_are_clamped_consistently();
        Invalid_numeric_options_preserve_the_last_valid_configuration();
        Reduced_motion_pointer_cancellation_completes_once_without_settling();
    }

    private static void Constructor_preserves_supplied_interaction_options()
    {
        var options = new PageCurlOptions
        {
            PageChangeThreshold = .3f,
            FlickEnabled = false,
            FlickVelocityThreshold = 2f,
        };
        var view = new PageTurnView(options);

        TestAssert.Equal(.3f, view.PageChangeThreshold);
        TestAssert.False(view.FlickEnabled);
        TestAssert.Equal(2f, view.FlickVelocityThreshold);
        TestAssert.Equal(.3f, options.PageChangeThreshold);
        TestAssert.False(options.FlickEnabled);
        TestAssert.Equal(2f, options.FlickVelocityThreshold);

        view.PageChangeThreshold = .4f;
        view.FlickEnabled = true;
        view.FlickVelocityThreshold = 3f;
        TestAssert.Equal(.4f, options.PageChangeThreshold);
        TestAssert.True(options.FlickEnabled);
        TestAssert.Equal(3f, options.FlickVelocityThreshold);
    }

    private static void Invalid_enum_values_do_not_change_the_control_or_renderer()
    {
        var wrapper = new PageCurlView { LockedAnchor = PageAnchor.TopLeft };
        RejectWithoutMutation(wrapper, PageCurlView.LockedAnchorProperty, (PageAnchor)999);
        TestAssert.Equal(PageAnchor.TopLeft, wrapper.Renderer.LockedAnchor);
        RejectWithoutMutation(wrapper, PageCurlView.BacksideModeProperty, (PageBacksideMode)999);
        TestAssert.Equal(PageBacksideMode.MirroredFront, wrapper.Renderer.BacksideMode);
        var spread = new PageSpreadView();
        RejectWithoutMutation(spread, PageSpreadView.BacksideModeProperty, (PageBacksideMode)999);
        TestAssert.Equal(PageBacksideMode.MirroredFront, spread.Overlay.BacksideMode);
        var renderer = new PageTurnView();
        RejectWithoutMutation(renderer, PageTurnView.BacksideModeProperty, (PageBacksideMode)999);
        RejectWithoutMutation(renderer, PageTurnView.ContentScaleProperty, (PageContentScale)999);
        RejectWithoutMutation(renderer, PageTurnView.LockedEdgeProperty, (PageEdge)999);
        RejectWithoutMutation(renderer, PageTurnView.LockedAnchorProperty, (PageAnchor)999);
        RejectWithoutMutation(renderer, PageTurnView.PageHorizontalPlacementProperty, (PageHorizontalPlacement)999);
    }

    private static void Non_finite_dim_factors_are_rejected_before_publication()
    {
        foreach (var invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            RejectWithoutMutation(new PageTurnView(), PageTurnView.BacksideDimFactorProperty, invalid);
            RejectWithoutMutation(new PageCurlView(), PageCurlView.BacksideDimFactorProperty, invalid);
            RejectWithoutMutation(new PageSpreadView(), PageSpreadView.BacksideDimFactorProperty, invalid);
        }
    }

    private static void Finite_dim_factors_are_clamped_consistently()
    {
        var wrapper = new PageCurlView { BacksideDimFactor = -1 };
        TestAssert.Equal(0f, wrapper.BacksideDimFactor);
        TestAssert.Equal(0f, wrapper.Renderer.BacksideDimFactor);
        wrapper.BacksideDimFactor = 2;
        TestAssert.Equal(1f, wrapper.BacksideDimFactor);
        TestAssert.Equal(1f, wrapper.Renderer.BacksideDimFactor);
        var spread = new PageSpreadView { BacksideDimFactor = 2 };
        TestAssert.Equal(1f, spread.BacksideDimFactor);
        TestAssert.Equal(1f, spread.Overlay.BacksideDimFactor);
    }

    private static void Invalid_numeric_options_preserve_the_last_valid_configuration()
    {
        var renderer = new PageTurnView();
        var wrapper = new PageCurlView();
        var spread = new PageSpreadView();
        object[] thresholds = [-.1f, 1.1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity];
        object[] velocities = [0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity];
        object[] ratios = [-1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity];
        var cases = new (BindableObject Control, BindableProperty Property, object Valid, object[] Invalid)[]
        {
            (renderer, PageTurnView.PageChangeThresholdProperty, .3f, thresholds),
            (wrapper, PageCurlView.PageChangeThresholdProperty, .3f, thresholds),
            (spread, PageSpreadView.PageChangeThresholdProperty, .3f, thresholds),
            (renderer, PageTurnView.FlickVelocityThresholdProperty, 2f, velocities),
            (wrapper, PageCurlView.FlickVelocityThresholdProperty, 2f, velocities),
            (spread, PageSpreadView.FlickVelocityThresholdProperty, 2f, velocities),
            (renderer, PageTurnView.PageAspectRatioProperty, .6f, ratios),
            (wrapper, PageCurlView.PageAspectRatioProperty, .6f, ratios),
            (spread, PageSpreadView.PageAspectRatioProperty, .6f, [0f, .. ratios]),
            (spread, PageSpreadView.GutterWidthProperty, 8d,
                [-1d, 25d, double.NaN, double.PositiveInfinity, double.NegativeInfinity]),
        };
        foreach (var (control, property, valid, invalid) in cases)
        {
            control.SetValue(property, valid);
            foreach (var value in invalid) RejectWithoutMutation(control, property, value);
        }
        // The wrapper computes the renderer ratio from its measured page frame.
        ((Microsoft.Maui.IView)wrapper).Arrange(new Microsoft.Maui.Graphics.Rect(0, 0, 800, 500));
        foreach (var view in new[] { renderer, wrapper.Renderer, spread.Overlay })
        {
            TestAssert.Equal(.3f, view.PageChangeThreshold);
            TestAssert.Equal(2f, view.FlickVelocityThreshold);
            TestAssert.Equal(.6f, view.PageAspectRatio);
        }
        // Endpoints and the intrinsic-ratio sentinel are valid values.
        foreach (var (control, property) in new[]
        {
            ((BindableObject)renderer, PageTurnView.PageChangeThresholdProperty),
            ((BindableObject)wrapper, PageCurlView.PageChangeThresholdProperty),
            ((BindableObject)spread, PageSpreadView.PageChangeThresholdProperty),
        })
        foreach (var endpoint in new[] { 0f, 1f })
        {
            control.SetValue(property, endpoint);
            TestAssert.Equal(endpoint, control.GetValue(property));
        }
        renderer.PageAspectRatio = 0;
        wrapper.PageAspectRatio = 0;
        TestAssert.Equal(0f, renderer.PageAspectRatio);
        TestAssert.Equal(0f, wrapper.PageAspectRatio);
        foreach (var endpoint in new[] { 0d, 24d })
        {
            spread.GutterWidth = endpoint;
            TestAssert.Equal(endpoint, spread.GutterWidth);
        }
    }

    private static void Reduced_motion_pointer_cancellation_completes_once_without_settling()
    {
        var view = new PageTurnView { ReduceMotion = true };
        var count = 0;
        view.PageTurnCompleted += (_, args) => { TestAssert.False(args.Committed); count++; };
        TestAssert.True(view.TryBeginInteraction(new(100, 100), new(100, 200), PageAnchor.Left));
        view.UpdateInteraction(new(20, 100));
        view.EndInteraction(cancelled: true);
        TestAssert.Equal(PageTurnState.Idle, view.Controller.Snapshot.State);
        TestAssert.Equal(1, count);
        view.CancelInteraction();
        TestAssert.Equal(1, count);
    }

    private static void RejectWithoutMutation(BindableObject control, BindableProperty property, object invalid)
    {
        var previous = control.GetValue(property);
        try { control.SetValue(property, invalid); }
        catch (ArgumentException) { }
        TestAssert.Equal(previous, control.GetValue(property));
    }
}
