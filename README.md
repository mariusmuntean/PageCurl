# PageCurl.Maui

`PageCurl.Maui` provides interactive page curls for images, live MAUI content,
and two-page spreads. It separates interaction, geometry and drawing so the math can be
tested without a device or UI toolkit:

* `src/PageCurl.Core` targets `net10.0` and contains the allocation-free drag
  state machine, spring settling, edge or corner anchors, aspect-fit viewport
  math, and developable cylinder/cone page mesh.
* `src/PageCurl.Maui` exposes the low-level `PageTurnView`, snapshot-backed
  `PageCurlView`, and two-page `PageSpreadView`. The Skia renderer draws fixed
  mesh buffers with per-vertex shading. A neutral `net10.0` target supports
  source and lifecycle tests without a device.
* `src/PageCurl.Demo` is a responsive four-scene showcase with eight generated
  pages, one- and two-page navigation, live MAUI content, licensed image and
  video assets, backside choices, every edge/corner anchor, haptics, and
  non-gesture commands.
* `tests/PageCurl.Core.Tests` is a dependency-free executable test runner. It
  does not require an internet package feed or a test adapter.
* `tests/PageCurl.Maui.Tests` covers snapshot replacement, content wrappers,
  and spread readiness on the neutral MAUI target.

## Try the demo

The repository pins .NET SDK 10.0.401 in [`global.json`](global.json). On a
macOS development machine, install the mobile workloads and run the iOS smoke
workflow:

```bash
dotnet workload install maui-ios maui-maccatalyst maui-android
./tools/ios-smoke.sh
```

To build only Android, use the explicit target:

```bash
dotnet build src/PageCurl.Demo/PageCurl.Demo.csproj -c Release -f net10.0-android
```

The control pins `SkiaSharp.Views.Maui.Controls` 4.150.1 with `net10.0-*`
assets and MAUI 10 dependencies.
Keep the package and MAUI workload on the same .NET major line when upgrading.

The project selects Android automatically on Linux. iOS and Mac Catalyst
targets are added on macOS, and the Windows target is added on Windows. This
keeps a Linux restore/build from evaluating native workloads that cannot run on
that host.

For fast visual iteration on macOS, build the desktop target and resize the
window freely while tuning the responsive stage:

```bash
dotnet build src/PageCurl.Demo/PageCurl.Demo.csproj -c Debug -f net10.0-maccatalyst
open src/PageCurl.Demo/bin/Debug/net10.0-maccatalyst/maccatalyst-arm64/PageCurl.Demo.app
```

The demo supports Android, iOS, Mac Catalyst and Windows. See the
[compatibility matrix](docs/compatibility.md) for the current evidence status.

### Run on a physical iPhone

The demo uses the device bundle identifier `de.marius.PageCurlDemo`. With a
matching Apple Development profile installed, select the paired iPhone as the
run target in Rider and run `PageCurl.Demo`. Developers using another Apple
team should replace `ApplicationId` in
`src/PageCurl.Demo/PageCurl.Demo.csproj` with an identifier owned by that team
and let Xcode create or download the matching development profile.

### Showcase scenes

The sample has four scenes: Single, Spread, Content, and Backside. Content opens
with a real image and can switch to a live `ContentView` or a bundled Big Buck
Bunny excerpt. Backside opens with the adjacent numbered page on its reverse and can
switch to mirrored or solid-color modes. Shared options expose every edge and
corner anchor, a percentage threshold, flicks, page ratios, reduced motion, and
mobile haptics. Desktop uses a scene rail and inspector; compact layouts move
those controls into a scene strip and options sheet. Third-party media details
are in [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md).

## Use the control

Reference `src/PageCurl.Maui` from your MAUI app, or consume a locally packed
package. Register Skia's handlers in `MauiProgram`:

```csharp
using SkiaSharp.Views.Maui.Controls.Hosting;

builder.UseMauiApp<App>().UseSkiaSharp();
```

Declare `xmlns:curl="clr-namespace:PageCurl.Maui;assembly=PageCurl.Maui"` on
your XAML page. The image control accepts `SKImage` instances. The caller owns those images and can
reuse or dispose them when the view is no longer using them.

```xml
<curl:PageTurnView
    x:Name="TurnView"
    ContentScale="AspectFit"
    LockedEdge="Auto" />
```

