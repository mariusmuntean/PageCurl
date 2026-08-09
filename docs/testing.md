# Testing and validation

The platform-free test runner covers:

- every edge and corner, pinned-corner rejection, corner direction selection,
  exact grabbed-point retention, pointer capture, and non-finite input;
- forward/backward progress, custom threshold and flick decisions, threshold
  entry/exit events, cancellation, reduced motion, programmatic turns,
  one-shot completion, stable diagonal rollback, and visible settling;
- navigation boundaries and direction-aware destination pages;
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
