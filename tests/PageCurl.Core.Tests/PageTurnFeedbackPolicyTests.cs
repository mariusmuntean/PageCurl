using PageCurl.Core;

namespace PageCurl.Core.Tests;

internal static class PageTurnFeedbackPolicyTests
{
    public static void RunAll()
    {
        Disabled_feedback_returns_no_pattern();
        Threshold_entry_and_exit_click_once_per_transition();
        Committed_completion_is_strong_and_cancel_is_silent();
    }

    private static void Disabled_feedback_returns_no_pattern()
    {
        var policy = new PageTurnFeedbackPolicy();

        var result = policy.GetThresholdPattern(
            new PageChangeThresholdCrossedEventArgs(
                PageTurnDirection.Forward,
                isCommitCandidate: true,
                progress: 0.5f),
            enabled: false);

        TestAssert.Equal(PageTurnFeedbackPattern.None, result);
    }

    private static void Threshold_entry_and_exit_click_once_per_transition()
    {
        var policy = new PageTurnFeedbackPolicy();
        var entry = new PageChangeThresholdCrossedEventArgs(
            PageTurnDirection.Forward,
            isCommitCandidate: true,
            progress: 0.5f);
        var exit = new PageChangeThresholdCrossedEventArgs(
            PageTurnDirection.Forward,
            isCommitCandidate: false,
            progress: 0.4f);

        TestAssert.Equal(PageTurnFeedbackPattern.Click, policy.GetThresholdPattern(entry, true));
        TestAssert.Equal(PageTurnFeedbackPattern.None, policy.GetThresholdPattern(entry, true));
        TestAssert.Equal(PageTurnFeedbackPattern.Click, policy.GetThresholdPattern(exit, true));
        TestAssert.Equal(PageTurnFeedbackPattern.None, policy.GetThresholdPattern(exit, true));
    }

    private static void Committed_completion_is_strong_and_cancel_is_silent()
    {
        var policy = new PageTurnFeedbackPolicy();

        TestAssert.Equal(
            PageTurnFeedbackPattern.Strong,
            policy.GetCompletionPattern(
                new PageTurnCompletedEventArgs(PageTurnDirection.Forward, committed: true),
                enabled: true));
        TestAssert.Equal(
            PageTurnFeedbackPattern.None,
            policy.GetCompletionPattern(
                new PageTurnCompletedEventArgs(PageTurnDirection.Forward, committed: false),
                enabled: true));
    }
}
