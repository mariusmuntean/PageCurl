# Interaction and rendering

## Gesture contract

`PageTurnView` accepts drags from every free edge and corner. `LockedEdge`
selects the stationary hinge; points on that edge, including its corners, do
not start a drag. `Auto` resolves a corner from the first meaningful inward
movement. One pointer owns the gesture until release or cancellation.

The page follows the pointer directly during a drag. Curl progress is
measured from the grab point, so a drag that starts in the middle of a page
begins flat and the sheet curls only while the pointer moves. Releasing
before or after `PageChangeThreshold` settles to the corresponding endpoint.
The value is a page-travel fraction from `0` to `1`; `SnapThreshold` remains
an alias. A directional flick can override the threshold when `FlickEnabled`
is true.
`FlickVelocityThreshold` controls the required normalized progress velocity.
The controller caps only the velocity carried into settling, so a fast gesture
still leaves visible motion. Diagonal rollback keeps its release trajectory.

`PageChangeThresholdCrossed` fires when a drag enters or leaves commit intent.
Repeated samples on one side do not repeat the event. The event carries the
turn direction, normalized progress, and current commit-candidate state.

`TryTurn` uses the same completion path as gestures. `ReduceMotion` skips
settling but preserves page-model completion and event semantics. Pose queries
scale material coordinates from the input page size to the requested render
size, keeping programmatic turns aligned on high-DPI surfaces.

The platform-free controller also accepts `PageAnchor`. Edge values match
`PageEdge`; corner values pin one point. The exact pin cannot start a drag, but
the remaining page boundary can. Controller snapshots and curl poses retain
the selected anchor.

`PageSpreadView` enables `PageCurlOptions.TurnAcrossHinge`: the grab travels
from its starting point to its reflection across the spine, and progress covers
that entire distance. The pointer may leave the starting page. At the default
50% threshold, release chooses the nearer endpoint (unless an enabled flick
overrides it). Diagonal pulls orient the bend; rigid rotation around the spine
provides complete cross-page travel while keeping the attachment stationary.
This option is for edge hinges; pinned-corner cones retain the original travel
contract. Low-level single-page turns keep their original progress convention.

`CancelInteraction()` completes rollback immediately and releases pointer
ownership before a view can be hidden or recycled. Touch cancellation can still
animate. The demo ignores completion events from inactive scenes.

## Paper model

Edge anchors use a direct, developable cylinder. Corner anchors use an analytic
developable cone centered on the pin. Both preserve sheet distance, respond in
the pointer frame, and keep no particle state. The single-page cylinder axis
follows the pull direction. Spread turns combine an angled cylinder with a
rotation around the spine. The bend stays outside the attached edge. The radius
never drops below six percent of the shorter page
dimension, which keeps the curl round instead of forming a crease.

The selected hinge stays stationary. When a drag exceeds the reachable curl,
the mapping remains bounded instead of stretching the page. Near the turn
endpoints, the preferred radius relaxes toward a flat sheet.

The default mesh is 64 by 64 cells. Front/back transitions do not snap to that
grid: triangles crossing a cylinder tangent split at the analytic tangent and
share one straight seam. Cone triangles follow the surface orientation and use
pin-aligned diagonals. The renderer depth-sorts both streams without a z-buffer,
so the nearer underside covers the front during a deep curl. Extra clipping
storage grows with rows plus columns rather than with every cell; only cells
crossed by the straight facing seam need additional vertices.

`PageAspectLayout.Fit` uses width divided by height. Pass a positive explicit
ratio, `0` for intrinsic content size, or let the `0.75` fallback apply when no
intrinsic size exists.

Per-vertex shade provides restrained curvature light. Physically complete
self-shadowing, perspective, collision, and multi-sheet contact are outside the
current renderer. Add them only after representative device profiling.


For spreads, the bend relaxes at each endpoint and a rigid hinge rotation
places the fully turned sheet flat on the opposite page.
The analytic mapping preserves material lengths instead of stretching the mesh
or stopping when the pointer reaches the gutter.

Snapshot-backed controls fit their live content inside a page frame and render
curls across the enclosing stage. The GL surface is not limited to the flat
page rectangle. Custom backs flip texture coordinates on the turning axis so
text is readable when the sheet lands.

Skia vertex copies are explicitly disposed after each draw. Reusable arrays
clear only vertices used by the previous frame, retaining a transparent tail.
The public Skia API still copies whole arrays and allocates a native vertex
wrapper per draw; the core geometry and controller remain allocation-free.

Automatic snapshots wait for content attachment and layout, coalesce changes,
and skip hidden controls. Spread captures yield between pages so input can be
dispatched during preparation. Explicit refresh remains available for changes
that do not affect layout. The demo retains spread content when its page index
has not changed; it does not force another capture on scene selection.
