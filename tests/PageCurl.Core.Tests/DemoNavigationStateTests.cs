using PageCurl.Core;
using PageCurl.Demo.Model;

namespace PageCurl.Core.Tests;

internal static class DemoNavigationStateTests
{
    public static void RunAll()
    {
        Initial_state_starts_at_the_first_page();
        Boundary_turns_are_rejected_without_changing_state();
        Forward_and_backward_turns_commit_in_the_requested_direction();
        Under_page_selection_follows_the_turn_direction();
    }

    private static void Initial_state_starts_at_the_first_page()
    {
        var state = new DemoNavigationState(3);

        TestAssert.Equal(0, state.PageIndex);
        TestAssert.True(state.CanTurnForward);
        TestAssert.False(state.CanTurnBackward);
    }

    private static void Boundary_turns_are_rejected_without_changing_state()
    {
        var state = new DemoNavigationState(1);

        TestAssert.False(state.TryCommit(PageTurnDirection.Backward));
        TestAssert.False(state.TryCommit(PageTurnDirection.Forward));
        TestAssert.Equal(0, state.PageIndex);
    }

    private static void Forward_and_backward_turns_commit_in_the_requested_direction()
    {
        var state = new DemoNavigationState(3);

        TestAssert.True(state.TryCommit(PageTurnDirection.Forward));
        TestAssert.Equal(1, state.PageIndex);
        TestAssert.True(state.TryCommit(PageTurnDirection.Backward));
        TestAssert.Equal(0, state.PageIndex);
    }

    private static void Under_page_selection_follows_the_turn_direction()
    {
        var state = new DemoNavigationState(3);

        TestAssert.Equal(1, state.GetUnderPageIndex(PageTurnDirection.Forward));
        TestAssert.Equal(0, state.GetUnderPageIndex(PageTurnDirection.Backward));

        state.TryCommit(PageTurnDirection.Forward);
        TestAssert.Equal(2, state.GetUnderPageIndex(PageTurnDirection.Forward));
        TestAssert.Equal(0, state.GetUnderPageIndex(PageTurnDirection.Backward));
    }
}