```csharp
TurnView.FrontImage = currentPage;
TurnView.BacksideMode = PageBacksideMode.CustomContent;
TurnView.BackImage = reverseSide; // optional caller-supplied backside
TurnView.UnderPageImage = nextPage;
TurnView.PreviousPageImage = previousPage; // optional page for backward turns
TurnView.PageTurnCompleted += (_, e) =>
{
    if (e.Committed)
    {
        // Advance or rewind the page model, then replace FrontImage.
    }
};
```

`PageTurnController` and `PageCurlGeometry` are public as well, so an app can
use the tested interaction/mesh layer with another renderer if required.
`Auto` selects the nearest free edge; a selected `LockedEdge` is the stationary
hinge, while the remaining edges and corners can start a gesture. A corner in
`Auto` mode resolves from the first meaningful inward drag vector. The legacy
two-argument `BeginDrag` overload remains the horizontal left/right behavior
for existing callers. `AspectFit` keeps source images undistorted and maps
touch coordinates into the same fitted content rectangle used by the mesh.
Curl progress is measured from the grab point, so a drag that starts in the
middle of a page begins flat and curls only while the pointer moves. During a
drag, the renderer keeps the grabbed point attached to the pointer until the
cylinder reaches its physical curl limit; beyond that limit the pose stays
bounded instead of stretching. The default material is a stiff sheet: it
curls without cloth-like lag or permanent fold state.
Set `PreviousPageImage` when backward turns should reveal a different page;
when it is not set, the control falls back to `UnderPageImage` for compatibility.

The core API accepts any stationary edge or pinned corner. It also exposes
normalized release intent and flick settings:

```csharp
var options = new PageCurlOptions
{
    PageChangeThreshold = 0.42f,
    FlickEnabled = true,
    FlickVelocityThreshold = 1.5f,
};
var controller = new PageTurnController(options);
controller.PageChangeThresholdCrossed += (_, e) =>
    UpdateCommitHint(e.IsCommitCandidate);

controller.BeginDrag(pointer, pageSize, PageAnchor.TopLeft);
```

An edge anchor keeps the current cylinder mapping. A corner anchor keeps that
point fixed and maps the sheet onto a developable cone. `SnapThreshold` remains
an alias for `PageChangeThreshold`. `PageAspectLayout.Fit` resolves an explicit
width/height ratio, intrinsic content size, or the `0.75` fallback.

For users who need an alternate to gesture input, call `TryTurn` from a labeled
button or command. `ReduceMotion` maps both gesture releases and programmatic
turns to immediate endpoint completion:

```csharp
TurnView.ReduceMotion = true;
if (TurnView.TryTurn(PageTurnDirection.Forward))
{
    // Commit the page model from PageTurnCompleted, as with a gesture turn.
}
```

Set image properties and invoke commands on the UI thread. `PageTurnView` does
not dispose caller-owned `SKImage` values; keep each image alive until it is no
longer assigned, clear the properties before disposing a recycled image set,
and call `CancelInteraction` when removing or recycling the view.

### Use MAUI content

`PageCurlView` keeps your MAUI view live while the page is flat. It captures a
texture after layout and swaps to that texture only during a curl.

```xml
<curl:PageCurlView
    x:Name="ArticlePage"
    LockedAnchor="TopLeft"
    PageAspectRatio="0.72"
    PageChangeThreshold="0.42"
    BacksideMode="CustomContent">
    <curl:PageCurlView.FrontContent>
        <local:ArticleContentView />
    </curl:PageCurlView.FrontContent>
    <curl:PageCurlView.BackContent>
        <local:ArticleNotesView />
    </curl:PageCurlView.BackContent>
</curl:PageCurlView>
```

Call `RefreshSnapshotAsync()` after dynamic content changes. Capture failures
raise `SnapshotFailed` and leave the last good texture in place. A live video
can remain in `FrontContent`; the curl uses its latest captured frame. Supply a
poster view when the platform video layer does not appear in native snapshots.
The demo keeps the media dependency outside the library and plays a bundled
local clip on Android, iOS, and Mac Catalyst. Its still-image poster remains
under the native video surface as a useful fallback and snapshot source.

### Use a two-page spread

