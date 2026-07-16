using PageCurl.Core;

namespace PageCurl.Core.Tests;

internal static class PageNavigationStateTests
{
    public static void RunAll()
    {
        Initial_state_starts_at_the_first_page();
        Invalid_page_counts_are_rejected();
        Boundary_turns_are_rejected_without_changing_state_or_notifications();
        Forward_and_backward_turns_commit_in_the_requested_direction();
        Successful_turns_notify_all_derived_properties_once();
        Under_page_selection_follows_the_turn_direction_and_clamps();
    }

    private static void Initial_state_starts_at_the_first_page()
    {
        var state = new PageNavigationState(3);

        TestAssert.Equal(3, state.PageCount);
        TestAssert.Equal(0, state.PageIndex);
        TestAssert.True(state.CanTurnForward);
        TestAssert.False(state.CanTurnBackward);
    }

    private static void Invalid_page_counts_are_rejected()
    {
        TestAssert.Throws<ArgumentOutOfRangeException>(() => new PageNavigationState(0));
        TestAssert.Throws<ArgumentOutOfRangeException>(() => new PageNavigationState(-1));
    }

    private static void Boundary_turns_are_rejected_without_changing_state_or_notifications()
    {
        var state = new PageNavigationState(1);
        var changed = new List<string>();
        state.PropertyChanged += (_, args) => changed.Add(args.PropertyName!);

        TestAssert.False(state.TryCommit(PageTurnDirection.Backward));
        TestAssert.False(state.TryCommit(PageTurnDirection.Forward));
        TestAssert.Equal(0, state.PageIndex);
        TestAssert.Equal(0, changed.Count);
    }

    private static void Forward_and_backward_turns_commit_in_the_requested_direction()
    {
        var state = new PageNavigationState(3);

        TestAssert.True(state.TryCommit(PageTurnDirection.Forward));
        TestAssert.Equal(1, state.PageIndex);
        TestAssert.True(state.TryCommit(PageTurnDirection.Backward));
        TestAssert.Equal(0, state.PageIndex);
    }

    private static void Successful_turns_notify_all_derived_properties_once()
    {
        var state = new PageNavigationState(3);
        var changed = new List<string>();
        state.PropertyChanged += (_, args) => changed.Add(args.PropertyName!);

        TestAssert.True(state.TryCommit(PageTurnDirection.Forward));

        TestAssert.Equal(3, changed.Count);
        TestAssert.Equal(nameof(PageNavigationState.PageIndex), changed[0]);
        TestAssert.Equal(nameof(PageNavigationState.CanTurnForward), changed[1]);
        TestAssert.Equal(nameof(PageNavigationState.CanTurnBackward), changed[2]);
    }

    private static void Under_page_selection_follows_the_turn_direction_and_clamps()
    {
        var state = new PageNavigationState(3);

        TestAssert.Equal(1, state.GetUnderPageIndex(PageTurnDirection.Forward));
        TestAssert.Equal(0, state.GetUnderPageIndex(PageTurnDirection.Backward));

        state.TryCommit(PageTurnDirection.Forward);
        TestAssert.Equal(2, state.GetUnderPageIndex(PageTurnDirection.Forward));
        TestAssert.Equal(0, state.GetUnderPageIndex(PageTurnDirection.Backward));

        state.TryCommit(PageTurnDirection.Forward);
        TestAssert.Equal(2, state.GetUnderPageIndex(PageTurnDirection.Forward));
        TestAssert.Equal(1, state.GetUnderPageIndex(PageTurnDirection.Backward));
    }
}
