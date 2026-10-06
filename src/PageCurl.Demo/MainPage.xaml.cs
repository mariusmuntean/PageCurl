using PageCurl.Core;
using PageCurl.Demo.Views;
using PageCurl.Maui;

namespace PageCurl.Demo;

public partial class MainPage : ContentPage
{
    private const double CompactBreakpoint = 720;
    private static readonly PageAnchor[] AnchorValues =
    {
        PageAnchor.Auto,
        PageAnchor.Top,
        PageAnchor.Right,
        PageAnchor.Bottom,
        PageAnchor.Left,
        PageAnchor.TopLeft,
        PageAnchor.TopRight,
        PageAnchor.BottomRight,
        PageAnchor.BottomLeft,
    };
    private static readonly float[] RatioValues = { 0f, 0.68f, 0.75f, 1f };

    private readonly Dictionary<ShowcaseScene, PageNavigationState> _navigation = new()
    {
        [ShowcaseScene.Single] = new(8),
        [ShowcaseScene.Content] = new(8),
        [ShowcaseScene.Backside] = new(8),
    };
    private readonly PageSpreadNavigationState _spreadNavigation = new(8);
    private readonly PageTurnHaptics _haptics = new();
    private DemoPageSet? _pageSet;
    private int? _spreadContentIndex;
    private int? _backsideContentIndex;
    private ImagePageView? _imagePage;
    private InteractivePageView? _interactivePage;
    private VideoPosterPageView? _videoPage;
    private ShowcaseScene _scene;
    private ShowcaseContentKind _contentKind = ShowcaseContentKind.Image;
    private PageBacksideMode _backsideMode = PageBacksideMode.CustomContent;
    private bool _isCompact;
    private bool _pageActive;
    private int _lifecycleRevision;
    private int _sceneRevision;
    private Window? _observedWindow;
    private (int Lifecycle, int Scene)? _interactiveRefreshEpoch;
    private Task? _interactiveRefreshTask;

    public MainPage()
    {
        InitializeComponent();
        AnchorPicker.ItemsSource = new[]
        {
            "Auto", "Top", "Right", "Bottom", "Left",
            "Top left", "Top right", "Bottom right", "Bottom left",
        };
        RatioPicker.ItemsSource = new[]
        {
            "Use content (3:4)", "Paperback (0.68)", "Classic (3:4)", "Square (1:1)",
        };
        AnchorPicker.SelectedIndex = 0;
        RatioPicker.SelectedIndex = 2;
        HapticsSwitch.IsToggled = PageTurnHaptics.IsMobilePlatform;
        _haptics.Enabled = HapticsSwitch.IsToggled;
        SizeChanged += OnPageSizeChanged;
        SpreadPageView.PropertyChanged += (_, e) =>
        {
            if (_scene == ShowcaseScene.Spread && e.PropertyName is nameof(PageSpreadView.CanTurnForward) or nameof(PageSpreadView.CanTurnBackward))
                UpdateNavigationButtons();
        };
        EnsureResources();
        SelectScene(ShowcaseScene.Single);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ObserveWindow(Window);
        _pageActive = true;
        _lifecycleRevision++;
        EnsureResources();
        UpdateResponsiveLayout(Width);
        UpdateActiveSceneContent();
    }

    protected override void OnDisappearing()
    {
        ObserveWindow(null);
        _pageActive = false;
        _lifecycleRevision++;
        CancelAllInteractions();
        ClearAllContent();
        _spreadContentIndex = null;
        _backsideContentIndex = null;
        if (_interactivePage is not null) _interactivePage.ContentChanged -= OnInteractiveContentChanged;
        _imagePage = null;
        _interactivePage = null;
        _videoPage?.Dispose();
        _videoPage = null;
        _pageSet?.Dispose();
        _pageSet = null;
        base.OnDisappearing();
    }

