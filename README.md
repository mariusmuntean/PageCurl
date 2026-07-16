# PageCurl.Maui

`PageCurl.Maui` is a small .NET MAUI control for dragging an image like a
book page. It separates interaction, geometry and drawing so the math can be
tested without a device or UI toolkit:

* `src/PageCurl.Core` targets `net8.0` and contains the allocation-free drag
  state machine, spring settling and cylinder page mesh.
* `src/PageCurl.Maui` exposes `PageTurnView`, a GPU-backed `SKGLView` that
  draws the mesh with SkiaSharp texture coordinates and per-vertex shading.
* `src/PageCurl.Demo` is a MAUI sample with three generated pages.
* `tests/PageCurl.Core.Tests` is a dependency-free executable test runner. It
  does not require an internet package feed or a test adapter.

## Try the demo

Install the .NET 8 SDK and the MAUI workloads for the target you want, then:

```bash
dotnet workload restore PageCurl.sln
dotnet build src/PageCurl.Demo/PageCurl.Demo.csproj -f net8.0-android
```

The control pins `SkiaSharp.Views.Maui.Controls` 3.116.1, the net8-compatible
SkiaSharp.Views release. Move the pin forward together with the MAUI workload
when upgrading the application to a newer .NET line.

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
```

The checks cover edge selection and clamping, normalized progress in both
directions, threshold snapping, cancellation, completion events, finite and
bounded mesh coordinates, UV/shading ranges, and mesh-buffer reuse.

The repository runner used to prepare this sample did not have MAUI workloads
or access to NuGet, so it could execute the core build and test runner but not
the platform project. On a developer machine, `dotnet workload restore` and a
normal NuGet restore provide the remaining MAUI/SkiaSharp build verification.

## Project knowledge

The [`knowledge/`](knowledge/index.md) directory is an Open Knowledge Format
(OKF) bundle for this project. It captures the goal, architecture, rendering
trade-offs, workarounds, testing strategy, release gates, and roadmap as linked
Markdown concepts so the reasoning remains easy to inspect and update with the
code.
