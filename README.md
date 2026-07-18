# PageCurl.Maui

`PageCurl.Maui` is a small .NET MAUI control for dragging an image like a
book page. It separates interaction, geometry and drawing so the math can be
tested without a device or UI toolkit:

* `src/PageCurl.Core` targets `net10.0` and contains the allocation-free drag
  state machine, spring settling, edge-aware hinge selection, aspect-fit
  viewport math, and cylinder page mesh.
* `src/PageCurl.Maui` exposes `PageTurnView`, a GPU-backed `SKGLView` that
  draws the mesh with SkiaSharp texture coordinates and per-vertex shading. It
  supports `LockedEdge` (`Auto`, `Top`, `Right`, `Bottom`, or `Left`) and
  `ContentScale` (`Fill` or `AspectFit`), plus a neutral `net10.0` target for
  source-level CI builds.
* `src/PageCurl.Demo` is a MAUI sample with four generated pages, including a
  large-page case and non-gesture page-turn commands.
* `tests/PageCurl.Core.Tests` is a dependency-free executable test runner. It
  does not require an internet package feed or a test adapter.

## Try the demo

The repository pins .NET SDK 10.0.301 in [`global.json`](global.json). On a
macOS development machine, install the mobile workloads and run the iOS smoke
workflow:

```bash
dotnet workload install maui-ios maui-android
./tools/ios-smoke.sh
```

To build only Android, use the explicit target:

```bash
dotnet build src/PageCurl.Demo/PageCurl.Demo.csproj -c Release -f net10.0-android
```

The control pins `SkiaSharp.Views.Maui.Controls` 4.150.1, the current stable
SkiaSharp.Views release with `net10.0-*` assets and MAUI 10 dependencies.
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

### Demo testing mode

Enable `Testing mode` in the sample app when validating a gesture or tuning the
renderer. The opt-in panel reports the controller state, pointer/start
coordinates, curl pose, completion result, and opt-in render timing/allocation
metrics. It is demo-only, disabled by default, and does not add polling or
diagnostic work to normal library usage.

## Use the control

The control accepts `SKImage` instances. The caller owns those images and can
reuse or dispose them when the view is no longer using them.

```xml
<curl:PageTurnView
    x:Name="TurnView"
    ContentScale="AspectFit"
    LockedEdge="Auto" />
```

```csharp
TurnView.FrontImage = currentPage;
TurnView.BackImage = currentPage; // optional backside texture
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
Set `PreviousPageImage` when backward turns should reveal a different page;
when it is not set, the control falls back to `UnderPageImage` for compatibility.

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

## Performance choices

The view uses `SKGLView` and invalidates only while a finger is moving or a
release is settling. Mesh arrays, Skia point arrays and color arrays are
allocated once. Image shaders are cached per `SKImage`; the settle animation
uses a critically damped spring and a single 60 Hz dispatcher timer. The
default mesh is 24 columns by 12 rows (576 triangles); increase it only if a
larger page or closer inspection warrants the extra work.

The geometry is a bounded analytic cylinder approximation. It has no z-buffer
dependency, so front and back triangle streams are depth-sorted and drawn in
two passes. The same approach works on Android OpenGL ES, iOS, Mac Catalyst
and Windows through the SkiaSharp MAUI handlers.

## Verification

Run the platform-independent checks with:

```bash
dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj
dotnet build src/PageCurl.Maui/PageCurl.Maui.csproj -c Release -f net10.0 -p:TargetFrameworks=net10.0
```

The checks cover edge selection and clamping, normalized progress in both
directions, threshold snapping, cancellation, completion events, finite and
bounded mesh coordinates, UV/shading ranges, mesh-buffer reuse, immediate and
programmatic turns, every physical edge and corner, locked-hinge anchoring,
aspect-fit viewport math, direction-aware demo navigation, and warmed
1,000-iteration core allocation loops. The MAUI control exposes opt-in
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

## Project knowledge

The [`knowledge/`](knowledge/index.md) directory is an Open Knowledge Format
(OKF) bundle for this project. It captures the goal, architecture, rendering
trade-offs, workarounds, testing strategy, release gates, and roadmap as linked
Markdown concepts so the reasoning remains easy to inspect and update with the
code.
