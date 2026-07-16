# PageCurl.Maui

`PageCurl.Maui` is a small .NET MAUI control for dragging an image like a
book page. It separates interaction, geometry and drawing so the math can be
tested without a device or UI toolkit:

* `src/PageCurl.Core` targets `net10.0` and contains the allocation-free drag
  state machine, spring settling and cylinder page mesh.
* `src/PageCurl.Maui` exposes `PageTurnView`, a GPU-backed `SKGLView` that
  draws the mesh with SkiaSharp texture coordinates and per-vertex shading.
  It also exposes a neutral `net10.0` target for source-level CI builds.
* `src/PageCurl.Demo` is a MAUI sample with three generated pages.
* `tests/PageCurl.Core.Tests` is a dependency-free executable test runner. It
  does not require an internet package feed or a test adapter.

## Try the demo

Install the .NET 10 SDK and the matching MAUI workload for the target you want,
then (on Linux):

```bash
dotnet workload install maui-android
dotnet build src/PageCurl.Demo/PageCurl.Demo.csproj -f net10.0-android
```

On macOS or Windows, install the full `maui` workload when building the
additional Apple or Windows targets.

The control pins `SkiaSharp.Views.Maui.Controls` 4.150.1, the current stable
SkiaSharp.Views release with `net10.0-*` assets and MAUI 10 dependencies.
Keep the package and MAUI workload on the same .NET major line when upgrading.

The project selects Android automatically on Linux. iOS and Mac Catalyst
targets are added on macOS, and the Windows target is added on Windows. This
keeps a Linux restore/build from evaluating native workloads that cannot run on
that host.

The demo supports Android, iOS, Mac Catalyst and Windows. The minimum
requirement in the question (Android and iOS) is covered by the first two
target frameworks; the other targets are included because the core and Skia
control have no platform-specific code.

## Use the control

The control accepts `SKImage` instances. The caller owns those images and can
reuse or dispose them when the view is no longer using them.

```xml
<curl:PageTurnView x:Name="TurnView" />
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
Set `PreviousPageImage` when backward turns should reveal a different page;
when it is not set, the control falls back to `UnderPageImage` for compatibility.

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
bounded mesh coordinates, UV/shading ranges, and mesh-buffer reuse.

The platform build must be run on a host with the matching .NET 10 MAUI
workload and native SDKs. On Linux, validate the Android target explicitly with
`-f net10.0-android`; do not ask Linux to evaluate the iOS, Mac Catalyst or
Windows targets. The .NET 10 Android toolchain defaults to API 36, so install
the API 36 platform and an appropriate JDK alongside the workload.

## Project knowledge

The [`knowledge/`](knowledge/index.md) directory is an Open Knowledge Format
(OKF) bundle for this project. It captures the goal, architecture, rendering
trade-offs, workarounds, testing strategy, release gates, and roadmap as linked
Markdown concepts so the reasoning remains easy to inspect and update with the
code.
