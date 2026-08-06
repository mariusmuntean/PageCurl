# PageCurl refinement design

**Status:** Approved for implementation
**Date:** 2026-08-06

## Goal

Finish the page deformation work, remove avoidable frame cost, simplify the
sample application, and replace the split `docs/` and `knowledge/` structure
with one current documentation tree.

The control uses a fixed-buffer, constraint-based paper solver guided by an
analytic curl target. This pass does not introduce another renderer, a UI
framework, tearing, plastic creases, or a collection of material-tuning APIs.

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

### Paper model

The final surface comes from a position-based sheet solver. The existing
bounded cylinder supplies a smooth target shape; it no longer supplies the
final vertex positions directly.

The solver stores 3-D particle positions in `PageMeshBuffer` and enforces:

- hard horizontal, vertical, and diagonal distance constraints so the page
  cannot stretch or shear;
- compliant second-neighbor bending constraints so curvature spreads across
  the sheet instead of collapsing into a crease;
- exact position constraints along the locked hinge;
- an interpolated edge constraint that keeps the grabbed point attached to the
  pointer even when it falls between mesh vertices.

The solver follows the extended position-based dynamics formulation: fixed
topology, per-constraint multipliers, compliance scaled by the step interval,
and a bounded iteration count. Stretch and shear compliance is zero. Bend
compliance is small and scale-aware, producing stiff paper rather than cloth.
The solver has no plasticity, tearing, or permanent fold state.

For each active frame, the buffer predicts from its previous solved positions,
moves toward the analytic curl target, projects sheet constraints, reapplies
hinge and pointer constraints, then writes the solved positions. It resets to a
flat sheet when page dimensions or interaction topology change. Inactive and
endpoint poses return a flat page without retaining a deformed state.

The existing progress-dependent radius remains the curl guide: the page is
flat near both turn endpoints and curves most around the midpoint. The default
is tuned as a stiff book page. `RadiusScale` remains the advanced curvature
control, avoiding another public material-options surface.

Triangle orientation comes from solved 3-D normals. Normals also provide
bounded per-vertex shading, so front/back classification and light follow the
paper surface instead of a depth threshold.

### Sorting and allocations

Particle positions, constraint topology, multipliers, grid vertices, and
triangle storage are allocated at `PageMeshBuffer` construction. The current
quadratic insertion sort is replaced by an in-place span sort over the active
triangle range. No comparer, collection, constraint object, or temporary array
is created per frame.

The implementation will not add a benchmark project or expose performance
configuration. Existing opt-in render diagnostics and the warmed allocation
guard cover the frame path; elapsed-time evidence is gathered from the demo
rather than asserted with a flaky wall-clock unit test.

### Library API

The default XAML surface stays small: assign page images, choose content scale,
and handle `PageTurnCompleted`. The paper solver remains internal. Consumers do
not configure particle masses, compliance, iterations, gravity, or damping.
The existing `PageCurlOptions` continues to cover mesh density, curl radius,
release threshold, and spring frequency for advanced callers.

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

- structural and diagonal edge lengths remain at their rest lengths within a
  strict floating-point tolerance;
- bending stays distributed and adjacent triangle angles remain below the
  crease threshold;
- stationary hinge vertices for every valid hinge/free-edge pairing;
- pointer attachment at valid grabbed points;
- finite positions, normals, depth, shading, and texture coordinates;
- stable front/back triangle streams;
- deterministic reset and repeated-frame convergence;
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
fresh rendered evidence shows a stiff, non-stretching page that curls without
forming sharp folds or the previous deformation bands. Physical-device
profiling remains a release gate rather than a claim of this local pass.

## Research basis

- Müller, Heidelberger, Hennix, and Ratcliff,
  [Position Based Dynamics](https://doi.org/10.1016/j.jvcir.2007.01.005).
- Macklin, Müller, and Chentanez,
  [XPBD: Position-Based Simulation of Compliant Constrained Dynamics](https://doi.org/10.1145/2994258.2994272).
- Guven and Müller,
  [How paper folds: bending with local constraints](https://arxiv.org/abs/0712.0978).
