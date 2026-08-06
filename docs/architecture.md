# Architecture

PageCurl keeps interaction, geometry, rendering, and presentation separate.

- `PageCurl.Core` targets `net10.0`. `PageTurnController` owns gestures and
  settling. `PageCurlGeometry` fills reusable mesh buffers. `PageNavigationState`
  provides optional page-index rules for samples and consumers.
- `PageCurl.Maui` adapts the core to MAUI with `PageTurnView`, a SkiaSharp
  `SKGLView`. It owns touch capture, shaders, fixed render buffers, invalidation,
  and the 60 Hz settle timer.
- `PageCurl.Demo` owns generated sample images and shows gesture, command,
  reduced-motion, edge-lock, and diagnostics behavior.
- `PageCurl.Core.Tests` is a dependency-free executable test runner.

On each active frame, the view maps the pointer into the aspect-fitted page
viewport. The controller produces a curl pose. The geometry layer maps the
flat sheet onto a rotated developable cylinder, clips triangles at the exact
front/back tangent, and sorts each stream by depth. The renderer draws the
destination page, then the outgoing page front and turned underside.

The core contains no MAUI, SkiaSharp, dispatcher, or device dependency. A
future renderer can consume the same pose and mesh without changing gesture or
navigation semantics.

Mesh, triangle, Skia point, UV, and color arrays are allocated at construction.
The idle view does not run an animation loop. The active path uses bounded
geometry and an allocation-free in-place sort.
