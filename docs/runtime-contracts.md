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
  Connected wrappers preserve a turn started from a cancellation subscriber;
  disconnected wrappers reject restart requests before releasing snapshots.
- Settling uses monotonic elapsed time between dispatcher callbacks. A delayed
  callback advances the spring within Core's 100 ms sample limit; each new loop
  starts a fresh clock and obsolete timer generations cannot advance a new turn.
- Commands rejected during an active turn preserve its presentation. A new
  pointer press cannot replace a settling turn; competing presses during an
  owned drag remain consumed without changing the owner.
- Invalid enum and non-finite setting assignments leave the last valid value
  intact. Finite backside dim factors clamp to 0–1. Binding rejection follows
  MAUI semantics and does not promise an exception for every invalid assignment.
- `PageTurnView(PageCurlOptions)` preserves the supplied threshold and flick
  settings together. Its `Options` and controller share that options instance;
  changing the view's interaction properties updates it.
- Call `CancelInteraction()` before removing or recycling the view. It immediately
  rolls back and releases pointer ownership; it does not leave a hidden animation. On teardown,
  cancel interaction, clear image properties, unsubscribe events, and only then
  dispose recycled images.
- Cancel active turns when the application window stops. The demo observes
  window stop/resume while its page is visible, cancels without committing
  navigation, pauses video, and resumes playback only in the active video scene.
- Enable `PerformanceDiagnosticsEnabled` only while collecting evidence. The
  default paint path avoids stopwatch and allocation reads.
- Showing an idle `PageTurnView` requests a fresh paint even when its images
  are unchanged. Hidden invalidations cannot substitute for that visible draw;
  the renderer remains on demand.
- Treat `SKGLView` as the current adapter, not part of the geometry contract. A
  target can replace it while retaining the core API.

## Snapshot-backed content

- `PageCurlView` owns only the `SKImage` snapshots it creates. It does not own
  `FrontContent`, `BackContent`, or caller-supplied destination images.
- Keep page content attached and laid out before capture. The wrapper captures
  after load and size changes; call `RefreshSnapshotAsync()` when a counter,
  image, animation, or video preview changes while idle.
- The live page fits inside the current layout bounds and wrapper padding at its configured aspect ratio.
  Resizing remeasures the page during arrangement so its native content and
  turning snapshots stay within the same page frame. Spreads apply the same
  fitting rule to their combined two-page frame.
- Automatic load and layout refreshes coalesce invalidations for one frame (16 ms), allowing
  live content and input to present before native rasterization. Explicit
  `RefreshSnapshotAsync()` does not add this scheduling delay. Layout readiness,
  snapshot resolution and inclusion of recent native changes remain required.
- The demo batches rapid live-counter changes into one capture of the latest
  value. Switching scenes or content kind, or stopping the window before the
  queued refresh begins, discards that obsolete request. Explicit Refresh keeps
  its direct, awaitable path.
- Refresh waits for an active curl to finish, including a turn started during
  capture. Waiting preserves cancellation; textures and null slots are not
  replaced while the sheet is moving. Obsolete content revisions and cancelled
  capture failures cannot publish images or error events.
- Hiding invalidates captures started in the previous presentation. Existing
  snapshots remain available; late images are disposed and late failures are
  suppressed even if the control becomes visible again.
- A live-page refresh records its content revision before waiting for a turn.
  Hiding or replacing that content during the wait makes the request obsolete;
  it finishes without rasterizing the outgoing page.
- A new capture cancels the previous generation. A late result is disposed and
  cannot replace the current frame. Front and back captures fail independently.
- Custom `IPageContentSnapshotter` implementations must eventually complete and
  cooperate with cancellation. Caller cancellation cannot forcibly stop a native
  capture or an arbitrary provider; spread refresh keeps its serialization barrier
  until that capture completes. A provider that never completes can block later
  refreshes, including after hide/reveal.
- `SnapshotFailed` reports the content and exception. The wrapper keeps the
  last good frame. A missing native video layer needs an app-supplied poster.
  For spreads, `IsBackside` identifies the previous-right and next-left sheet
  backs; current and under-page destinations report `false`.
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
  and remain false while hidden or while a sheet settles. Hidden commands are
  rejected even from visibility-change callbacks. Revealing current snapshots
  restores readiness without requiring another explicit refresh. Layout and
  content invalidation disable affected directions until current snapshots
  publish; commands also recheck readiness inside change callbacks. Update the
  content model only after a committed completion.
- Public `RefreshSnapshotAsync` invalidates every slot so changes inside an
  existing content view are captured. Automatic property and layout refreshes
  serialize behind the running capture and drain only dirty slots.
- A queued drain waits for newly assigned content to attach and arrange before
  capture. Cancellation, replacement, hiding and handler teardown wake layout
  waits; replacement content is checked again before any capture can publish.
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
- Pending automatic spread capture resumes on a later UI dispatch after turn
  completion. This lets a cancellation callback hide or remove the outgoing
  spread before native rasterization; immediate hide/re-entry also invalidates
  that continuation. Normal completion still captures the dirty slots, and an
  explicit refresh remains awaitable.
- Removed optional sheet backs immediately use the configured fallback, even
  while clearing their cached images is queued.
