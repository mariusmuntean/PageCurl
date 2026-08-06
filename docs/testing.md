# Testing and validation

The platform-free test runner covers:

- every edge and corner, pinned-corner rejection, corner direction selection,
  exact grabbed-point retention, pointer capture, and non-finite input;
- forward/backward progress, custom threshold and flick decisions, threshold
  entry/exit events, cancellation, reduced motion, programmatic turns,
  one-shot completion, stable diagonal rollback, and visible settling;
- navigation boundaries and direction-aware destination pages;
- finite cylinder and cone geometry, stationary edges and pins, pointer
  attachment, developable material lengths, valid UV/shade ranges, front/back
  orientation, exact cylinder tangent seams, deep-curl layering inputs, and
  deterministic buffer reuse;
- warmed controller, cylinder, and four-corner cone loops with zero managed
  allocations.

Visual validation still matters. Exercise every edge and corner, reversal,
snap-back, committed settling, backward turns, all lock modes, large pages,
light/dark appearance, compact and wide layouts, rotation, reduced motion,
diagnostics, background/foreground transitions, and repeated image replacement.

Every geometry or interaction fix needs a deterministic regression. Visual
changes need a reproducible manual scenario or before/after evidence. Use the
repository validation skill for current commands and explicit target routing.
