using PageCurl.Core;
using System.ComponentModel;

namespace PageCurl.Demo.Model;

public sealed class DemoNavigationState : INotifyPropertyChanged
{
    private readonly int _pageCount;

    public DemoNavigationState(int pageCount)
    {
        if (pageCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(pageCount), "At least one page is required.");
        }

        _pageCount = pageCount;
    }

    public int PageIndex { get; private set; }

    public bool CanTurnForward => PageIndex < _pageCount - 1;

    public bool CanTurnBackward => PageIndex > 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool TryCommit(PageTurnDirection direction)
    {
        switch (direction)
        {
            case PageTurnDirection.Forward when CanTurnForward:
                PageIndex++;
                NotifyChanged();
                return true;
            case PageTurnDirection.Backward when CanTurnBackward:
                PageIndex--;
                NotifyChanged();
                return true;
            default:
                return false;
        }
    }

    public int GetUnderPageIndex(PageTurnDirection direction) => direction switch
    {
        PageTurnDirection.Forward => Math.Min(PageIndex + 1, _pageCount - 1),
        PageTurnDirection.Backward => Math.Max(PageIndex - 1, 0),
        _ => PageIndex,
    };

    private void NotifyChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PageIndex)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanTurnForward)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanTurnBackward)));
    }
}
