# Testing and validation

The [requirements contract](requirements.md) assigns stable IDs R01–R12.
Tests prove acceptance behavior; dated records prove completed runs. Neither a
build nor a workflow definition establishes runtime, device or release readiness.
Use the [validation skill](../.agents/skills/pagecurl-maui-validation/SKILL.md)
for current commands and explicit platform targets.

| Layer | Purpose | Automation |
| --- | --- | --- |
| Core | R01–R05/R08/R11: input, geometry, navigation, completion and allocations | Dependency-free deterministic runner |
| Public control API | R04/R09: settings, forwarding, retained state and reduced motion | `PublicApiTests` in the neutral MAUI runner |
| Snapshot/lifecycle | R06–R08: ownership, revisions, failure and cancellation | Neutral runner on one pumped UI thread |
| Package consumer | R12: transitive packages and representative consumer calls | `tools/test-package-consumer.sh`: external project, mapped local feed, empty package cache |
| Quick start | Examples aligned with the public API | `tools/test-quick-start.sh` compiles unchanged guide examples in a temporary neutral MAUI project |
| Native Apple fixture | R02/R06/R08/R10: pixels, mesh submission, layout and linked demo commands | Mac Catalyst assertion host with bounded `tools/run-native-fixture.py` |
| Computer control | R04/R05/R08/R10: pointer routing, accessibility and visible layout | AX-based replay, screenshot-based drags and visual inspection |

## Deterministic regression coverage

Every geometry or interaction fix needs a deterministic regression. Core covers
all edges/corners and locks, grabbed-point retention, finite input, relative
progress, threshold/flick decisions, reversal, cancellation, reduced motion,
one-shot and chained completion, and direction-aware navigation boundaries.
Extreme finite sizes, spring frequencies and page counts must remain bounded
without overflow, non-finite state or extra completion events.
Geometry checks protect stationary edges, developable material lengths, valid
UV/shade ranges, front/back orientation, tangent seams, underside lighting,
cross-spine travel and reflected landing. Warmed controller, cylinder, cone and
spread loops require zero managed allocations and bounded reusable buffers.

The neutral MAUI runner checks property validation before publication, wrapper
forwarding, immediate reduced-motion cancellation, snapshot generations,
last-good-frame retention, and stale-result disposal. It protects independently
owned captures and caller-owned images through replacement, active turns and
teardown, including missing optional backs and failures in all six spread slots.
Spread checks cover dirty-slot coalescing, explicit in-place refresh, directional
readiness, adjacent-page backs, transparent overlays and renderer visibility.
Readiness regressions reject replaced snapshots, retain optional-back fallback,
and protect explicit captures from delayed automatic refreshes without losing
later edits. Native-layout waits cover cancellation, replacement, disconnect
and immediate hide/re-entry; reconnects preserve serialized capture cleanup.

Continuations run on one pumped UI thread. Cancellation tests distinguish a
cancelled caller wait from completion of serialized capture cleanup; disposal
checks await that cleanup. Settling loops stop after 600 frames. Both turn
directions cover foreign/owned pointer termination, duplicate events,
interruption, disconnected restart rejection and replacement turns started by
completion/cancellation subscribers. Hidden scenes reject pending captures even
when immediately re-entered. Delayed timer tests advance a monotonic clock and
check fresh, obsolete and chained animation timers.

## Native behavior and performance

`tests/PageCurl.Apple.Tests` uses actual native snapshotters, the GL renderer and
the linked demo handlers. It runs first-to-last-to-first navigation in all four
scenes through both wide and compact buttons, plus animated forward/backward
turns. Counters, completion status, endpoint buttons and both layouts must agree;
commands stay disabled during animation. Capture-boundary failures exercise
refresh, replacement/re-entry and resource teardown. Other assertions cover
snapshot color/alpha, custom-back landing orientation, shrinking meshes, native
hit targets, a 250 by 333 page, compact Options at 600 by 750, and light/dark
option-heading contrast of at least 4.5:1.

Window stop/resume notifications must cancel turns without changing navigation,
restore commands, pause video and resume only an active video scene.
Video paused before handler attachment must remain paused after native loading,
then play when explicitly activated. Layout-wait
probes cancel or disconnect unarranged content without false readiness or a
capture failure. Replacement spread pages must wait for native layout.

Timing probes switch immediately after real GL drag frames, during settling,
after completion and while spread capture is pending. Worker-queued probes
include time waiting for the UI handler, verify native capture overlap and record
actual release-to-request delay. Queue/handler latency must stay below 150 ms;
next dispatch and the visible Single scene's first paint must stay below 250 ms.
Destination snapshot readiness is recorded separately. Tests also prohibit idle
cancellation redraws and rebuilding unchanged Backside content.

Five live-counter inputs must produce one capture of the latest value. Exact
pixel and resolution comparison with an explicit native capture protects visual
quality. Gated providers cover content-kind/scene replacement and re-entry,
window stop, and input during an unfinished capture, including image disposal.
These checks distinguish queued demo updates from legitimate wrapper refreshes.

