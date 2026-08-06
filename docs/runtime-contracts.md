# Runtime contracts

- The caller owns every supplied `SKImage`. Keep images alive while assigned,
  clear the view properties before recycling them, then dispose them from the
  application cache.
- Set images and invoke control commands on the UI thread.
- Assign `FrontImage` for the current page, `BackImage` for its optional
  underside, `UnderPageImage` for the forward destination, and
  `PreviousPageImage` for the backward destination. Backward turns fall back to
  `UnderPageImage` when `PreviousPageImage` is absent.
- Commit the page model from `PageTurnCompleted`. Gesture and programmatic turns
  raise the same event once.
- Call `CancelInteraction()` before removing or recycling the view. On teardown,
  cancel interaction, clear image properties, unsubscribe events, and only then
  dispose recycled images.
- Enable `PerformanceDiagnosticsEnabled` only while collecting evidence. The
  default paint path avoids stopwatch and allocation reads.
- Treat `SKGLView` as the current adapter, not part of the geometry contract. A
  target can replace it while retaining the core API.

## Snapshot-backed content

- `PageCurlView` owns only the `SKImage` snapshots it creates. It does not own
  `FrontContent`, `BackContent`, or caller-supplied destination images.
- Keep page content attached and laid out before capture. The wrapper captures
  after load and size changes; call `RefreshSnapshotAsync()` when a counter,
  image, animation, or video preview changes while idle.
- A new capture cancels the previous generation. A late result is disposed and
  cannot replace the current frame. Front and back captures fail independently.
- `SnapshotFailed` reports the content and exception. The wrapper keeps the
  last good frame. A missing native video layer needs an app-supplied poster.
- Gesture start never waits for capture. Edge and corner activation zones use
  the cached frame, and the center remains available to live child controls.
- On handler teardown, the wrapper cancels capture, clears renderer image
  properties, then disposes owned snapshots.

## Two-page spreads

- `PageSpreadView` owns snapshots for current left/right content and the
  previous-left/next-right destinations. It does not own the MAUI views.
- Forward uses the right sheet and next-right destination. Backward uses the
  left sheet and previous-left destination. Both paths raise one
  `PageTurnCompleted` event.
- `CanTurnForward` and `CanTurnBackward` include content and snapshot readiness,
  and remain false while a sheet settles. Update the content model only after a
  committed completion.
