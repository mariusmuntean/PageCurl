# Requirements and acceptance criteria

This is the current behavioral contract. Historical plans describe earlier
choices; they do not add requirements. “Implemented” means the behavior exists,
not that every platform or release gate has passed. Each change should cite the
requirement it affects and add a regression at the lowest layer that can prove it.

## Library requirements

| ID | Required behavior | Acceptance and automated coverage |
| --- | --- | --- |
| R01 | Keep interaction, navigation and geometry independent of MAUI, Skia and devices. | Core runs as a dependency-free console suite; adapters and the demo consume it without moving native code into Core. |
| R02 | Behave like stiff paper: preserve material distances, fixed hinges/pins and round curls. | `PageCurlGeometryTests` checks cylinder/cone lengths, stationary attachments, seam orientation and bounded coordinates. The minimum cylinder radius is 6% of the shorter page side. Mesh chords approximate curved material; existing length tolerances measure that discretization, not permission to stretch the analytic sheet. Visual quality still needs native inspection. |
| R03 | Follow the grabbed point directly; allow every free edge/corner and reject a locked edge or pinned point. | `PageTurnControllerTests`, `PageEdgeTests` and `PageAnchorTests` cover grab-relative zero progress, Auto corner resolution, reversal, scaling and invalid input. Two-argument `BeginDrag` retains legacy horizontal selection. |
| R04 | Use one completion path for gestures and commands, with threshold/flick decisions and exactly one completion per turn. | Controller tests cover commit/rollback, thresholds, velocity, duplicate release, reused controllers and a completion subscriber starting the next animated turn. `Tick` must keep requesting frames while that next turn settles. Reduced motion completes release and pointer cancellation immediately; explicit `CancelInteraction` always rolls back immediately. |
| R05 | Fit page content without distortion and select the correct navigation destination. | `PageAspectLayoutTests`, `PageViewportTests` and both navigation suites cover explicit/intrinsic/fallback ratios, pointer mapping and beginning/end boundaries. Single-page ratio 0 means intrinsic/fallback; spread ratio must be positive. |
| R06 | Keep caller-supplied images caller-owned; own only generated snapshots and drawing resources. | `PageSnapshotStateTests`, wrapper tests and native mesh assertions cover replacement, disposal, cancellation and teardown. An active turn retains its images until renderer assignments are cleared. Native texture/memory soak remains a hardware gate. |
| R07 | Refresh snapshots without publishing stale results or losing the last good frame. | Wrapper tests cover revisions, explicit in-place refresh, dirty-slot coalescing, independent front/back failure, superseded captures, cancelled caller waits and deferred cleanup. Calls run on the UI thread; caller cancellation may finish before internal capture cleanup. |
| R08 | Turn a physical spread across its spine in both directions. | Spread and geometry tests cover full travel, fixed spine and readiness. Forward uses Right as front, NextRight underneath, NextLeft as back; backward uses Left, PreviousLeft and PreviousRight. Omitted backs use the chosen fallback. Native tests verify readable landing orientation and stationary-page visibility. |
| R09 | Keep public control settings valid and consistent with their renderer. | `PublicApiTests` exercises invalid enums/numbers, retained last-valid values, finite dim-factor clamping, property forwarding and reduced-motion cancellation. Invalid enum and non-finite settings must not become stored values. Finite dim factors clamp to 0–1. MAUI binding rejection need not throw; the observable contract is unchanged valid state. |
| R10 | Keep the four-scene demo usable by gestures and labeled commands in compact and wide layouts. | Native demo assertions cover synchronous/animated commands, inactive completion, live snapshots, scene cancellation and resource recreation. Automatic and explicit refresh must report failure honestly and must not overwrite another scene's status, including after leaving and re-entering Content. Compact live content must keep the counter and action visible; the native fixture covers a 250×333 page. Computer-control journeys exercise the actual buttons, visible counters, boundaries, spread drags and live content. Accessibility-tree presence does not certify a screen-reader pass. |
| R11 | Bound mesh capacity and reuse warmed frame buffers. | Options reject more than 16,384 mesh cells. `PageTurnPerformanceTests` requires zero managed bytes in warmed 1,000-iteration controller/cylinder/cone/spread loops. Native frame/GPU performance is measured separately; no portable FPS guarantee is implied. |
| R12 | Ship packages that a fresh consumer can restore and compile against. | The isolated package-consumer check packs Core and MAUI, uses only `PackageReference`, resolves Core transitively, compiles Skia handler registration and representative wrapper/command APIs and executes Core behavior. It uses fresh package storage and no repository build properties. Neutral-package success is not native packaging or binary compatibility certification. |

Test classes are under [Core tests](../tests/PageCurl.Core.Tests/),
[neutral MAUI tests](../tests/PageCurl.Maui.Tests/) and
[native Apple tests](../tests/PageCurl.Apple.Tests/).
See [testing](testing.md) for test layers and [runtime contracts](runtime-contracts.md)
for ownership and threading details.

## Release requirements

These require evidence beyond implementation or a local desktop pass:

- Clean Android/iOS packaging in hosted CI and supported OS versions.
- Representative low-end Android and physical iOS performance, thermal behavior,
  frame pacing, and sustained texture/memory stability.
- VoiceOver and TalkBack use on hardware, plus Windows build and UI validation.
- Signing, archives, store validation, a published-version API compatibility
  baseline, and package publication rehearsal.

See [release readiness](release-readiness.md) and [compatibility](compatibility.md).
A build, an accessibility dump, a fixture invoking handlers, and actual pointer
input are different evidence; report them separately.

## Non-goals

PageCurl is not a reader, paginator, document loader, image cache, full cloth or
collision simulation, multi-sheet contact solver, or physically complete lighting
engine. The current stiff-sheet model is analytic cylinder/cone geometry. A new
simulation backend would need its own requirements and validation before replacing it.
