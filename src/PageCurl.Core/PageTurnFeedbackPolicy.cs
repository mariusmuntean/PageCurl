namespace PageCurl.Core;

public enum PageTurnFeedbackPattern
{
    None,
    Click,
    Strong,
}

/// <summary>Maps page-turn semantics to platform-independent feedback cues.</summary>
public sealed class PageTurnFeedbackPolicy
{
    private bool? _lastCommitCandidate;

    public PageTurnFeedbackPattern GetThresholdPattern(
        PageChangeThresholdCrossedEventArgs args,
        bool enabled)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (!enabled || _lastCommitCandidate == args.IsCommitCandidate)
        {
            return PageTurnFeedbackPattern.None;
        }

        _lastCommitCandidate = args.IsCommitCandidate;
        return PageTurnFeedbackPattern.Click;
    }

    public PageTurnFeedbackPattern GetCompletionPattern(
        PageTurnCompletedEventArgs args,
        bool enabled)
    {
        ArgumentNullException.ThrowIfNull(args);
        _lastCommitCandidate = null;
        return enabled && args.Committed
            ? PageTurnFeedbackPattern.Strong
            : PageTurnFeedbackPattern.None;
    }

    public void Reset() => _lastCommitCandidate = null;
}
