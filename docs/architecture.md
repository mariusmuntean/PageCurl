# Architecture

PageCurl keeps interaction, geometry, rendering, and presentation separate.

- `PageCurl.Core` targets `net10.0`. `PageTurnController` owns gestures and
  settling. `PageCurlGeometry` fills reusable mesh buffers. `PageNavigationState`
  provides optional page-index rules for samples and consumers.
- `PageCurl.Maui` adapts the core to MAUI with `PageTurnView`, a SkiaSharp
  `SKGLView`. It owns touch capture, shaders, fixed render buffers, invalidation,
  and the 60 Hz settle timer. `PageCurlView` adds live MAUI content and owned
  snapshots. `PageSpreadView` places one full-width renderer over two live
  pages and a gutter.
- `PageCurl.Demo` owns generated sample images and shows gesture, command,
  reduced-motion, edge-lock, and diagnostics behavior.
- `PageCurl.Core.Tests` is a dependency-free executable test runner.
- `PageCurl.Maui.Tests` runs wrapper and snapshot lifecycle checks against the
  neutral MAUI target.

On each active frame, the view maps the pointer into the aspect-fitted page
viewport. The controller produces a curl pose. The geometry layer maps the
flat sheet onto a rotated developable cylinder for edge anchors or an analytic
cone for pinned corners. It clips cylinder triangles at the exact front/back
tangent, classifies cone triangles by surface orientation, and sorts each
stream by depth. The renderer draws the destination page, then the outgoing
page front and turned underside.

The core contains no MAUI, SkiaSharp, dispatcher, or device dependency. A
future renderer can consume the same pose and mesh without changing gesture or
navigation semantics.

Mesh, triangle, Skia point, UV, and color arrays are allocated at construction.
The idle view does not run an animation loop. The active path uses bounded
geometry and an allocation-free in-place sort.

`PageCurlView` lays out one page with the same width/height ratio used by the
renderer. Eight narrow Skia touch zones cover its edges and corners; the center
remains available to child controls. Native snapshotters capture Android,
Apple, and Windows views after layout. The wrapper clears renderer references
before disposing owned snapshots on handler teardown.

Live wrappers start turns only with current required snapshots. Explicit
refreshes wait for native layout and active turns; layout waits are cancellable
and end when the presentation is hidden or disconnected. Automatic refreshes
coalesce layout changes without cancelling an explicit request. Optional sheet
backs use the configured fallback when their current capture is unavailable.

`PageSpreadView` lays out two equal page cells. Its renderer spans both cells,
then aligns one page mesh to the left or right half. Forward turns hide the
right live page and reveal the next-right destination; backward turns do the
same on the left. One subtle shadow marks the gutter below the moving sheet.
