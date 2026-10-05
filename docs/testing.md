# Testing and validation

The [requirements contract](requirements.md) assigns stable IDs R01–R12.
Tests prove acceptance behavior; a dated validation record proves a particular
run. Keep those separate from release and platform readiness. The latest
[requirements validation record](validation/requirements-2026-10-05/README.md)
includes failures, fixes, repeated runs and visible computer-control observations.
The [test-quality audit](validation/test-quality-2026-10-05/README.md) records
which tests were removed or strengthened and the hosted Actions fixes.
The [showcase polish record](validation/showcase-polish-2026-10-05/README.md)
covers shared page copy, theme contrast and stronger UI-replay assertions.

| Layer | Purpose | Automation |
| --- | --- | --- |
| Core | R01–R05/R08/R11: input, geometry, navigation, completion and allocations | Dependency-free deterministic runner, including chained turns from completion handlers |
| Public control API | R04/R09: valid settings, forwarding, retained state and reduced motion | `PublicApiTests` in the neutral MAUI runner |
| Snapshot/lifecycle | R06–R08: ownership, revisions, failure and cancellation | Neutral runner on one pumped UI thread |
| Package consumer | R12: usable transitive packages and representative consumer calls | `tools/test-package-consumer.sh` with a temporary external project, mapped local feed and empty package cache |
| Native Apple fixture | R02/R06/R08/R10: native snapshot pixels, mesh submission, actual layout and linked demo commands | Built Mac Catalyst assertion host plus bounded `tools/run-native-fixture.py`; driver tests under `tools/tests` |
| Computer control | R04/R05/R08/R10: real input routing, visible counters, boundaries and live controls | Replayable AX-based journey plus screenshot-based drags and responsive-layout inspection |

For computer control, load
[`pagecurl-checks.js`](../tests/computer-control/pagecurl-checks.js) into a
runtime providing `cua`, and call `runPageCurlChecks(cua, absoluteAppPath)`.
Pass the optional scene list to run one scene per bounded tool call. It resolves
buttons from fresh accessibility trees, waits for readiness and checks the
visible counter and enabled/disabled boundary buttons after commands, instead of guessing coordinates or capture
durations. It temporarily enables reduced motion and restores its previous
value. Counters finish at each scene's first page. The same replay resolves
compact scene labels and opens/closes the Options sheet for settings.
`runPageCurlLiveContentCheck(cua, absoluteAppPath)` verifies the interactive
counter, automatic refresh and explicit refresh in either layout.
`runPageCurlGestureCheck(cua, absoluteAppPath, from, to, counter, status)` checks
real pointer input; choose points from a fresh screenshot of the visible page.
`runPageCurlAccessibilityCheck(cua, absoluteAppPath)` checks accessible names
and native roles for navigation and option controls, closing compact Options
when it opened them. Run `node --test tests/computer-control/pagecurl-checks.test.js`
to check the replay itself, including false positives and failure cleanup.
The [showcase checks skill](../.agents/skills/pagecurl-showcase-checks/SKILL.md)
provides the repeatable visual and interaction workflow.
Resize through the native window controls and inspect both layouts visually.
AX counters alone cannot prove gesture routing or pixel quality.

The native driver removes stale evidence, bounds execution, and requires exit
0, no failed assertions and the final `PASS all native suites` marker. Its
subprocess tests verify timeouts, missing/partial output and contradictory
exit-code/result cases. The fixture itself tests automatic and explicit refresh failure and
completion and delayed failure after changing or re-entering scenes, and the counter/button layout
at a 250 by 333 page size. It also checks the resolved option-heading contrast
in light and dark themes against a 4.5:1 minimum, plus compact Options routing
and restoration of wide controls at a constrained 600 by 750 page layout. These are injected at the capture boundary
while the real page and event handlers run.

CI runs Core, the isolated package consumer and validation-driver and UI-replay tests on Linux;
the macOS job runs the neutral API/lifecycle suite and builds/runs the native
fixture, retaining its assertion log even on failure. A workflow definition is
not evidence of a hosted run. No computer-control or physical-device pass is
implied by CI configuration.

The neutral MAUI runner pumps asynchronous continuations on one thread to
match the controls' UI-thread contract. Cancellation tests distinguish a
cancelled caller wait from completion of the internal serialized capture;
disposal assertions wait for that capture cleanup. Settling loops have a 600-frame
limit so a broken completion path produces a failure instead of hanging. Spread
touch tests exercise owned and foreign pointer release/cancellation in both
directions, including duplicate terminal events. Native rendering regressions
remain alongside these behavioral checks; private IL call-name inspections and
checks for an already removed API are intentionally omitted.

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

- public enum/numeric validation before property publication, wrapper/renderer
  consistency, finite dim-factor clamping and immediate reduced-motion cancellation;
- unavailable-platform and pre-cancelled snapshot capture;
- snapshot generations, stale-result disposal, last-good-frame retention,
  independent front/back replacement, hide/reveal rejection and teardown order;
- `PageCurlView` capture, failure events, and interaction property forwarding;
- six-slot spread capture, transparent overlay composition, and
  readiness-aware forward/back state;
- spread refresh coalescing: automatic requests capture only dirty slots,
  explicit requests recapture in-place changes, obsolete revisions are rejected,
  and deferred requests preserve completion and cancellation;
- active-turn image ownership in both directions, removed optional backs,
  cancellation and handler teardown;
- immediate cancellation and release of spread pointer ownership, stable
  snapshot textures during curls (including failed capture and null backs),
  suppressed stale failures, stage-sized renderers, and caller-owned image survival through replacement and teardown;
- first programmatic turns from a hidden snapshot renderer;
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

## Workflow and documentation checks

Verify runs pinned `actionlint` to check workflow syntax, expressions and runner
labels. Script syntax and project-reference checks complement it; they cannot
validate GitHub expression scopes. Run the same check before workflow edits are
pushed:

```bash
go run github.com/rhysd/actionlint/cmd/actionlint@v1.7.12 -shellcheck= -pyflakes=
```

The documentation build treats DocFX warnings as errors and checks local links,
anchors, private source links and publication boundaries. Package inspection
requires the requested platform assemblies, XML documentation and Core dependency.
See [documentation publishing](documentation.md) and the Verify workflow.
