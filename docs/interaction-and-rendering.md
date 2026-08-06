# Interaction and rendering

## Gesture contract

`PageTurnView` accepts drags from every free edge and corner. `LockedEdge`
selects the stationary hinge; points on that edge, including its corners, do
not start a drag. `Auto` resolves a corner from the first meaningful inward
movement. One pointer owns the gesture until release or cancellation.

The page follows the pointer directly during a drag. Releasing before or after
`SnapThreshold` settles to the corresponding endpoint. A directional flick
can override that threshold. The controller limits only the velocity carried
into settling, so a fast gesture still leaves enough visible motion to read the
outgoing sheet. Diagonal rollback preserves its release trajectory and does
not rotate into a second edge snap.

`TryTurn` uses the same completion path as gestures. `ReduceMotion` skips
settling but preserves page-model completion and event semantics.

## Paper model

The current page is a direct, developable cylinder mapping rather than a cloth
simulation. It preserves material distance by construction, responds in the
same frame as the pointer, and has no cross-frame particle state. The cylinder
axis follows the pull direction. Its radius tightens enough to reach the
pointer but never below six percent of the shorter page dimension, which gives
the sheet a stiff-paper curl instead of a sharp crease.

The selected hinge stays stationary. When a drag exceeds the reachable curl,
the mapping remains bounded instead of stretching the page. Near the turn
endpoints, the preferred radius relaxes toward a flat sheet.

The default mesh is 48 by 48 cells. Front/back transitions do not snap to that
grid: triangles crossing the cylinder tangent are split at the analytic tangent
and share an exact straight seam. The two streams are depth-sorted without a
z-buffer and drawn so the nearer turned underside can cover the front during a
deep curl.

Per-vertex shade provides restrained curvature light. Physically complete
self-shadowing, perspective, collision, and multi-sheet contact are outside the
current renderer. Add them only after representative device profiling.
