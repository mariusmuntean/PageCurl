using System.Numerics;
using Microsoft.Maui.Dispatching;
using PageCurl.Core;
using PageCurl.Maui;

namespace PageCurl.Demo;

public partial class MainPage : ContentPage
{
    private readonly PageNavigationState _navigation;
    private DemoPageSet? _pageSet;
    private IDispatcherTimer? _testingTimer;
    private string _lastTestingResult = "Last result —";

    public MainPage()
    {
        InitializeComponent();
        _navigation = new PageNavigationState(4);
        BindingContext = _navigation;
        LockedEdgePicker.ItemsSource = new[] { "Auto", "Top", "Right", "Bottom", "Left" };
        LockedEdgePicker.SelectedIndex = 0;
        EnsurePageSet();
    }

    private void OnPageTurnCompleted(object? sender, PageTurnCompletedEventArgs args)
    {
        if (!args.Committed)
        {
            StatusLabel.Text = "Turn cancelled";
            PageHintLabel.Text = "Pull past halfway to complete the turn.";
            _lastTestingResult = $"Last result · {args.Direction.ToString().ToUpperInvariant()} CANCELLED";
            UpdateTestingPanel();
            return;
        }

        if (!_navigation.TryCommit(args.Direction))
        {
            StatusLabel.Text = "Edge reached";
            PageHintLabel.Text = "There is no page beyond this edge.";
            _lastTestingResult = $"Last result · {args.Direction.ToString().ToUpperInvariant()} UNAVAILABLE";
            UpdateTestingPanel();
            return;
        }

        ShowPage();
        StatusLabel.Text = _navigation.CanTurnForward ? "Turn complete" : "Final page";
        PageHintLabel.Text = args.Direction == PageTurnDirection.Forward
            ? "Drag again to continue."
            : "Drag again to go back.";
        _lastTestingResult = $"Last result · {args.Direction.ToString().ToUpperInvariant()} COMMITTED";
        UpdateTestingPanel();
    }

    private void OnPreviousClicked(object? sender, EventArgs e) =>
        RequestTurn(PageTurnDirection.Backward);

    private void OnNextClicked(object? sender, EventArgs e) =>
        RequestTurn(PageTurnDirection.Forward);

    private void OnTestingModeToggled(object? sender, ToggledEventArgs e)
    {
        TestingPanel.IsVisible = e.Value;
        PageView.PerformanceDiagnosticsEnabled = e.Value;

        if (e.Value)
        {
            _testingTimer ??= CreateTestingTimer();
            _testingTimer.Start();
            UpdateTestingPanel();
        }
        else
        {
            _testingTimer?.Stop();
            PageView.PerformanceDiagnosticsEnabled = false;
        }
    }

    private void OnLockedEdgeChanged(object? sender, EventArgs e)
    {
        var edge = LockedEdgePicker.SelectedIndex switch
        {
            1 => PageEdge.Top,
            2 => PageEdge.Right,
            3 => PageEdge.Bottom,
            4 => PageEdge.Left,
            _ => PageEdge.Auto,
        };
        PageView.LockedEdge = edge;
        LockedEdgeLabel.Text = edge == PageEdge.Auto
            ? "Auto selects the opposite edge."
            : $"{edge} stays fixed while the page turns.";
        PageHintLabel.Text = edge == PageEdge.Auto
            ? "Drag the page from any free edge."
            : $"{edge} edge locked. Drag elsewhere to curl.";
        UpdateTestingPanel();
    }

    private void OnSettingsClicked(object? sender, EventArgs e) =>
        SettingsPanel.IsVisible = !SettingsPanel.IsVisible;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        EnsurePageSet();
    }

    protected override void OnDisappearing()
    {
        _testingTimer?.Stop();
        PageView.PerformanceDiagnosticsEnabled = false;
        TestingModeSwitch.IsToggled = false;
        SettingsPanel.IsVisible = false;
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

        StatusLabel.Text = "Turning";
        PageHintLabel.Text = direction == PageTurnDirection.Forward
            ? "Turning forward."
            : "Turning back.";
        if (!PageView.TryTurn(direction))
        {
            StatusLabel.Text = "Not ready";
            PageHintLabel.Text = "Finish the current turn first.";
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

    private IDispatcherTimer CreateTestingTimer()
    {
        var timer = Dispatcher.CreateTimer();
        timer.Interval = TimeSpan.FromMilliseconds(100);
        timer.Tick += OnTestingTimerTick;
        return timer;
    }

    private void OnTestingTimerTick(object? sender, EventArgs e) => UpdateTestingPanel();

    private void UpdateTestingPanel()
    {
        if (!TestingPanel.IsVisible)
        {
            return;
        }

        var snapshot = PageView.Controller.Snapshot;
        TestingStateLabel.Text = $"{snapshot.State.ToString().ToUpperInvariant()} · {snapshot.Direction.ToString().ToUpperInvariant()} · {snapshot.Progress:P0}";
        TestingPointerLabel.Text = $"Pointer {FormatPoint(snapshot.PointerPosition)} · Start {FormatPoint(snapshot.DragStartPosition)}";

        var pageSize = PageView.LastContentViewport.Size;
        var pose = PageView.Controller.GetCurlPose(pageSize);
        TestingPoseLabel.Text = pose.IsActive
            ? $"Pose {FormatPoint(pose.Position)} · {pose.FreeEdge}/{pose.HingeEdge} · Radius {pose.Radius:0.0}"
            : "Pose inactive";

        var performance = PageView.PerformanceSnapshot;
        TestingPerformanceLabel.Text = performance.FrameCount == 0
            ? "Frames — · Render —"
            : $"Frames {performance.FrameCount} · Render {performance.AverageFrameMilliseconds:0.00} ms avg · {performance.MaxFrameAllocatedBytes} B max";
        TestingResultLabel.Text = _lastTestingResult;
    }

    private static string FormatPoint(Vector2 point) =>
        $"({point.X:0.0},{point.Y:0.0})";

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
        StatusLabel.Text = _navigation.CanTurnForward ? "Ready" : "Final page";
        PageHintLabel.Text = "Drag the page from any free edge.";
    }

}
