# Test quality and hosted verification — 5 October 2026

This audit follows the [requirements validation](../requirements-2026-10-05/README.md).
It strengthens behavioral evidence without changing the stiff-paper model or
adding public API surface.

## Audit decisions

- Removed two private IL call-name inspections and one check for an already
  deleted PNG helper. They constrained implementation shape without proving the
  required behavior. Native snapshot, rendered-mesh, lifetime, geometry and
  allocation regressions remain.
- Bounded neutral settling loops at 600 frames; reused the existing bounded Core
  helper for its remaining unbounded loop. Stalled completion now fails promptly.
- Added direct caller-image survival through replacement, cancellation and
  handler teardown (R06).
- Exercised actual spread activation handlers for owned/foreign pointers,
  release/cancellation, duplicate terminal events and both directions (R04/R08).
  Neutral controls receive explicit logical bounds; the native fixture separately
  verifies the UIKit hit target.
- Expanded numeric validation across all three controls, both infinities,
  threshold endpoints, intrinsic ratio, gutter bounds and renderer forwarding
  (R09). The wrapper's ratio is checked after layout because it is derived from
  the fitted page frame.
- Added native automatic-refresh failure and queued scene-reentry regressions
  (R10). Before the fix, the failure probe produced
  `FAIL live refresh hid capture failure: Live content updated`. Automatic and
  explicit refresh now share generation-guarded result reporting and preserve
  the last-good-snapshot failure status.
- Independent review found delayed capture failures crossing a hide/re-entry
  boundary. The [native red run](reentry-red.log) reproduced Spread status being
  replaced by `Using last snapshot`; a neutral wrapper regression reproduced one
  stale failure event instead of zero. Hiding now invalidates old capture
  revisions, preserves existing snapshots and makes spread slots dirty for the
  next presentation. Both delayed failures and stale successful-image disposal
  are covered (R07/R10).
- Compiled documented Skia handler registration in the isolated package consumer
  (R12), and corrected README custom-backside examples and setup instructions.

## Completed checks

| Check | Result |
| --- | --- |
| Core Release runner | [12 groups passed](core-tests.log) |
| Neutral MAUI Release runner | [5 groups passed](maui-tests.log) |
| Validation tooling | [10 tests passed](tooling-tests.log), including stock Unix PATH coverage |
| External package consumer | Fresh local packages/cache restored and compiled; transitive Core command/mesh ran |
| Native Mac Catalyst fixture | [All suites passed](native-assertions.log), including refresh and visibility regressions |
| Workflow project references | All referenced projects exist |

Environment: .NET SDK 10.0.401, workload set 10.0.401.1, Xcode 27.0;
Apple version checking enabled. A local parallel MSBuild run failed with MSB4166
worker-node exits. Completed serial builds succeeded; no source workaround was
introduced for that toolchain failure.

## Hosted Actions fixes and evidence

The Linux Core job initially failed because the repository's native workload
pin was resolved on a neutral-only host. That job now disables workload
resolution. The next run exposed an undeclared `rg` dependency in the workflow
checker; the checker now uses standard `grep`, with a portable-PATH regression.
The package consumer already packs/builds both neutral packages, so redundant
steps were removed.

[Verify run 37283932941](https://github.com/mariusmuntean/PageCurl/actions/runs/37283932941)
on commit `57ea2c6` completed successfully: Linux Core/tooling/package consumer,
macOS Core/MAUI tests, Android/iOS simulator/Mac Catalyst builds, and native
fixture assertions. Historical failed runs remain as diagnostic evidence. GitHub links require access
to this private repository; local assertion logs above remain available in the
checkout.
[Run 37285777668](https://github.com/mariusmuntean/PageCurl/actions/runs/37285777668)
on `4c564bb` also passed both jobs with the expanded test audit and refresh fix.
Subsequent revisions require their own green hosted run before reporting completion.

The earlier record contains actual computer-control journeys for commands,
live content, compact options and pointer drags. The rebuilt Mac showcase was also exercised through computer control: the live
counter changed from 0 to 1 with `Live content updated`, then the actual Refresh
button produced `Snapshot refreshed`. Task-started demo processes were closed.
This audit's added failure checks are injected native/neutral regressions, not
physical-device interactions.
Custom snapshotters must complete their task and cooperate with cancellation;
an unfinished provider can delay serialized spread refresh. This is an explicit
provider contract, not a hard capture timeout guarantee.
Physical iOS/low-end Android performance, Windows and hardware screen-reader
certification remain [release gates](../../release-readiness.md).
