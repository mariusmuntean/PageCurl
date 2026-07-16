using PageCurl.Core;
using PageCurl.Maui;
using PageCurl.Demo.Model;

namespace PageCurl.Demo;

public partial class MainPage : ContentPage
{
    private readonly DemoNavigationState _navigation;
    private DemoPageSet? _pageSet;

    public MainPage()
    {
        InitializeComponent();
        _navigation = new DemoNavigationState(4);
        BindingContext = _navigation;
        EnsurePageSet();
    }

    private void OnPageTurnCompleted(object? sender, PageTurnCompletedEventArgs args)
    {
        if (args.Committed)
        {
            if (!_navigation.TryCommit(args.Direction))
            {
                StatusLabel.Text = "Turn unavailable";
                return;
            }

            ShowPage();
            return;
        }

        StatusLabel.Text = args.Committed ? "Turn committed" : "Turn cancelled";
    }

    private void OnPreviousClicked(object? sender, EventArgs e) =>
        RequestTurn(PageTurnDirection.Backward);

    private void OnNextClicked(object? sender, EventArgs e) =>
        RequestTurn(PageTurnDirection.Forward);

    protected override void OnAppearing()
    {
        base.OnAppearing();
        EnsurePageSet();
    }

    protected override void OnDisappearing()
    {
        PageView.CancelInteraction();
        PageView.FrontImage = null;
        PageView.BackImage = null;
        PageView.UnderPageImage = null;
        PageView.PreviousPageImage = null;
        PageView.PageTurnCompleted -= OnPageTurnCompleted;
        _pageSet?.Dispose();
        _pageSet = null;
        base.OnDisappearing();
    }

    private void RequestTurn(PageTurnDirection direction)
    {
        var canTurn = direction == PageTurnDirection.Forward
            ? _navigation.CanTurnForward
            : _navigation.CanTurnBackward;
        if (canTurn && !PageView.TryTurn(direction))
        {
            StatusLabel.Text = "Turn unavailable";
        }
    }

    private void EnsurePageSet()
    {
        if (_pageSet is null)
        {
            _pageSet = DemoPageSet.Create();
            PageView.PageTurnCompleted += OnPageTurnCompleted;
        }

        ShowPage();
    }

    private void ShowPage()
    {
        if (_pageSet is null)
        {
            return;
        }

        var pages = _pageSet.Pages;
        var pageIndex = _navigation.PageIndex;
        PageView.FrontImage = pages[pageIndex];
        PageView.BackImage = pages[pageIndex];
        PageView.UnderPageImage = pages[_navigation.GetUnderPageIndex(PageTurnDirection.Forward)];
        PageView.PreviousPageImage = pages[_navigation.GetUnderPageIndex(PageTurnDirection.Backward)];
        StatusLabel.Text = $"Page {pageIndex + 1} of {pages.Count} · drag from either edge";
    }
}
