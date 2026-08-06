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
