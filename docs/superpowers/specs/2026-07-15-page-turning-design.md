# Page Turning Control Design

**Status:** Historical design record. The implemented architecture is documented
in `knowledge/project/architecture.md`.

## Goal

Provide a .NET MAUI control that lets a user grab a page image, drag it with precise pointer tracking, see a continuously deforming page curl, and settle it to the nearest endpoint when released. The first supported mobile targets are Android and iOS; the pure geometry/state library must remain usable on any .NET platform.

## Architecture

The solution is split into three projects:

1. `PageCurl.Core` targets `net10.0` and contains all interaction state, curl-pose calculation, mesh generation and deterministic tests. It has no MAUI or SkiaSharp dependency.
2. `PageCurl.Maui` targets MAUI mobile/desktop platforms and exposes `PageTurnView`, backed by SkiaSharp's GPU view. It owns image shaders, touch event translation, invalidation and settle timing.
3. `PageCurl.Demo` is a MAUI single-project sample that creates front, back and under-page images and consumes the control through XAML/code-behind.

The renderer boundary is intentionally narrow. The core returns reusable front/back mesh buffers containing screen positions, normalized texture coordinates and lighting factors. The MAUI adapter converts these into Skia vertices. A future `SKMesh`, Metal or WebGL renderer can replace this adapter without changing interaction semantics.

## Curl model

The first implementation uses a cylinder-like analytic curl inspired by the Apache-2.0 `android-pagecurl` algorithm:

- A drag begins at the nearest horizontal page edge.
- The pointer and drag-start positions determine the curl direction.
- The radius is reduced near the opposite edge so the page can complete a half-turn.
- A regular page grid is transformed into flat, curled and fully rotated regions.
- Triangles are depth-ordered before being written to reusable front/back buffers.

This is deliberately not a mass-spring cloth simulation. The cylinder is stable, cheap and easier to constrain so the page remains attached to the user's gesture.

## Public core API

`PageTurnController` exposes `BeginDrag`, `UpdateDrag`, `EndDrag`, `Cancel`, `Tick` and a read-only `PageTurnSnapshot`. It reports `PageTurnCompletedEventArgs` when settling finishes.

`PageCurlGeometry.Build` fills a caller-owned `PageMeshBuffer` from a `CurlPose`. `PageCurlOptions` controls mesh resolution, radius and spring settings.

## Public MAUI API

`PageTurnView` exposes:

- `FrontImage` — the turning page front texture.
- `BackImage` — optional back texture; the front texture is reused when absent.
- `UnderPageImage` — optional page visible underneath the curl.
- `Options` — mesh and animation configuration.
- `PageTurnCompleted` — raised after a committed or cancelled settle.

The view accepts touch input directly and invalidates only while a gesture or settle is active. It does not send per-frame messages to managed application code.

## Testing and acceptance

Core tests must verify:

- Direction selection and edge clamping.
- Progress tracking for forward and backward drags.
- Nearest-endpoint selection and eventual settle completion.
- Finite, bounded mesh positions and normalized texture coordinates.
- Both front and back regions appearing during a mid-curl.
- Repeated builds reuse the same mesh arrays.

The supported-host acceptance target is a 60 FPS floor during drag/settle with no image decode or texture upload on the interaction path. Device profiling is required for release; this repository can verify the deterministic core and compile the MAUI project where the required workload is installed.