```xml
<curl:PageSpreadView
    x:Name="Book"
    PageAspectRatio="0.72"
    LeftContent="{Binding LeftPage}"
    RightContent="{Binding RightPage}"
    PreviousLeftContent="{Binding PreviousLeftPage}"
    NextRightContent="{Binding NextRightPage}"
    PreviousRightContent="{Binding PreviousRightPage}"
    NextLeftContent="{Binding NextLeftPage}" />
```

`TryTurnForward()` curls the right sheet across the gutter;
`TryTurnBackward()` curls the left. `CanTurnForward` and `CanTurnBackward`
remain false until both the current and destination snapshots are ready.
Update the content properties from the shared `PageTurnCompleted` event.
Call `RefreshSnapshotAsync()` to capture in-place edits; automatic property
refreshes recapture only dirty slots. Explicit requests made during a turn wait
until it ends and preserve cancellation.
The turn reads like a physical sheet: `NextRightContent` is revealed
underneath a forward turn, and the sheet's back shows `NextLeftContent`,
the page it lands on (mirrored by `PreviousLeftContent`/`PreviousRightContent`
for backward turns). Omit the back-face properties to keep a mirrored or
solid backside.

## Performance choices

The view uses `SKGLView` and invalidates only while a finger is moving or a
release is settling. Mesh arrays, Skia point arrays and color arrays are
allocated once. Image shaders are cached per `SKImage`; the settle animation
uses a critically damped spring and a single 60 Hz dispatcher timer. The
default mesh is 64 columns by 64 rows. Crossing triangles are clipped at the
analytic front/back tangent, so the visible seam does not stair-step along the
mesh grid. Underside lighting has a continuous ambient floor, avoiding dark
faceted bands while leaving backside dimming under application control.

Edge curls use a bounded, developable cylinder whose preferred radius relaxes
near the endpoints and tightens around the midpoint. Pinned corners use an
analytic cone. Neither path needs a z-buffer: the outgoing front and turned
underside are depth-sorted and drawn in two passes. The same approach works on
Android, iOS, Mac Catalyst, and Windows through the SkiaSharp MAUI handlers.

## Verification

Start with the [requirements and acceptance criteria](docs/requirements.md) to
see which behaviors each test layer proves and which release gates remain open.

Run the platform-independent checks with:

```bash
MSBuildEnableWorkloadResolver=false dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj -c Release
MSBuildEnableWorkloadResolver=false dotnet run --project tests/PageCurl.Maui.Tests/PageCurl.Maui.Tests.csproj -c Release -f net10.0 -p:TargetFrameworks=net10.0
dotnet build src/PageCurl.Maui/PageCurl.Maui.csproj -c Release -f net10.0 -p:TargetFrameworks=net10.0
```

The checks cover edge selection and clamping, normalized progress in both
directions, custom thresholds and flicks, threshold transitions, cancellation,
completion events, finite and bounded mesh coordinates, UV/shading ranges,
mesh-buffer reuse, immediate and programmatic turns, semantic haptic cue
mapping, every physical edge and pinned corner, locked anchoring,
aspect-fit viewport math, direction-aware navigation state, and warmed
1,000-iteration core allocation loops. Neutral MAUI checks cover platform
capture fallback, snapshot cancellation/replacement, wrapper property
forwarding, six-slot spread capture, and readiness state. The MAUI control exposes opt-in
`PageTurnPerformanceSnapshot` values for interactive render diagnostics; the
default render path does not collect those metrics.

The platform build must be run on a host with the matching .NET 10 MAUI
workload and native SDKs. On Linux, validate the Android target explicitly with
`-f net10.0-android`; do not ask Linux to evaluate the iOS, Mac Catalyst or
Windows targets. The .NET 10 Android toolchain defaults to API 36, so install
the API 36 platform and an appropriate JDK alongside the workload.

For the full iOS validation record, including simulator screenshots and
physical-device limitations, see
[`docs/validation/ios-release-readiness.md`](docs/validation/ios-release-readiness.md).

The project is released under the [MIT License](LICENSE) with the policy in
[`SUPPORT.md`](SUPPORT.md).

## Documentation

The current architecture, rendering contract, lifecycle rules, platform notes,
testing strategy, and release gates live under [`docs/`](docs/index.md).
