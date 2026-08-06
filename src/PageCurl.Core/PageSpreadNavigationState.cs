namespace PageCurl.Core;

/// <summary>Tracks two-page spreads with one shared completion path.</summary>
public sealed class PageSpreadNavigationState
{
    public PageSpreadNavigationState(int pageCount)
    {
        if (pageCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageCount));
        }

        PageCount = pageCount;
    }

    public int PageCount { get; }

    public int SpreadIndex { get; private set; }

    public int LeftPageIndex => SpreadIndex * 2;

    public int? RightPageIndex => LeftPageIndex + 1 < PageCount
        ? LeftPageIndex + 1
        : null;

    public bool CanTurnForward => LeftPageIndex + 2 < PageCount;

    public bool CanTurnBackward => SpreadIndex > 0;

    public event EventHandler? SpreadChanged;

    public bool TryCommit(PageTurnDirection direction)
    {
        switch (direction)
        {
            case PageTurnDirection.Forward when CanTurnForward:
                SpreadIndex++;
                break;
            case PageTurnDirection.Backward when CanTurnBackward:
                SpreadIndex--;
                break;
            default:
                return false;
        }

        SpreadChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }
}
