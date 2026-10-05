using Microsoft.Maui.Controls;
using PageCurl.Core;

namespace PageCurl.Maui.Tests;

internal static class PublicApiTests
{
    public static void RunAll()
    {
        Invalid_enum_values_do_not_change_the_control_or_renderer();
        Non_finite_dim_factors_are_rejected_before_publication();
        Finite_dim_factors_are_clamped_consistently();
        Invalid_numeric_options_preserve_the_last_valid_configuration();
        Reduced_motion_pointer_cancellation_completes_once_without_settling();
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
        var wrapper = new PageCurlView { PageChangeThreshold = .3f, FlickVelocityThreshold = 2 };
        foreach (var invalid in new[] { -.1f, 1.1f, float.NaN, float.PositiveInfinity })
            RejectWithoutMutation(wrapper, PageCurlView.PageChangeThresholdProperty, invalid);
        foreach (var invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            RejectWithoutMutation(wrapper, PageCurlView.FlickVelocityThresholdProperty, invalid);
        TestAssert.Equal(.3f, wrapper.Renderer.PageChangeThreshold);
        TestAssert.Equal(2f, wrapper.Renderer.FlickVelocityThreshold);
        RejectWithoutMutation(new PageSpreadView(), PageSpreadView.PageAspectRatioProperty, 0f);
        RejectWithoutMutation(new PageCurlView(), PageCurlView.PageAspectRatioProperty, -1f);
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
