using PageCurl.Core;

namespace PageCurl.Core.Tests;

internal static class PageSpreadNavigationStateTests
{
    public static void RunAll()
    {
        Initial_spread_starts_with_the_first_two_pages();
        Odd_page_count_exposes_a_single_page_on_the_last_spread();
        Forward_and_backward_turns_move_one_spread();
        Boundary_turns_do_not_notify_or_change_indices();
        Successful_completion_notifies_exactly_once();
        Maximum_page_count_retains_the_final_spread_boundary();
    }

    private static void Initial_spread_starts_with_the_first_two_pages()
    {
        var state = new PageSpreadNavigationState(6);

        TestAssert.Equal(0, state.SpreadIndex);
        TestAssert.Equal(0, state.LeftPageIndex);
        TestAssert.Equal(1, state.RightPageIndex);
        TestAssert.True(state.CanTurnForward);
        TestAssert.False(state.CanTurnBackward);
    }

    private static void Odd_page_count_exposes_a_single_page_on_the_last_spread()
    {
        var state = new PageSpreadNavigationState(5);
        state.TryCommit(PageTurnDirection.Forward);
        state.TryCommit(PageTurnDirection.Forward);

        TestAssert.Equal(4, state.LeftPageIndex);
        TestAssert.Equal(null, state.RightPageIndex);
        TestAssert.False(state.CanTurnForward);
        TestAssert.True(state.CanTurnBackward);
    }

    private static void Forward_and_backward_turns_move_one_spread()
    {
        var state = new PageSpreadNavigationState(6);

        TestAssert.True(state.TryCommit(PageTurnDirection.Forward));
        TestAssert.Equal(1, state.SpreadIndex);
        TestAssert.Equal(2, state.LeftPageIndex);
        TestAssert.Equal(3, state.RightPageIndex);
        TestAssert.True(state.TryCommit(PageTurnDirection.Backward));
        TestAssert.Equal(0, state.SpreadIndex);
    }

    private static void Boundary_turns_do_not_notify_or_change_indices()
    {
        var state = new PageSpreadNavigationState(2);
        var notifications = 0;
        state.SpreadChanged += (_, _) => notifications++;

        TestAssert.False(state.TryCommit(PageTurnDirection.Backward));
        TestAssert.False(state.TryCommit(PageTurnDirection.Forward));

        TestAssert.Equal(0, state.SpreadIndex);
        TestAssert.Equal(0, notifications);
    }

    private static void Successful_completion_notifies_exactly_once()
    {
        var state = new PageSpreadNavigationState(4);
        var notifications = 0;
        state.SpreadChanged += (_, _) => notifications++;

        TestAssert.True(state.TryCommit(PageTurnDirection.Forward));

        TestAssert.Equal(1, notifications);
    }

    private static void Maximum_page_count_retains_the_final_spread_boundary()
    {
        var state = new PageSpreadNavigationState(int.MaxValue);
        // Seed the reachable final spread instead of issuing a billion turns.
        typeof(PageSpreadNavigationState).GetProperty(nameof(PageSpreadNavigationState.SpreadIndex))!
            .SetValue(state, 1_073_741_823);
        var notifications = 0;
        state.SpreadChanged += (_, _) => notifications++;

        TestAssert.Equal(2_147_483_646, state.LeftPageIndex);
        TestAssert.Equal(null, state.RightPageIndex);
        TestAssert.False(state.CanTurnForward, "The final spread wrapped its forward boundary.");
        TestAssert.False(state.TryCommit(PageTurnDirection.Forward));
        TestAssert.Equal(1_073_741_823, state.SpreadIndex);
        TestAssert.Equal(0, notifications);

        TestAssert.True(state.TryCommit(PageTurnDirection.Backward));
        TestAssert.Equal(2_147_483_644, state.LeftPageIndex);
        TestAssert.Equal(2_147_483_645, state.RightPageIndex);
        TestAssert.True(state.CanTurnForward);
        TestAssert.True(state.TryCommit(PageTurnDirection.Forward));
        TestAssert.Equal(2_147_483_646, state.LeftPageIndex);
        TestAssert.False(state.TryCommit(PageTurnDirection.Forward));
        TestAssert.Equal(2, notifications);
    }
}
