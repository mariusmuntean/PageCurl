# Testing and validation

The neutral MAUI runner pumps asynchronous continuations on one thread to
match the controls' UI-thread contract. Cancellation tests distinguish a
cancelled caller wait from completion of the internal serialized capture;
disposal assertions wait for that capture cleanup.

The platform-free test runner covers:

- every edge and corner, pinned-corner rejection, corner direction selection,
  exact grabbed-point retention, pointer capture, and non-finite input;
- grab-relative progress: mid-page grabs start flat at zero progress and the
  curl grows only while the pointer moves;
- forward/backward progress, custom threshold and flick decisions, threshold
  entry/exit events, cancellation, reduced motion, programmatic turns,
  one-shot completion, stable diagonal rollback, and visible settling;
- navigation boundaries and direction-aware destination pages;
- full spread travel beyond the spine, both endpoint decisions, diagonal pulls
  with a fixed spine, reflected landing geometry, narrow-grid clipping capacity,
  and warmed spread allocations;
- semantic haptic mapping for threshold entry/exit, committed completion,
  cancellation, disabled feedback, and duplicate suppression;
- finite cylinder and cone geometry, stationary edges and pins, pointer
  attachment, developable material lengths, valid UV/shade ranges, front/back
  orientation, exact cylinder tangent seams, deep-curl layering inputs,
  continuous per-triangle underside lighting, readable ambient shade, and
  deterministic buffer reuse;
- warmed controller, cylinder, and four-corner cone loops with zero managed
  allocations.

The neutral MAUI runner covers:

- unavailable-platform and pre-cancelled snapshot capture;
- snapshot generations, stale-result disposal, last-good-frame retention,
  independent front/back replacement, and teardown order;
- `PageCurlView` capture, failure events, and interaction property forwarding;
- four-page spread capture, transparent overlay composition, and
  readiness-aware forward/back state;
- spread refresh coalescing: automatic requests capture only dirty slots,
  explicit requests recapture in-place changes, obsolete revisions are rejected,
  and deferred requests preserve completion and cancellation;
- active-turn image ownership in both directions, removed optional backs,
  cancellation and handler teardown;
- immediate cancellation and release of spread pointer ownership, stable
  snapshot textures during curls (including failed capture and null backs),
  suppressed stale failures, stage-sized renderers, and native vertex disposal;
- first programmatic turns from a hidden snapshot renderer and no filesystem
  calls in touch dispatch;
- book-model spread wiring: the turning sheet's back face is the adjacent
  page it lands on, in both directions;
- renderer visibility while flat, during a turn, and after cancellation.

Visual validation still matters. Exercise every edge and corner, reversal,
snap-back, committed settling, backward turns, all lock modes, large pages,
light/dark appearance, compact and wide layouts, rotation, reduced motion,
background/foreground transitions, live controls, local video and its poster
fallback, snapshot failure, repeated image replacement, spread boundaries, and
repeated gutter-crossing turns.

The iOS smoke records the simulator while dragging, then extracts a frame from
the active gesture. This makes a missing stationary spread page, blank custom
backside, or faceted underside band reviewable instead of relying on a flat
before/after screenshot.

Every geometry or interaction fix needs a deterministic regression. Visual
changes need a reproducible manual scenario or before/after evidence. Use the
repository validation skill for current commands and explicit target routing.

`tests/PageCurl.Apple.Tests` is a native Mac Catalyst host for actual snapshot
color/alpha assertions and linked demo command/lifecycle regressions. It checks
six color cases, all four reduced-motion scenes, animated completion, all six
spread slots on teardown, resource recreation, and queued callbacks. Its
`PAGECURL_TEST_OUTPUT` environment variable selects the result log; a native
build alone does not mean those assertions ran. The native process exits with
code 1 if any assertion fails, or code 0 when all assertions pass. Use the recorded platform
validation results to distinguish fixture passes from unavailable device gates.

Native assertions also verify custom backs in landing orientation, shrinking
mesh tails, the spread edge's UIKit hit target, immediate cancellation during
Single-to-Spread switching, and ignored inactive completion events.

The immediate-switch regression renders five actual GL drag frames, releases,
and clicks Spread in the same event-loop turn. It requires the click handler
to return within 150 ms and the next UI dispatch within 250 ms. Navigation
checks await snapshot readiness rather than assuming a fixed capture duration.

The iOS smoke terminates its app, stops recording, removes temporary movies,
and shuts down simulators it boots, including on failures. Existing simulator
sessions retain their original appearance and boot state.

The [latest interaction and Rider review](validation/rider-and-interaction-2026-10-04/README.md)
records the completed regressions, native assertions, platform builds, and
remaining environment limits for these fixes.
