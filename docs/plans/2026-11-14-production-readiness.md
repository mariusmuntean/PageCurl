# Production-readiness review — plan

Branch: `production-readiness`. Review date: 2026-11-14.
Scope: `PageCurl.Core`, `PageCurl.Maui`, `PageCurl.Demo`, docs.
One commit per finding, in the order below. Status is updated by the orchestrator.

## Order and dependencies

Core first (adapters depend on it), then renderer bugs, then the views built on
the renderer, then the refactor (after bug fixes touch the same files), then
demo, then docs (must describe final behaviour).

| # | Area | Finding | Depends on | Status |
| --- | --- | --- | --- | --- |
| F1 | Core | Turn direction comes from pointer half, not the selected free edge | – | pending |
| F2 | Maui | `PageTurnView` hit-testing and unit inconsistencies | F1 | pending |
| F3 | Maui | Null `Color` values crash painting | F2 (same file) | pending |
| F4 | Maui | Snapshot refresh is not fault tolerant | F3 | pending |
| F5 | Maui | Android snapshot does a PNG round trip | – | pending |
| F6 | Maui | Duplicated helpers across the two live-content views | F2–F4 | pending |
| F7 | Demo | Hard-coded page counts, unguarded `async void` | F4 | pending |
| F8 | Docs | Contradictions and release-note noise | F1–F7 | pending |

## Findings

### F1 — Turn direction vs. free edge (bug)
`PageTurnController.BeginDrag(pointer, pageSize, PageAnchor)` sets `_direction`
from `pointer.X >= width/2`. With a locked edge the free edge can be `Right`
while the pointer is in the left half (tall page, `LockedAnchor=Left`), so
`PageTurnCompleted.Direction` reports `Backward` for a left-hinged turn. The
pending-corner path (`UpdateDrag` → `SelectCornerEdge`) never updates direction
either. Fix: when the free edge resolves to `Left`/`Right`, derive direction from
it (`Right` free → Forward, `Left` free → Backward); keep pointer-half for
`Top`/`Bottom`. Regression tests in `PageCurl.Core.Tests`.

### F2 — `PageTurnView` hit-testing and units (bugs)
- `TryGetPagePointer` rejects outside-page presses only when
  `ContentScale == AspectFit`, but `PageAspectRatio > 0` also letterboxes
  (`GetContentViewport`). Reject whenever the viewport is smaller than the canvas.
- Fallbacks in `TryTurn`, `TryGetPagePointer`, `ToPagePointer` build a viewport
  from `Width`/`Height` (DIPs) while touch and paint use canvas pixels. Use
  `CanvasSize` when available, one shared helper.
- `UpdateInteraction` reads `Stopwatch` directly instead of
  `AnimationTimestampProvider`, so drag velocity is not deterministic in tests.

### F3 — Null colours (bug)
`BacksideColor`/`SurfaceColor` (`PageTurnView`, `PageCurlView`, `PageSpreadView`)
accept `null`; `ToSkColor` then throws inside paint. Add `validateValue` non-null.

### F4 — Snapshot refresh fault tolerance (bugs)
- `PageSpreadView.RefreshAfterAsync` awaits the previous chain link and only
  swallows cancellation; any other fault (for example a throwing
  `SnapshotFailed` handler) poisons every later refresh.
- `QueueSnapshotRefresh`/`ResumePendingSnapshotRefresh` (both views) are
  `async void` and catch only `OperationCanceledException`; anything else
  (`ObjectDisposedException` after handler detach, subscriber exceptions)
  crashes the app.
Fix: chain ignores predecessor faults; automatic refresh paths never throw;
explicit `RefreshSnapshotAsync` still surfaces errors to its caller.

### F5 — Android snapshot PNG round trip (simplification/perf)
`PageContentSnapshotter.Android` encodes to PNG then decodes. Copy pixels
directly into an `SKImage` (as the Apple path does) and re-check cancellation.

### F6 — Duplicated helpers (simplification)
`PageCurlView` and `PageSpreadView` each implement zone-touch → page-pointer
scaling and the "content has handler and size" predicate. Extract one internal
static helper; no behaviour or public API change. Larger unification of the two
snapshot schedulers is out of scope (risk > value).

### F7 — Demo cleanup
- Page count `8` hard-coded in four navigation states and the `"/ 08"` label;
  derive from `DemoPageCopy.Pages.Count`.
- `NextRightContent` clamps to the last page instead of `null` for an odd tail.
- `async void` handlers (`OnRefreshSnapshotClicked`, `OnInteractiveContentChanged`)
  let non-cancellation exceptions escape.

### F8 — Documentation
- README carries 0.1.1/0.1.2 release notes in the install section.
- `docs/site/content.md` lists `LockedEdge` for `PageCurlView`, which only has
  `LockedAnchor`.
- `SnapThreshold`/`LockedEdge` are described as compatibility facades without
  saying which name to prefer everywhere they appear.
- Re-check runtime/interaction docs against F1–F4 behaviour; run
  `tools/check-docs.py` and `tools/test-quick-start.sh`.

## Not changing
- `PageCurlView.Renderer` / `PageSpreadView.Overlay` public exposure (API break).
- Per-frame `SKVertices.CreateCopy` (documented trade-off).
- Windows snapshotter (cannot be validated on this host).
