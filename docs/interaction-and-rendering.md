# Interaction and rendering

## Gesture contract

`PageTurnView` accepts drags from every free edge and corner. `LockedEdge`
selects the stationary hinge; points on that edge, including its corners, do
not start a drag. `Auto` resolves a corner from the first meaningful inward
movement. One pointer owns the gesture until release or cancellation.

The page follows the pointer directly during a drag. Releasing before or after
`PageChangeThreshold` settles to the corresponding endpoint. The value is a
page-travel fraction from `0` to `1`; `SnapThreshold` remains an alias. A
directional flick can override the threshold when `FlickEnabled` is true.
`FlickVelocityThreshold` controls the required normalized progress velocity.
The controller caps only the velocity carried into settling, so a fast gesture
still leaves visible motion. Diagonal rollback keeps its release trajectory.

`PageChangeThresholdCrossed` fires when a drag enters or leaves commit intent.
Repeated samples on one side do not repeat the event. The event carries the
turn direction, normalized progress, and current commit-candidate state.

`TryTurn` uses the same completion path as gestures. `ReduceMotion` skips
settling but preserves page-model completion and event semantics.

The platform-free controller also accepts `PageAnchor`. Edge values match
`PageEdge`; corner values pin one point. The exact pin cannot start a drag, but
the remaining page boundary can. Controller snapshots and curl poses retain
the selected anchor.

## Paper model

Edge anchors use a direct, developable cylinder. Corner anchors use an analytic
developable cone centered on the pin. Both preserve sheet distance, respond in
the pointer frame, and keep no particle state. The cylinder axis follows the
pull direction. Its radius never drops below six percent of the shorter page
dimension, which keeps the curl round instead of forming a crease.

The selected hinge stays stationary. When a drag exceeds the reachable curl,
the mapping remains bounded instead of stretching the page. Near the turn
endpoints, the preferred radius relaxes toward a flat sheet.

The default mesh is 48 by 48 cells. Front/back transitions do not snap to that
grid: triangles crossing a cylinder tangent split at the analytic tangent and
share one straight seam. Cone triangles follow the surface orientation and use
pin-aligned diagonals. The renderer depth-sorts both streams without a z-buffer,
so the nearer underside covers the front during a deep curl.

`PageAspectLayout.Fit` uses width divided by height. Pass a positive explicit
ratio, `0` for intrinsic content size, or let the `0.75` fallback apply when no
intrinsic size exists.

Per-vertex shade provides restrained curvature light. Physically complete
self-shadowing, perspective, collision, and multi-sheet contact are outside the
current renderer. Add them only after representative device profiling.