The native driver removes stale evidence, bounds execution and requires exit 0,
no failed assertions and the final `PASS all native suites` marker. Its tests
reject timeout, missing/partial output and contradictory exit/result cases.
`PAGECURL_TEST_OUTPUT` selects the fixture log; a native build alone does not run
its assertions. CI runs Core, isolated consumption and driver/replay tests on
Linux, plus neutral MAUI and native assertions on macOS, retaining failure logs.
The native host runs in Release with the demo's XAML source generation so timing
checks exercise the shipping configuration. Microsoft also recommends
[Release builds for profiling](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/profiling?view=net-maui-10.0).

## Visual and device checks

Reserve computer control for actual pointer routing, accessibility-tree
inspection and visible rendering/layout behavior that automated assertions
cannot establish. Load [pagecurl-checks.js](../tests/computer-control/pagecurl-checks.js)
in a runtime providing `cua`:

| Replay | Checks |
| --- | --- |
| `runPageCurlChecks(cua, absoluteAppPath, scenes?)` | Fresh AX navigation, counters, boundaries and compact Options; temporarily enables then restores reduced motion |
| `runPageCurlLiveContentCheck(cua, absoluteAppPath)` | Interactive counter, automatic and explicit refresh in either layout |
| `runPageCurlGestureCheck(cua, absoluteAppPath, from, to, counter, status)` | Real pointer input using points from a fresh screenshot |
| `runPageCurlAccessibilityCheck(cua, absoluteAppPath)` | Accessible names and roles; closes compact Options when it opened them |

Run `node --test tests/computer-control/pagecurl-checks.test.js` to validate the
replay, including false positives and failure cleanup. The [showcase skill](../.agents/skills/pagecurl-showcase-checks/SKILL.md)
provides the visual workflow. Inspect edges/corners, reversal, snap-back,
backward and gutter-crossing turns, all lock modes, large pages, light/dark,
compact/wide layouts, rotation, reduced motion, background/foreground, live
controls, local video/poster fallback, failure and repeated image replacement.
AX counters cannot establish pixel quality; visual changes need a reproducible
scenario or before/after evidence.

The iOS smoke records drags and extracts an active-gesture frame to expose
missing stationary pages, blank backs or underside bands. It terminates its app,
stops recording, removes temporary movies and shuts down simulators it boots,
also on failure. Existing simulator appearance and boot state are preserved.
Fixture passes do not satisfy physical-device or unavailable interaction gates.

## Distribution and documentation

Product builds pin the .NET 10 analyzer rules. Core uses Microsoft's
`recommended` mode; MAUI and the demo use `minimum`, which Microsoft recommends
for build enforcement, plus `recommended` globalization rules. Compiler and
enabled analyzer warnings fail these builds. Existing public `pointer` parameter
names retain named-argument compatibility through a localized CA1720 exception.
Test projects keep their existing analyzer settings so interface-contract tests
are not rewritten merely to satisfy concrete-type performance suggestions.
See Microsoft's [analysis modes](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview).

Packing Core and MAUI runs the SDK's [package validation](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/package-validation/overview).
MAUI also requires equal public APIs and parameter names across compatible
frameworks in each host's package. Android's generated resource class is internal;
it is not part of the controls' public contract. Cross-version compatibility
against a published baseline remains a release gate.

The distribution job combines unsigned macOS/Windows inputs, requires all five
frameworks and matching package/symbol/release identities, then restores an
isolated consumer. Tool tests reject mixed versions/revisions, missing assets,
conflicting shared files, malformed ZIP/OPC structure, unsafe/colliding paths,
signed inputs and stale output directories. Validated output is renamed from a
sibling temporary directory as one tree. Signing follows assembly. Inspection
requires platform assemblies, XML documentation and the Core dependency.

Verify runs pinned `actionlint`. Run the same check before pushing workflow changes:

```bash
go run github.com/rhysd/actionlint/cmd/actionlint@v1.7.12 -shellcheck= -pyflakes=
```

Script syntax and reference checks complement validation of GitHub expressions.
The documentation build treats DocFX warnings as errors and checks local links,
anchors, private source links and publication boundaries. See [documentation
publishing](documentation.md).

## Completed validation records

- [Microsoft guidance](validation/microsoft-guidance-2026-10-07/README.md): analyzer enforcement, strict package APIs and Release native assertions.
- [Runtime cleanup](validation/runtime-cleanup-2026-10-07/README.md): numerical edges, capture races, readiness and native video.
- [Requirements](validation/requirements-2026-10-05/README.md): failures, fixes, repeated runs and visible observations.
- [Test quality](validation/test-quality-2026-10-05/README.md): removed/strengthened tests and hosted Actions fixes.
- [Showcase polish](validation/showcase-polish-2026-10-05/README.md): page copy, contrast and replay assertions.
- [Production hardening](validation/production-hardening-2026-10-06/README.md): interruption, teardown, layout waits and navigation.
- [Public launch](validation/public-launch-review-2026-10-06/README.md): timers, window/video lifecycle, assembly and adoption.
- [Queued scene switching](validation/queued-scene-switching-2026-10-06/README.md): queue wait, first paint, coalescing and pixel equivalence.
- [Interaction and Rider](validation/rider-and-interaction-2026-10-04/README.md): regressions, platform builds and environment limits.
