# Testing and validation

The platform-free test runner covers:

- every edge and corner, locked-edge rejection, corner direction selection,
  pointer capture, and non-finite input;
- forward/backward progress, threshold and flick decisions, cancellation,
  reduced motion, programmatic turns, one-shot completion, stable diagonal
  rollback, and visible committed settling;
- navigation boundaries and direction-aware destination pages;
- finite geometry, stationary hinges, pointer attachment, developable material
  lengths, valid UV/shade ranges, front/back orientation, exact tangent seams,
  deep-curl layering inputs, and deterministic buffer reuse;
- warmed controller and geometry loops with zero managed allocations.

Visual validation still matters. Exercise every edge and corner, reversal,
snap-back, committed settling, backward turns, all lock modes, large pages,
light/dark appearance, compact and wide layouts, rotation, reduced motion,
diagnostics, background/foreground transitions, and repeated image replacement.

Every geometry or interaction fix needs a deterministic regression. Visual
changes need a reproducible manual scenario or before/after evidence. Use the
repository validation skill for current commands and explicit target routing.
