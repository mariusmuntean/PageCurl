# PageCurl refinement design

**Status:** Approved for implementation
**Date:** 2026-08-06

## Goal

Finish the page deformation work, remove avoidable frame cost, simplify the
sample application, and replace the split `docs/` and `knowledge/` structure
with one current documentation tree.

The control remains a small analytic page curl. This pass does not introduce a
cloth simulation, another renderer, a UI framework, or new public tuning APIs.

## Work tracks

The work is split into two independent tracks that meet at final validation:

1. Core geometry and render-path performance.
2. Demo presentation and repository documentation.

Both tracks preserve the existing boundary between `PageCurl.Core`,
`PageCurl.Maui`, and `PageCurl.Demo`.

## Core geometry

### Interaction contract

`PageTurnController`, `CurlPose`, `PageMeshBuffer`, and the public
`PageTurnView` API remain compatible. Every physical edge can curl while a
different physical edge is locked as the stationary hinge.

The locked edge includes its two endpoints. An exact point on that edge cannot
both remain stationary and follow a pointer, so locked-edge endpoints do not
start a gesture. Other edge points and corners retain direct pointer tracking.

### Deformation model

The geometry continues to use a bounded analytic cylinder in the coordinate
frame of the selected free edge. Each source point is mapped once by a shared
point-transform function. The same function is used to solve the grab anchor,
which removes the duplicated curl-region logic currently used by mesh building
and anchor calculation.

For a locked hinge that is not opposite the free edge, displacement is
constrained by a cubic smooth-step field:

- influence is zero at the stationary hinge;
- influence reaches one at the grabbed point's hinge distance;
- position and first derivative are continuous;
- points beyond the grabbed constraint receive the full cylinder mapping.

Position displacement, curl depth, and shading use the same influence. This
keeps the hinge stationary without introducing bands or classifying visually
flat hinge triangles as the page back.

The existing progress-dependent radius remains: the page relaxes near both
turn endpoints and curls most tightly near the midpoint. Values remain finite
and bounded before being written to the fixed mesh buffers.

### Sorting and allocations

Triangle storage remains fixed at `PageMeshBuffer` construction. The current
quadratic insertion sort is replaced by an in-place span sort over the active
triangle range. No comparer, collection, or temporary array is created per
frame.

The implementation will not add a benchmark project or expose performance
configuration. Existing opt-in render diagnostics and the warmed allocation
guard are sufficient for this pass; elapsed-time evidence is gathered from the
demo rather than asserted with a flaky wall-clock unit test.

## Demo application

### Visual direction

The sample uses an editorial canvas:

- a warm neutral background and warm paper surface;
- ink-colored typography;
- one cobalt accent;
- a slim header containing `PageCurl` and the page count;
- a large, uncluttered reading stage;
- compact Previous and Next commands in the footer.

The page curl is the visual focus. The gradient brand tile, status badge,
uppercase label clutter, stacked control cards, and eight large edge markers
are removed.

One quiet instruction explains that any free edge or corner can be dragged. It
fades after the first interaction and can be restored from the settings tray.
The generated page specimens use warm paper, ink text, cobalt details, and
visible alignment marks that make deformation errors easy to see.

### Secondary controls

Hinge selection, reduced motion, testing mode, and hint restoration live in a
compact settings tray toggled from the footer. The tray is built with existing
MAUI controls and simple visibility state; it does not add a toolkit or custom
panel abstraction.

Diagnostics continue to poll only while testing mode is enabled. Page images
remain caller-owned, and disappearing/reappearing lifecycle behavior remains
unchanged.

### Responsive and accessible behavior

The stage fills its star-sized row and `PageTurnView` aspect-fits the generated
page. The same hierarchy works in portrait mobile windows and wide Mac Catalyst
windows without fixed landscape caps.

Previous, Next, settings, hinge selection, reduced motion, testing mode, and
the interactive page retain explicit semantic descriptions. Decorative hints
remain outside the accessibility tree. Command-driven and gesture-driven turns
continue through the same completion path.

Light and dark themes share the editorial hierarchy. Dark mode changes the
surrounding canvas and controls while keeping the paper legible.

## Documentation

The repository exposes one canonical `docs/` tree:

```text
docs/
  index.md
  architecture.md
  interaction-and-rendering.md
  runtime-contracts.md
  testing.md
  platform-notes.md
  compatibility.md
  release-readiness.md
  validation/
```

Content from `knowledge/project/` is merged into these pages. Duplicate
introductions and command lists are removed. Validation commands remain
authoritative in `.agents/skills/pagecurl-maui-validation/SKILL.md`, while
dated device evidence remains under `docs/validation/`.

The `knowledge/` directory, obsolete design specifications, completed
implementation plans, `.DS_Store` files, and their routing README are removed.
Git history remains the archive. `README.md`, `CONTRIBUTING.md`, `SUPPORT.md`,
`AGENTS.md`, the validation skill, and all retained Markdown links are updated
to the canonical locations.

This approved design file is temporary implementation guidance. Its durable
decisions are folded into the canonical documentation before the work is
finished, after which the design file is removed with the older historical
records.

## Failure handling

Non-finite geometry inputs still produce the safe flat page. Invalid or locked
gesture starts leave the controller idle. Workload and native SDK failures are
classified before source changes; unavailable platform checks are reported as
not verified rather than treated as implementation failures.

The demo settings tray has no persistent storage. Closing it does not change
the selected hinge, reduced-motion value, or testing-mode value.

## Verification

Core regression coverage will prove:

- continuous neighboring mesh displacement for horizontal and vertical curls;
- stationary hinge vertices for every valid hinge/free-edge pairing;
- pointer attachment at valid grabbed points;
- finite positions, depth, shading, and texture coordinates;
- stable front/back triangle streams;
- fixed buffer reuse and zero warmed managed allocations;
- locked-edge endpoints cannot start contradictory gestures.

Repository and adapter checks will include the core runner, neutral MAUI build,
explicit Mac Catalyst build, workflow project-reference checks, documentation
link checks, and diff hygiene.

Visual validation will exercise portrait and wide layouts, light and dark
themes, the settings tray, reduced motion, testing diagnostics, every edge and
corner, each hinge selection, slow release, flick release, cancellation,
forward/backward commands, and the large-page specimen. iOS simulator evidence
will be refreshed when CoreSimulator is available; otherwise the missing check
is recorded explicitly.

## Completion criteria

The work is complete when both tracks are integrated, the canonical docs match
the current source, automated checks pass, available platform builds pass, and
fresh rendered evidence shows the editorial demo and continuous page curl
without the previous deformation bands. Physical-device profiling remains a
release gate rather than a claim of this local pass.