    private void ObserveWindow(Window? window)
    {
        if (ReferenceEquals(_observedWindow, window)) return;
        if (_observedWindow is not null)
        {
            _observedWindow.Stopped -= OnWindowStopped;
            _observedWindow.Resumed -= OnWindowResumed;
        }
        _observedWindow = window;
        if (window is not null)
        {
            window.Stopped += OnWindowStopped;
            window.Resumed += OnWindowResumed;
        }
    }

    private void OnWindowStopped(object? sender, EventArgs e)
    {
        _lifecycleRevision++;
        CancelAllInteractions();
        _videoPage?.SetActive(false);
    }

    private void OnWindowResumed(object? sender, EventArgs e)
    {
        if (_pageActive)
            _videoPage?.SetActive(_scene == ShowcaseScene.Content
                && _contentKind == ShowcaseContentKind.Video);
    }

    private void OnSceneClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: string value }
            && Enum.TryParse<ShowcaseScene>(value, out var scene))
        {
            SelectScene(scene);
        }
    }

    private void SelectScene(ShowcaseScene scene)
    {
        _sceneRevision++;
        CancelAllInteractions();
        _scene = scene;
        StatusLabel.Text = "Ready";
        _haptics.Reset();
        _videoPage?.SetActive(scene == ShowcaseScene.Content
            && _contentKind == ShowcaseContentKind.Video);
        SinglePageView.IsVisible = scene == ShowcaseScene.Single;
        SpreadPageView.IsVisible = scene == ShowcaseScene.Spread;
        ContentPageView.IsVisible = scene == ShowcaseScene.Content;
        BacksidePageView.IsVisible = scene == ShowcaseScene.Backside;
        ContentOptions.IsVisible = scene == ShowcaseScene.Content;
        BacksideOptions.IsVisible = scene == ShowcaseScene.Backside;
        AnchorOptions.IsVisible = scene is not ShowcaseScene.Spread;
        SceneTitleLabel.Text = scene switch
        {
            ShowcaseScene.Single => "One page",
            ShowcaseScene.Spread => "Open spread",
            ShowcaseScene.Content => "Live content",
            ShowcaseScene.Backside => "Page back",
            _ => "Page curl",
        };
        OptionsTitleLabel.Text = scene switch
        {
            ShowcaseScene.Content => "Content options",
            ShowcaseScene.Backside => "Page back options",
            ShowcaseScene.Spread => "Spread options",
            _ => "Page options",
        };
        PageHintLabel.Text = scene switch
        {
            ShowcaseScene.Spread => "Drag the outer edge of either page.",
            ShowcaseScene.Content => "Use the live view, then curl from its edge.",
            ShowcaseScene.Backside => "Curl far enough to inspect the underside.",
            _ => "Drag from any free edge or corner.",
        };
        DetailLabel.Text = scene switch
        {
            ShowcaseScene.Spread => "Turn the right page forward or the left page backward.",
            ShowcaseScene.Content => "Choose Image, View or Video in Options.",
            ShowcaseScene.Backside => "Choose Page, Mirror or Color to compare reverse sides.",
            _ => "The page follows the pointer without stretching.",
        };

        UpdateActiveSceneContent();
        UpdateSelectionStyles();
        if (_isCompact)
        {
            SetMobileOptionsOpen(false);
        }
    }

    private void OnPreviousClicked(object? sender, EventArgs e) =>
        RequestTurn(PageTurnDirection.Backward);

    private void OnNextClicked(object? sender, EventArgs e) =>
        RequestTurn(PageTurnDirection.Forward);

    private void RequestTurn(PageTurnDirection direction)
    {
        StatusLabel.Text = "Turning";
        PreviousButton.IsEnabled = false;
        NextButton.IsEnabled = false;
        MobilePreviousButton.IsEnabled = false;
        MobileNextButton.IsEnabled = false;
        var started = _scene switch
        {
            ShowcaseScene.Single => SinglePageView.TryTurn(direction),
            ShowcaseScene.Spread when direction == PageTurnDirection.Forward => SpreadPageView.TryTurnForward(),
            ShowcaseScene.Spread => SpreadPageView.TryTurnBackward(),
            ShowcaseScene.Content => ContentPageView.TryTurn(direction),
            ShowcaseScene.Backside => BacksidePageView.TryTurn(direction),
            _ => false,
        };
        if (!started)
        {
            StatusLabel.Text = "Not available";
            UpdateNavigationButtons();
        }
    }

    private void OnPageTurnCompleted(object? sender, PageTurnCompletedEventArgs args)
    {
        if (!_pageActive || !IsActiveTurnSender(sender)) return;
        _haptics.OnCompleted(args);
        if (!args.Committed)
        {
            StatusLabel.Text = "Returned";
            UpdateNavigationButtons();
            return;
        }

        var committed = _scene == ShowcaseScene.Spread
            ? _spreadNavigation.TryCommit(args.Direction)
            : _navigation[_scene].TryCommit(args.Direction);
        StatusLabel.Text = committed ? "Turn complete" : "Boundary reached";
        UpdateActiveSceneContent();
    }

    private void OnThresholdCrossed(object? sender, PageChangeThresholdCrossedEventArgs args)
    {
        if (!_pageActive || !IsActiveTurnSender(sender)) return;
        _haptics.OnThreshold(args);
        StatusLabel.Text = args.IsCommitCandidate ? "Release to turn" : "Release to return";
    }

    private bool IsActiveTurnSender(object? sender) => _scene switch
    {
        ShowcaseScene.Single => ReferenceEquals(sender, SinglePageView.Controller) || ReferenceEquals(sender, SinglePageView),
        ShowcaseScene.Spread => ReferenceEquals(sender, SpreadPageView),
        ShowcaseScene.Content => ReferenceEquals(sender, ContentPageView),
        ShowcaseScene.Backside => ReferenceEquals(sender, BacksidePageView),
        _ => false,
    };

    private void OnAnchorChanged(object? sender, EventArgs e)
    {
        if (AnchorPicker.SelectedIndex < 0)
        {
            return;
        }

        var anchor = AnchorValues[AnchorPicker.SelectedIndex];
        SinglePageView.LockedAnchor = anchor;
        ContentPageView.LockedAnchor = anchor;
        BacksidePageView.LockedAnchor = anchor;
        AnchorHelpLabel.Text = anchor switch
        {
            PageAnchor.Auto => "Auto chooses from the first inward motion.",
            PageAnchor.TopLeft or PageAnchor.TopRight or PageAnchor.BottomLeft or PageAnchor.BottomRight =>
                $"{FormatAnchor(anchor)} is pinned; the page curls around a cone.",
            _ => $"The {FormatAnchor(anchor).ToLowerInvariant()} edge stays fixed.",
        };
    }

    private void OnThresholdChanged(object? sender, ValueChangedEventArgs e)
    {
        var threshold = (float)e.NewValue;
        ThresholdLabel.Text = threshold.ToString("P0");
        SinglePageView.PageChangeThreshold = threshold;
        SpreadPageView.PageChangeThreshold = threshold;
        ContentPageView.PageChangeThreshold = threshold;
        BacksidePageView.PageChangeThreshold = threshold;
    }

    private void OnFlickToggled(object? sender, ToggledEventArgs e)
    {
        SinglePageView.FlickEnabled = e.Value;
        SpreadPageView.FlickEnabled = e.Value;
        ContentPageView.FlickEnabled = e.Value;
        BacksidePageView.FlickEnabled = e.Value;
    }

    private void OnRatioChanged(object? sender, EventArgs e)
    {
        if (RatioPicker.SelectedIndex < 0)
        {
            return;
        }

        var ratio = RatioValues[RatioPicker.SelectedIndex];
        SinglePageView.PageAspectRatio = ratio;
        ContentPageView.PageAspectRatio = ratio;
        BacksidePageView.PageAspectRatio = ratio;
        SpreadPageView.PageAspectRatio = ratio > 0 ? ratio : 0.75f;
    }

    private void OnReduceMotionToggled(object? sender, ToggledEventArgs e)
    {
        SinglePageView.ReduceMotion = e.Value;
        SpreadPageView.ReduceMotion = e.Value;
        ContentPageView.ReduceMotion = e.Value;
        BacksidePageView.ReduceMotion = e.Value;
    }

    private void OnHapticsToggled(object? sender, ToggledEventArgs e)
    {
        _haptics.Enabled = e.Value;
        _haptics.Reset();
    }

    private void OnContentKindClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: string value }
            && Enum.TryParse<ShowcaseContentKind>(value, out var contentKind))
        {
            if (_contentKind != contentKind)
            {
                _sceneRevision++;
                StatusLabel.Text = "Ready";
            }
            _contentKind = contentKind;
            _videoPage?.SetActive(contentKind == ShowcaseContentKind.Video);
            UpdateContentScene();
            UpdateSelectionStyles();
        }
    }

    private void OnBacksideModeClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: string value }
            && Enum.TryParse<PageBacksideMode>(value, out var mode))
        {
            _backsideMode = mode;
            BacksidePageView.BacksideMode = mode;
            UpdateSelectionStyles();
        }
    }

    private async void OnRefreshSnapshotClicked(object? sender, EventArgs e)
    {
        RefreshSnapshotButton.IsEnabled = false;
        StatusLabel.Text = "Capturing";
        try
        {
            await RefreshContentAsync("Snapshot refreshed", reportCancellation: true);
        }
        finally
        {
            RefreshSnapshotButton.IsEnabled = true;
        }
    }

    internal async Task RefreshContentAsync(string successStatus, bool deferUntilLayout = false,
        bool reportCancellation = false)
    {
        var revision = _lifecycleRevision;
        var sceneRevision = _sceneRevision;
        if (deferUntilLayout) await Task.Yield();
        if (!CanReportResult()) return;

        var failed = false;
        void OnCaptureFailed(object? source, PageSnapshotFailedEventArgs args) => failed = true;
        ContentPageView.SnapshotFailed += OnCaptureFailed;
        try
        {
            await ContentPageView.RefreshSnapshotAsync();
            if (CanReportResult() && !failed) StatusLabel.Text = successStatus;
        }
        catch (OperationCanceledException)
        {
            if (reportCancellation && CanReportResult()) StatusLabel.Text = "Capture replaced";
        }
        finally
        {
            ContentPageView.SnapshotFailed -= OnCaptureFailed;
        }

        bool CanReportResult() => _pageActive && revision == _lifecycleRevision
            && sceneRevision == _sceneRevision && _scene == ShowcaseScene.Content;
    }

    private void OnSnapshotFailed(object? sender, PageSnapshotFailedEventArgs e)
    {
        if (!_pageActive || !IsActiveTurnSender(sender)) return;

        StatusLabel.Text = "Using last snapshot";
        DetailLabel.Text = $"Capture unavailable: {e.Exception.Message}";
    }

    private void OnOptionsClicked(object? sender, EventArgs e)
    {
        if (_isCompact)
        {
            SetMobileOptionsOpen(!MobileOptionsOverlay.IsVisible);
        }
    }

    private void SetMobileOptionsOpen(bool open)
    {
        if (open)
        {
            CancelAllInteractions();
        }

        StageHost.InputTransparent = open;
        MobileOptionsOverlay.IsVisible = open;
    }

    private void OnPageSizeChanged(object? sender, EventArgs e) =>
        UpdateResponsiveLayout(Width);

    private void OnStageSizeChanged(object? sender, EventArgs e)
    {
        var width = Math.Max(StageHost.Width - StageHost.Padding.HorizontalThickness, 0);
        var height = Math.Max(StageHost.Height - StageHost.Padding.VerticalThickness, 0);
        if (width <= 0 || height <= 0)
        {
            return;
        }

        foreach (var view in new View[] { SpreadPageView, ContentPageView, BacksidePageView })
        {
            view.WidthRequest = width;
            view.HeightRequest = height;
        }
    }

    private void UpdateResponsiveLayout(double width)
    {
        if (width <= 0)
        {
            return;
        }

        var compact = width < CompactBreakpoint;
        if (_isCompact == compact && OptionsScroll.Parent is not null)
        {
            return;
        }

        _isCompact = compact;
        DesktopSceneRail.IsVisible = !compact;
        InspectorPanel.IsVisible = !compact;
        DesktopFooter.IsVisible = !compact;
        MobileFooter.IsVisible = compact;
        CloseOptionsButton.IsVisible = compact;
        AdaptiveBody.ColumnDefinitions[0].Width = compact ? 0 : 154;
        AdaptiveBody.ColumnDefinitions[2].Width = compact ? 0 : 294;
        AdaptiveBody.ColumnSpacing = compact ? 0 : 14;
        RootGrid.Padding = compact ? new Thickness(10, 10, 10, 8) : new Thickness(18, 16);

        if (compact)
        {
            InspectorContent.Children.Remove(OptionsScroll);
            if (!MobileOptionsContent.Children.Contains(OptionsScroll))
            {
                MobileOptionsContent.Children.Add(OptionsScroll);
            }
        }
        else
        {
            SetMobileOptionsOpen(false);
            MobileOptionsContent.Children.Remove(OptionsScroll);
            if (!InspectorContent.Children.Contains(OptionsScroll))
            {
                InspectorContent.Children.Add(OptionsScroll);
            }
        }
    }

    private void EnsureResources()
    {
        if (_pageSet is not null)
        {
            return;
        }

        _pageSet = DemoPageSet.Create();
        _imagePage = new ImagePageView();
        _interactivePage = new InteractivePageView();
        _interactivePage.ContentChanged += OnInteractiveContentChanged;
        _videoPage = new VideoPosterPageView();
    }

    private async void OnInteractiveContentChanged(object? sender, EventArgs e)
    {
        var epoch = (_lifecycleRevision, _sceneRevision);
        if (_interactiveRefreshEpoch != epoch)
        {
            _interactiveRefreshEpoch = epoch;
            _interactiveRefreshTask = RefreshInteractiveContentAsync(epoch);
        }
        await _interactiveRefreshTask!;
    }

    private async Task RefreshInteractiveContentAsync((int Lifecycle, int Scene) epoch)
    {
        // Several inputs can arrive in one dispatch. Present their live updates
        // before capturing the latest state once, rather than queuing one native
        // rasterization per click. Explicit Refresh keeps its direct path.
        await Task.Yield();
        await Task.Delay(16);
        if (_interactiveRefreshEpoch == epoch)
        {
            _interactiveRefreshEpoch = null;
            _interactiveRefreshTask = null;
        }
        if (!_pageActive || epoch != (_lifecycleRevision, _sceneRevision)
            || _scene != ShowcaseScene.Content
            || !ReferenceEquals(ContentPageView.FrontContent, _interactivePage)) return;
        await RefreshContentAsync("Live content updated");
    }

    private void UpdateActiveSceneContent()
    {
        if (_pageSet is null)
        {
            return;
        }

        switch (_scene)
        {
            case ShowcaseScene.Single:
                SetImagePage(SinglePageView, _navigation[_scene]);
                break;
            case ShowcaseScene.Spread:
                UpdateSpreadScene();
                break;
            case ShowcaseScene.Content:
                UpdateContentScene();
                break;
            case ShowcaseScene.Backside:
                UpdateBacksideScene();
                break;
        }

        UpdateCounter();
        UpdateNavigationButtons();
    }

    private void SetImagePage(PageTurnView view, PageNavigationState navigation)
    {
        var pages = _pageSet!.Pages;
        view.FrontImage = pages[navigation.PageIndex];
        view.BackImage = pages[navigation.PageIndex];
        view.UnderPageImage = pages[navigation.GetUnderPageIndex(PageTurnDirection.Forward)];
        view.PreviousPageImage = pages[navigation.GetUnderPageIndex(PageTurnDirection.Backward)];
    }

    private void UpdateSpreadScene()
    {
        var left = _spreadNavigation.LeftPageIndex;
        if (_spreadContentIndex == left) return;
        _spreadContentIndex = left;
        var right = _spreadNavigation.RightPageIndex;
        SpreadPageView.LeftContent = CreatePageView(left);
        SpreadPageView.RightContent = right.HasValue ? CreatePageView(right.Value) : null;
        SpreadPageView.PreviousLeftContent = _spreadNavigation.CanTurnBackward
            ? CreatePageView(Math.Max(left - 2, 0))
            : null;
        // The sheet's back faces carry the adjacent pages the turn lands on,
        // so each turn reads like flipping one sheet of a physical book.
        SpreadPageView.PreviousRightContent = _spreadNavigation.CanTurnBackward
            ? CreatePageView(Math.Max(left - 1, 0))
            : null;
        SpreadPageView.NextLeftContent = _spreadNavigation.CanTurnForward
            ? CreatePageView(left + 2)
            : null;
        SpreadPageView.NextRightContent = _spreadNavigation.CanTurnForward
            ? CreatePageView(Math.Min(left + 3, _pageSet!.Pages.Count - 1))
            : null;
    }

    private void UpdateContentScene()
    {
        var navigation = _navigation[ShowcaseScene.Content];
        ContentPageView.FrontContent = _contentKind switch
        {
            ShowcaseContentKind.Interactive => _interactivePage,
            ShowcaseContentKind.Video => _videoPage,
            _ => _imagePage,
        };
        ContentPageView.UnderPageImage = _pageSet!.Pages[navigation.GetUnderPageIndex(PageTurnDirection.Forward)];
        ContentPageView.PreviousPageImage = _pageSet.Pages[navigation.GetUnderPageIndex(PageTurnDirection.Backward)];
    }

    private void UpdateBacksideScene()
    {
        var navigation = _navigation[ShowcaseScene.Backside];
        if (_backsideContentIndex != navigation.PageIndex)
        {
            _backsideContentIndex = navigation.PageIndex;
            BacksidePageView.FrontContent = CreatePageView(navigation.PageIndex);
            BacksidePageView.BackContent = CreatePageView(navigation.GetUnderPageIndex(PageTurnDirection.Forward));
        }
        BacksidePageView.BacksideMode = _backsideMode;
        BacksidePageView.BacksideColor = Color.FromArgb("#D5CBAF");
        BacksidePageView.UnderPageImage = _pageSet!.Pages[navigation.GetUnderPageIndex(PageTurnDirection.Forward)];
        BacksidePageView.PreviousPageImage = _pageSet.Pages[navigation.GetUnderPageIndex(PageTurnDirection.Backward)];
    }

    private DemoPageView CreatePageView(int pageIndex)
    {
        var copy = DemoPageCopy.Pages[pageIndex];
        return new DemoPageView(pageIndex + 1, copy.Eyebrow, copy.Title, copy.Body);
    }

    private void UpdateCounter()
    {
        if (_scene == ShowcaseScene.Spread)
        {
            var right = _spreadNavigation.RightPageIndex;
            PageCounterLabel.Text = right.HasValue
                ? $"{_spreadNavigation.LeftPageIndex + 1:00}–{right.Value + 1:00} / 08"
                : $"{_spreadNavigation.LeftPageIndex + 1:00} / 08";
            return;
        }

        var navigation = _navigation[_scene];
        PageCounterLabel.Text = $"{navigation.PageIndex + 1:00} / {navigation.PageCount:00}";
    }

    private void UpdateNavigationButtons()
    {
        var canBack = _scene == ShowcaseScene.Spread
            ? _spreadNavigation.CanTurnBackward && SpreadPageView.CanTurnBackward
            : _navigation[_scene].CanTurnBackward;
        var canForward = _scene == ShowcaseScene.Spread
            ? _spreadNavigation.CanTurnForward && SpreadPageView.CanTurnForward
            : _navigation[_scene].CanTurnForward;
        PreviousButton.IsEnabled = canBack;
        NextButton.IsEnabled = canForward;
        MobilePreviousButton.IsEnabled = canBack;
        MobileNextButton.IsEnabled = canForward;
    }

    private void UpdateSelectionStyles()
    {
        var selectedScene = (Style)Application.Current!.Resources["SceneButtonSelected"];
        var normalScene = (Style)Application.Current.Resources["SceneButton"];
        SetSceneButtonStyle(SingleSceneButton, MobileSingleSceneButton, ShowcaseScene.Single, normalScene, selectedScene);
        SetSceneButtonStyle(SpreadSceneButton, MobileSpreadSceneButton, ShowcaseScene.Spread, normalScene, selectedScene);
        SetSceneButtonStyle(ContentSceneButton, MobileContentSceneButton, ShowcaseScene.Content, normalScene, selectedScene);
        SetSceneButtonStyle(BacksideSceneButton, MobileBacksideSceneButton, ShowcaseScene.Backside, normalScene, selectedScene);

        var segment = (Style)Application.Current.Resources["SegmentButton"];
        var selected = (Style)Application.Current.Resources["CobaltButton"];
        ImageContentButton.Style = _contentKind == ShowcaseContentKind.Image ? selected : segment;
        InteractiveContentButton.Style = _contentKind == ShowcaseContentKind.Interactive ? selected : segment;
        VideoContentButton.Style = _contentKind == ShowcaseContentKind.Video ? selected : segment;
        MirrorBackButton.Style = _backsideMode == PageBacksideMode.MirroredFront ? selected : segment;
        ColorBackButton.Style = _backsideMode == PageBacksideMode.SolidColor ? selected : segment;
        CustomBackButton.Style = _backsideMode == PageBacksideMode.CustomContent ? selected : segment;
    }

    private void SetSceneButtonStyle(
        Button desktop,
        Button mobile,
        ShowcaseScene scene,
        Style normal,
        Style selected)
    {
        desktop.Style = _scene == scene ? selected : normal;
        mobile.Style = _scene == scene ? selected : normal;
    }

    private void CancelAllInteractions()
    {
        SinglePageView.CancelInteraction();
        ContentPageView.CancelInteraction();
        BacksidePageView.CancelInteraction();
        SpreadPageView.CancelInteraction();
    }

    private void ClearAllContent()
    {
        SinglePageView.FrontImage = null;
        SinglePageView.BackImage = null;
        SinglePageView.UnderPageImage = null;
        SinglePageView.PreviousPageImage = null;

        ContentPageView.FrontContent = null;
        ContentPageView.BackContent = null;
        ContentPageView.UnderPageImage = null;
        ContentPageView.PreviousPageImage = null;
        BacksidePageView.FrontContent = null;
        BacksidePageView.BackContent = null;
        BacksidePageView.UnderPageImage = null;
        BacksidePageView.PreviousPageImage = null;
        SpreadPageView.LeftContent = null;
        SpreadPageView.RightContent = null;
        SpreadPageView.PreviousLeftContent = null;
        SpreadPageView.NextRightContent = null;
        SpreadPageView.PreviousRightContent = null;
        SpreadPageView.NextLeftContent = null;
    }

    private static string FormatAnchor(PageAnchor anchor) => anchor switch
    {
        PageAnchor.TopLeft => "Top left",
        PageAnchor.TopRight => "Top right",
        PageAnchor.BottomLeft => "Bottom left",
        PageAnchor.BottomRight => "Bottom right",
        _ => anchor.ToString(),
    };
}
