using PageCurl.Core;
using PageCurl.Maui;

namespace PageCurl.Demo;

public partial class MainPage : ContentPage
{
    private readonly PageNavigationState _navigation;
    private DemoPageSet? _pageSet;

    public MainPage()
    {
        InitializeComponent();
        _navigation = new PageNavigationState(4);
        BindingContext = _navigation;
        EnsurePageSet();
    }

    private void OnPageTurnCompleted(object? sender, PageTurnCompletedEventArgs args)
    {
        if (!args.Committed)
        {
            StatusLabel.Text = "TURN CANCELLED";
            PageHintLabel.Text = "Release before the midpoint to return to the current page.";
            return;
        }

        if (!_navigation.TryCommit(args.Direction))
        {
            StatusLabel.Text = "TURN UNAVAILABLE";
            PageHintLabel.Text = "The page is already at this edge.";
            return;
        }

        ShowPage();
        StatusLabel.Text = _navigation.CanTurnForward ? "CURL COMPLETE" : "FINAL PAGE";
        PageHintLabel.Text = args.Direction == PageTurnDirection.Forward
            ? "Forward curl completed from the right side or a corner."
            : "Backward curl completed from the left side or a corner.";
    }

    private void OnPreviousClicked(object? sender, EventArgs e) =>
        RequestTurn(PageTurnDirection.Backward);

    private void OnNextClicked(object? sender, EventArgs e) =>
        RequestTurn(PageTurnDirection.Forward);

    private void OnStageSizeChanged(object? sender, EventArgs e)
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        StageGrid.HeightRequest = Width > Height ? 250 : 340;
    }

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
        if (!canTurn)
        {
            return;
        }

        StatusLabel.Text = "TURNING...";
        PageHintLabel.Text = direction == PageTurnDirection.Forward
            ? "Curling from the right side or a corner..."
            : "Curling from the left side or a corner...";
        if (!PageView.TryTurn(direction))
        {
            StatusLabel.Text = "TURN UNAVAILABLE";
            PageHintLabel.Text = "The renderer is not ready for a new turn.";
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
        PageCounterLabel.Text = $"{pageIndex + 1:00} / {pages.Count:00}";
        StatusLabel.Text = _navigation.CanTurnForward ? "READY TO TURN" : "FINAL PAGE";
        PageHintLabel.Text = "Drag from a side or corner to reveal the next fold.";
    }
}
