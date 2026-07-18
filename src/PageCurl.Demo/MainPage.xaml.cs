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
        ApplyLockedEdgeVisuals();
        EnsurePageSet();
    }

    private void OnPageTurnCompleted(object? sender, PageTurnCompletedEventArgs args)
    {
        if (!args.Committed)
        {
            StatusLabel.Text = "TURN CANCELLED";
            PageHintLabel.Text = "Release before the midpoint to return to the current page.";
            _lastTestingResult = $"Last result · {args.Direction.ToString().ToUpperInvariant()} CANCELLED";
            UpdateTestingPanel();
            return;
        }

        if (!_navigation.TryCommit(args.Direction))
        {
            StatusLabel.Text = "TURN UNAVAILABLE";
            PageHintLabel.Text = "The page is already at this edge.";
            _lastTestingResult = $"Last result · {args.Direction.ToString().ToUpperInvariant()} UNAVAILABLE";
            UpdateTestingPanel();
            return;
        }

        ShowPage();
        EdgeHintsOverlay.Opacity = 0.28;
        HintsButton.IsVisible = true;
        StatusLabel.Text = _navigation.CanTurnForward ? "CURL COMPLETE" : "FINAL PAGE";
        PageHintLabel.Text = args.Direction == PageTurnDirection.Forward
            ? "Forward curl completed from the right side or a corner."
            : "Backward curl completed from the left side or a corner.";
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
        UpdateStageHeight();

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

    private void OnStageSizeChanged(object? sender, EventArgs e)
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        UpdateStageHeight();
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
        LockedEdgeLabel.Text = edge.ToString().ToUpperInvariant();
        ApplyLockedEdgeVisuals();
        PageHintLabel.Text = edge == PageEdge.Auto
            ? "Auto hinge: the nearest free edge controls the curl."
            : $"{edge} edge locked. Drag any other edge or corner toward the page.";
        UpdateTestingPanel();
    }

    private void OnShowHintsClicked(object? sender, EventArgs e)
    {
        EdgeHintsOverlay.Opacity = 1;
        HintsButton.IsVisible = false;
    }

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
        HintsButton.IsVisible = false;
        EdgeHintsOverlay.Opacity = 1;
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
        StatusLabel.Text = _navigation.CanTurnForward ? "READY TO TURN" : "FINAL PAGE";
        PageHintLabel.Text = "Drag from a side or corner to reveal the next fold.";
    }

    private void ApplyLockedEdgeVisuals()
    {
        var edge = PageView.LockedEdge;
        var auto = edge == PageEdge.Auto;
        TopEdgeHint.Opacity = auto || edge == PageEdge.Top ? 1 : 0.28;
        RightEdgeHint.Opacity = auto || edge == PageEdge.Right ? 1 : 0.28;
        BottomEdgeHint.Opacity = auto || edge == PageEdge.Bottom ? 1 : 0.28;
        LeftEdgeHint.Opacity = auto || edge == PageEdge.Left ? 1 : 0.28;
        TopLeftCornerHint.Opacity = auto || edge == PageEdge.Top || edge == PageEdge.Left ? 1 : 0.28;
        TopRightCornerHint.Opacity = auto || edge == PageEdge.Top || edge == PageEdge.Right ? 1 : 0.28;
        BottomLeftCornerHint.Opacity = auto || edge == PageEdge.Bottom || edge == PageEdge.Left ? 1 : 0.28;
        BottomRightCornerHint.Opacity = auto || edge == PageEdge.Bottom || edge == PageEdge.Right ? 1 : 0.28;
    }

    private void UpdateStageHeight()
    {
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        var landscape = Width > Height;
        var responsiveLandscapeHeight = Math.Clamp(Height * (TestingPanel.IsVisible ? 0.32 : 0.45), 220, 360);
        StageGrid.HeightRequest = TestingPanel.IsVisible
            ? (landscape ? responsiveLandscapeHeight : 225)
            : (landscape ? responsiveLandscapeHeight : 340);
    }
}
