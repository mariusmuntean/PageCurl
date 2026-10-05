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
- A completion subscriber may start another animated turn synchronously. The
  controller's `Tick` continues to request frames for that new settling turn.
- Invalid enum and non-finite setting assignments leave the last valid value
  intact. Finite backside dim factors clamp to 0–1. Binding rejection follows
  MAUI semantics and does not promise an exception for every invalid assignment.
- Call `CancelInteraction()` before removing or recycling the view. It immediately
  rolls back and releases pointer ownership; it does not leave a hidden animation. On teardown,
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
- Refresh waits for an active curl to finish, including a turn started during
  capture. Waiting preserves cancellation; textures and null slots are not
  replaced while the sheet is moving. Obsolete content revisions and cancelled
  capture failures cannot publish images or error events.
- Hiding invalidates captures started in the previous presentation. Existing
  snapshots remain available; late images are disposed and late failures are
  suppressed even if the control becomes visible again.
- A new capture cancels the previous generation. A late result is disposed and
  cannot replace the current frame. Front and back captures fail independently.
- Custom `IPageContentSnapshotter` implementations must eventually complete and
  cooperate with cancellation. Caller cancellation cannot forcibly stop a native
  capture or an arbitrary provider; spread refresh keeps its serialization barrier
  until that capture completes. A provider that never completes can block later
  refreshes, including after hide/reveal.
- `SnapshotFailed` reports the content and exception. The wrapper keeps the
  last good frame. A missing native video layer needs an app-supplied poster.
- Gesture start never waits for capture. Edge and corner activation zones use
  the cached frame, and the center remains available to live child controls.
- On handler teardown, the wrapper cancels capture, clears renderer image
  properties, then disposes owned snapshots.

## Two-page spreads

- `PageSpreadView` owns snapshots for current left/right content, the
  previous-left/next-right under-page destinations, and the
  previous-right/next-left sheet back faces. It does not own the MAUI views.
- Forward uses the right sheet: the next-right page is revealed underneath
  and the next-left page rides on the sheet's back. Backward mirrors that
  with the left sheet, previous-left under page, and previous-right back
  face. Omitted back-face content falls back to the configured
  `BacksideMode`. Both paths raise one `PageTurnCompleted` event.
- `CanTurnForward` and `CanTurnBackward` include content and snapshot readiness,
  and remain false while a sheet settles. Update the content model only after a
  committed completion.
- Public `RefreshSnapshotAsync` invalidates every slot so changes inside an
  existing content view are captured. Automatic property and layout refreshes
  serialize behind the running capture and drain only dirty slots.
- Hiding ends the current refresh lifecycle and marks all slots dirty while
  retaining their last good textures. Showing queues a fresh drain.
- Each content/layout invalidation increments a slot revision. Obsolete results
  are disposed without replacing current images or acknowledging newer content.
  A current failed capture keeps the last good texture and its dirty state,
  raises `SnapshotFailed`, and is retried by the next refresh request.
- Refresh requests made during a turn wait for completion and retain their
  cancellation token. Cancellation of one request does not cancel queued ones.
  Capture publication and disposal pause while the overlay references images;
  completion or teardown clears overlay assignments before disposing snapshots.
- Removed optional sheet backs immediately use the configured fallback, even
  while clearing their cached images is queued.
