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
        [ShowcaseScene.CornerPin] = new(8),
    };
    private readonly PageSpreadNavigationState _spreadNavigation = new(8);
    private readonly PageTurnHaptics _haptics = new();
    private DemoPageSet? _pageSet;
    private InteractivePageView? _interactivePage;
    private VideoPosterPageView? _videoPage;
    private ShowcaseScene _scene;
    private ShowcaseContentKind _contentKind = ShowcaseContentKind.Image;
    private PageBacksideMode _backsideMode = PageBacksideMode.MirroredFront;
    private PageAnchor _cornerAnchor = PageAnchor.TopLeft;
    private bool _isCompact;
    private bool _snapshotRetryQueued;

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
        EnsureResources();
        SelectScene(ShowcaseScene.Single);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        EnsureResources();
        UpdateResponsiveLayout(Width);
        UpdateActiveSceneContent();
    }

    protected override void OnDisappearing()
    {
        CancelAllInteractions();
        ClearAllContent();
        _interactivePage = null;
        _videoPage?.Dispose();
        _videoPage = null;
        _pageSet?.Dispose();
        _pageSet = null;
        base.OnDisappearing();
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
        CornerPageView.IsVisible = scene == ShowcaseScene.CornerPin;
        CornerPinMarker.IsVisible = scene == ShowcaseScene.CornerPin;
        ContentOptions.IsVisible = scene == ShowcaseScene.Content;
        BacksideOptions.IsVisible = scene == ShowcaseScene.Backside;
        CornerOptions.IsVisible = scene == ShowcaseScene.CornerPin;
        AnchorOptions.IsVisible = scene is not ShowcaseScene.Spread and not ShowcaseScene.CornerPin;
        SceneTitleLabel.Text = scene switch
        {
            ShowcaseScene.Single => "One page",
            ShowcaseScene.Spread => "Open spread",
            ShowcaseScene.Content => "Live content",
            ShowcaseScene.Backside => "The other side",
            ShowcaseScene.CornerPin => "Pinned corner",
            _ => "Page curl",
        };
        OptionsTitleLabel.Text = scene switch
        {
            ShowcaseScene.Content => "Content + capture",
            ShowcaseScene.Backside => "Backside material",
            ShowcaseScene.CornerPin => "Corner geometry",
            ShowcaseScene.Spread => "Spread behaviour",
            _ => "Page behaviour",
        };
        PageHintLabel.Text = scene switch
        {
            ShowcaseScene.Spread => "Drag the outer edge of either page.",
            ShowcaseScene.Content => "Use the live view, then curl from its edge.",
            ShowcaseScene.Backside => "Curl far enough to inspect the underside.",
            ShowcaseScene.CornerPin => "The marked corner stays pinned. Drag elsewhere.",
            _ => "Drag from any free edge or corner.",
        };
        DetailLabel.Text = scene switch
        {
            ShowcaseScene.Spread => "Forward and backward turns share one completion path.",
            ShowcaseScene.Content => "MAUI content remains interactive while the page is flat.",
            ShowcaseScene.Backside => "Mirror, color, or supply another MAUI view.",
            ShowcaseScene.CornerPin => "The paper wraps a developable cone without stretching.",
            _ => "The page follows the pointer without stretching.",
        };

        UpdateActiveSceneContent();
        UpdateSelectionStyles();
        if (_isCompact)
        {
            MobileOptionsOverlay.IsVisible = false;
        }
    }

    private void OnPreviousClicked(object? sender, EventArgs e) =>
        RequestTurn(PageTurnDirection.Backward);

    private void OnNextClicked(object? sender, EventArgs e) =>
        RequestTurn(PageTurnDirection.Forward);

    private void RequestTurn(PageTurnDirection direction)
    {
        var started = _scene switch
        {
            ShowcaseScene.Single => SinglePageView.TryTurn(direction),
            ShowcaseScene.Spread when direction == PageTurnDirection.Forward => SpreadPageView.TryTurnForward(),
            ShowcaseScene.Spread => SpreadPageView.TryTurnBackward(),
            ShowcaseScene.Content => ContentPageView.TryTurn(direction),
            ShowcaseScene.Backside => BacksidePageView.TryTurn(direction),
            ShowcaseScene.CornerPin => CornerPageView.TryTurn(direction),
            _ => false,
        };
        StatusLabel.Text = started ? "Turning" : "Not available";
        if (started)
        {
            PreviousButton.IsEnabled = false;
            NextButton.IsEnabled = false;
            MobilePreviousButton.IsEnabled = false;
            MobileNextButton.IsEnabled = false;
        }
    }

    private void OnPageTurnCompleted(object? sender, PageTurnCompletedEventArgs args)
    {
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
        _haptics.OnThreshold(args);
        StatusLabel.Text = args.IsCommitCandidate ? "Release to turn" : "Release to return";
    }

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
        CornerPageView.PageChangeThreshold = threshold;
    }

    private void OnFlickToggled(object? sender, ToggledEventArgs e)
    {
        SinglePageView.FlickEnabled = e.Value;
        SpreadPageView.FlickEnabled = e.Value;
        ContentPageView.FlickEnabled = e.Value;
        BacksidePageView.FlickEnabled = e.Value;
        CornerPageView.FlickEnabled = e.Value;
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
        CornerPageView.PageAspectRatio = ratio;
        SpreadPageView.PageAspectRatio = ratio > 0 ? ratio : 0.75f;
    }

    private void OnReduceMotionToggled(object? sender, ToggledEventArgs e)
    {
        SinglePageView.ReduceMotion = e.Value;
        SpreadPageView.ReduceMotion = e.Value;
        ContentPageView.ReduceMotion = e.Value;
        BacksidePageView.ReduceMotion = e.Value;
        CornerPageView.ReduceMotion = e.Value;
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
            _contentKind = contentKind;
            _videoPage?.SetActive(contentKind == ShowcaseContentKind.Video);
            UpdateContentScene();
            UpdateSelectionStyles();
            QueueSnapshotRefresh();
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
            QueueSnapshotRefresh();
        }
    }

    private void OnCornerPinClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: string value }
            && Enum.TryParse<PageAnchor>(value, out var anchor))
        {
            _cornerAnchor = anchor;
            CornerPageView.LockedAnchor = anchor;
            PositionCornerMarker(anchor);
            UpdateSelectionStyles();
            StatusLabel.Text = $"{FormatAnchor(anchor)} pinned";
        }
    }

    private async void OnRefreshSnapshotClicked(object? sender, EventArgs e)
    {
        RefreshSnapshotButton.IsEnabled = false;
        StatusLabel.Text = "Capturing";
        try
        {
            await ContentPageView.RefreshSnapshotAsync();
            StatusLabel.Text = "Snapshot refreshed";
        }
        catch (OperationCanceledException)
        {
            StatusLabel.Text = "Capture replaced";
        }
        finally
        {
            RefreshSnapshotButton.IsEnabled = true;
        }
    }

    private void OnSnapshotFailed(object? sender, PageSnapshotFailedEventArgs e)
    {
        if (e.Exception.Message.Contains("laid out", StringComparison.OrdinalIgnoreCase))
        {
            if (!_snapshotRetryQueued)
            {
                _snapshotRetryQueued = true;
                Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(120), () =>
                {
                    _snapshotRetryQueued = false;
                    QueueSnapshotRefresh();
                });
            }

            return;
        }

        StatusLabel.Text = "Using last snapshot";
        DetailLabel.Text = $"Capture unavailable: {e.Exception.Message}";
    }

    private void OnOptionsClicked(object? sender, EventArgs e)
    {
        if (_isCompact)
        {
            MobileOptionsOverlay.IsVisible = !MobileOptionsOverlay.IsVisible;
        }
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

        QueueSnapshotRefresh();
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
            MobileOptionsOverlay.IsVisible = false;
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
        _interactivePage = new InteractivePageView();
        _interactivePage.ContentChanged += OnInteractiveContentChanged;
        _videoPage = new VideoPosterPageView();
    }

    private async void OnInteractiveContentChanged(object? sender, EventArgs e)
    {
        try
        {
            await ContentPageView.RefreshSnapshotAsync();
            StatusLabel.Text = "Live content updated";
        }
        catch (OperationCanceledException)
        {
        }
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
            case ShowcaseScene.CornerPin:
                SetImagePage(CornerPageView, _navigation[_scene]);
                PositionCornerMarker(_cornerAnchor);
                break;
        }

        UpdateCounter();
        UpdateNavigationButtons();
        QueueSnapshotRefresh();
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
        var right = _spreadNavigation.RightPageIndex;
        SpreadPageView.LeftContent = CreatePageView(left);
        SpreadPageView.RightContent = right.HasValue ? CreatePageView(right.Value) : null;
        SpreadPageView.PreviousLeftContent = _spreadNavigation.CanTurnBackward
            ? CreatePageView(Math.Max(left - 2, 0))
            : null;
        SpreadPageView.NextRightContent = _spreadNavigation.CanTurnForward
            ? CreatePageView(Math.Min((right ?? left) + 2, _pageSet!.Pages.Count - 1))
            : null;
    }

    private void UpdateContentScene()
    {
        var navigation = _navigation[ShowcaseScene.Content];
        ContentPageView.FrontContent = _contentKind switch
        {
            ShowcaseContentKind.Interactive => _interactivePage,
            ShowcaseContentKind.Video => _videoPage,
            _ => new Image
            {
                Source = _pageSet!.GetImageSource(navigation.PageIndex),
                Aspect = Aspect.AspectFill,
                BackgroundColor = Color.FromArgb("#FAF8F1"),
            },
        };
        ContentPageView.UnderPageImage = _pageSet!.Pages[navigation.GetUnderPageIndex(PageTurnDirection.Forward)];
        ContentPageView.PreviousPageImage = _pageSet.Pages[navigation.GetUnderPageIndex(PageTurnDirection.Backward)];
    }

    private void UpdateBacksideScene()
    {
        var navigation = _navigation[ShowcaseScene.Backside];
        BacksidePageView.FrontContent = CreatePageView(navigation.PageIndex);
        BacksidePageView.BackContent = new CustomBacksideView();
        BacksidePageView.BacksideMode = _backsideMode;
        BacksidePageView.BacksideColor = Color.FromArgb("#D5CBAF");
        BacksidePageView.UnderPageImage = _pageSet!.Pages[navigation.GetUnderPageIndex(PageTurnDirection.Forward)];
        BacksidePageView.PreviousPageImage = _pageSet.Pages[navigation.GetUnderPageIndex(PageTurnDirection.Backward)];
    }

    private DemoPageView CreatePageView(int pageIndex)
    {
        var copy = pageIndex switch
        {
            0 => ("THE MATERIAL", "A page should feel certain.", "It follows the hand, keeps its length, and returns without a wrinkle."),
            1 => ("EIGHT ANCHORS", "Turn from anywhere.", "Lock any edge or pin any corner."),
            2 => ("NAVIGATION", "One book. Two directions.", "The next or previous page waits underneath."),
            3 => ("MAUI VIEWS", "Content stays content.", "Controls remain live until the curl begins."),
            4 => ("PAGE BACKSIDE", "The back is yours.", "Mirror, color, or provide another view."),
            5 => ("FLICK + THRESHOLD", "Fast hands welcome.", "Intent and distance work together."),
            6 => ("HIGH RESOLUTION", "Pixels under pressure.", "Inspect the seam and shadow up close."),
            _ => ("PAGECURL.MAUI", "Build something tactile.", "The geometry is tested. The surface is yours."),
        };
        return new DemoPageView(pageIndex + 1, copy.Item1, copy.Item2, copy.Item3);
    }

    private async void QueueSnapshotRefresh()
    {
        if (!IsLoaded)
        {
            return;
        }

        await Task.Yield();
        try
        {
            switch (_scene)
            {
                case ShowcaseScene.Spread:
                    await SpreadPageView.RefreshSnapshotAsync();
                    break;
                case ShowcaseScene.Content:
                    await ContentPageView.RefreshSnapshotAsync();
                    break;
                case ShowcaseScene.Backside:
                    await BacksidePageView.RefreshSnapshotAsync();
                    break;
            }
        }
        catch (OperationCanceledException)
        {
        }

        if (StatusLabel.Text is "Using last snapshot" or "Capturing")
        {
            StatusLabel.Text = "Ready";
        }

        UpdateNavigationButtons();
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
        SetSceneButtonStyle(CornerSceneButton, MobileCornerSceneButton, ShowcaseScene.CornerPin, normalScene, selectedScene);

        var segment = (Style)Application.Current.Resources["SegmentButton"];
        var selected = (Style)Application.Current.Resources["CobaltButton"];
        ImageContentButton.Style = _contentKind == ShowcaseContentKind.Image ? selected : segment;
        InteractiveContentButton.Style = _contentKind == ShowcaseContentKind.Interactive ? selected : segment;
        VideoContentButton.Style = _contentKind == ShowcaseContentKind.Video ? selected : segment;
        MirrorBackButton.Style = _backsideMode == PageBacksideMode.MirroredFront ? selected : segment;
        ColorBackButton.Style = _backsideMode == PageBacksideMode.SolidColor ? selected : segment;
        CustomBackButton.Style = _backsideMode == PageBacksideMode.CustomContent ? selected : segment;
        TopLeftPinButton.Style = _cornerAnchor == PageAnchor.TopLeft ? selected : segment;
        TopRightPinButton.Style = _cornerAnchor == PageAnchor.TopRight ? selected : segment;
        BottomLeftPinButton.Style = _cornerAnchor == PageAnchor.BottomLeft ? selected : segment;
        BottomRightPinButton.Style = _cornerAnchor == PageAnchor.BottomRight ? selected : segment;
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

    private void PositionCornerMarker(PageAnchor anchor)
    {
        CornerPinMarker.HorizontalOptions = anchor is PageAnchor.TopRight or PageAnchor.BottomRight
            ? LayoutOptions.End
            : LayoutOptions.Start;
        CornerPinMarker.VerticalOptions = anchor is PageAnchor.BottomLeft or PageAnchor.BottomRight
            ? LayoutOptions.End
            : LayoutOptions.Start;
        CornerPinMarker.Margin = new Thickness(18);
    }

    private void CancelAllInteractions()
    {
        SinglePageView.CancelInteraction();
        ContentPageView.CancelInteraction();
        BacksidePageView.CancelInteraction();
        CornerPageView.CancelInteraction();
        SpreadPageView.Overlay.CancelInteraction();
    }

    private void ClearAllContent()
    {
        foreach (var view in new[] { SinglePageView, CornerPageView })
        {
            view.FrontImage = null;
            view.BackImage = null;
            view.UnderPageImage = null;
            view.PreviousPageImage = null;
        }

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
