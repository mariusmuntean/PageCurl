# Library and demo review — 2026-09-15

## Scope and disposition

Reviewed `PageCurl.Core`, the MAUI renderer/content/spread adapters and native
snapshot implementations, demo navigation/resource/media paths, their current
changes, and relevant tests. Baseline is HEAD `80bc1a3`; the reviewed proposed
change is the pre-existing working-tree diff, not a new commit.

**Disposition: the current patch is not ready to merge.** Existing tests and
builds pass, but targeted reproducers expose missing behavior coverage. No
library, demo, or existing test source was modified by this review.

- [Fix plan](../../superpowers/plans/2026-09-15-library-demo-review-fixes.md)
- [Progress and resume instructions](../../superpowers/plans/2026-09-15-library-demo-review-progress.md)
- [Original working-tree inventory](initial-status.txt)
- [Original source diff](initial-source.diff)

## Confirmed regressions

| ID | Priority | Location | Finding and evidence |
| --- | --- | --- | --- |
| R1 | P1 | `PageTurnView.OnTouch`, line 611 | Unconditional synchronous append to `/tmp/touch-trace.log` runs on every touch before even checking handler state. A missing/unwritable directory throws out of input dispatch; a writable path still introduces string allocation, file opens and unbounded logging. Violates the hot-path requirement in [AGENTS.md, line 13](../../../AGENTS.md#L13). Static verification; no mobile crash was deliberately induced. |
| R2 | P1 | `PageContentSnapshotter.Apple.cs`, lines 34–39 | Native renderer output is assumed to be BGRA8888 without format or color-space conversion. The native [probe](apple-pixels.m) produced grayscale+alpha, RGBA8888, and 64-bit extended color. Plain red has `ff 00 00 ff` bytes, which BGRA reads as blue; grayscale rows can be too short. See [native results](apple-pixels.log). |
| R3 | P2 | `PageTurnController.UpdateDrag`, lines 205–211 | Grab-relative controller progress is passed to `PageCurlGeometry.ComputePose`, which still calculates edge-relative pointer progress. On a 400×500 page, grab (200,250), drag to (150,250), left hinge: current pose is (180,250), not (150,250). This is below the physical curl limit. [Current](repro.log) versus [baseline](baseline-repro.log). |
| R4 | P2 | `PageSpreadView.RefreshAfterAsync`, line 457 | Completing an old capture overwrites the dirty bit set by a newer content assignment. Queued refresh skips that slot and enables navigation with an old image. Reproducer: current content width 22, stored image width 11, readiness true. Baseline cancels the superseded capture and stores width 22. |
| R5 | P2 | `PageSpreadView.RefreshAfterAsync`, lines 441–445 | After initial capture, mutating a child of the same content view and calling public `RefreshSnapshotAsync` performs no capture. There is no public invalidation alternative. Reproducer retains capture count 4; baseline advances to 8. Keep automatic refresh coalescing without removing explicit refresh semantics. |
| R6 | P2 | `PageSpreadView.PrepareTurn`, lines 737–739 | Removing `NextLeftContent`/`PreviousRightContent` while an unrelated capture holds the refresh queue can leave the direction ready but selects the old cached back image and forces CustomContent. Reproducer removes the forward back content, then successfully prepares a turn with a non-null back image. Omission must immediately fall back or block until cleared. |

All C# reproductions and their supporting test doubles are in
[repro/Program.cs](repro/Program.cs). They intentionally print observed failures
and exit normally: **exit 0 means the probe ran, not that the observed behavior
is correct**. Convert them into asserting regression tests during implementation.

## Existing issues and simplicity/performance backlog

These do not count against the proposed patch because baseline comparison or
source history shows they already exist.

### B1 — Preserve snapshots for the complete active turn (P1)

An asynchronous refresh can start with the left slot, then a ready forward
turn starts. Replacing right content while the left capture is suspended lets
the running loop replace/dispose the right image while the overlay still uses
it. Both current and baseline probes report `state=Settling`, image handle zero,
and the overlay retaining that disposed image. Windows capture naturally
suspends; injected snapshotters can do so on every target. Readiness checks at
refresh entry are not an image-lifetime guarantee. Add before/after-await turn
checks or defer publishing/disposal until the turn ends; prefer a small explicit
state machine over additional loosely related boolean flags.

### B2 — Avoid unused demo PNG encoding (P2, optimization)

`DemoPageSet.Create` eagerly PNG-encodes all eight generated images and retains
those byte arrays; `GetImageSource` has no call sites in source or tests. The
present demo uses SKImages for Single and new MAUI `DemoPageView`s for Spread
and Backside. One cold host probe measured **317.5 ms total Create time**,
422,840 managed bytes allocated, and **385,992 bytes of unused PNG payload**.
The total includes drawing and initial native/font setup, so it is not an
isolated measurement of PNG encode time. Remove or make encoding lazy, then
measure before/after using the same host and sequence. Do not redesign the
scene model or add a cache service to solve an unused-data problem.

### B3 — Finish demo lifecycle cleanup (P2 follow-up)

`ClearAllContent` clears four spread slots but not the two new sheet-back slots.
Include all six in the same teardown path and test repeated disappearance and
reappearance. Also inspect delayed snapshot retry and interactive callbacks
across disappearing/handler teardown before changing behavior. The omitted
slots are statically confirmed; retained-memory growth and callback crashes
were **not** established in this run, so this is not an inline regression claim.

### B4 — Respect synchronous command completion (P3)

With Reduce motion enabled, Single advanced from 01/08 to 02/08 but the demo
status remained “Turning”. `RequestTurn` writes that status after `TryTurn`
has synchronously raised completion. Both navigation buttons were usable
again once the queued refresh updated them; this was **not** a permanently
stuck-navigation reproduction. Derive post-command status from actual state,
or set transient state before invoking the command. Preserve the shared
completion path ([AGENTS.md, line 15](../../../AGENTS.md#L15)).

### Further evidence gates, not asserted bugs

- Profile complete native paint/upload work before changing mesh defaults.
  One warmed 64×64 cylinder pose took 0.973 ms/build over 1,000 builds with
  zero managed allocations on this host. This does not include Skia draw,
  GL upload, native allocations, or other poses/devices.
- The renderer fills and submits full-capacity triangle arrays, including
  unused degenerate tails. Measure native bandwidth and allocation impact;
  do not introduce unsafe buffers or a renderer rewrite without evidence.
- Validate video capture/poster behavior and unavailable playback on real
  targets; playback failure fallback was inspected, not reproduced.
- Verify cancellation/reconnect, compact layouts, all anchors, reduced motion,
  accessibility, and physical-device behavior after fixes. No full visual or
  release-readiness certification is implied by this review.

## Completed validation

Commands used the live-verified SDK `/Users/marius/.dotnet/dotnet` (10.0.301).
The general routing remains in the repository validation skill; these are
records of this particular run.

| Check | Result | Evidence |
| --- | --- | --- |
| Core Release runner | PASS, all 12 groups | [core-tests.log](core-tests.log) |
| Neutral MAUI Release runner (`-f net10.0`) | PASS, all 4 groups | [maui-tests.log](maui-tests.log) |
| Neutral library Release build (`-f net10.0 -p:TargetFrameworks=net10.0`) | PASS, 0 warnings/errors | [neutral-build.log](neutral-build.log) |
| Mac Catalyst demo Debug build (`-f net10.0-maccatalyst`) | PASS, 0 warnings/errors | [maccatalyst-build.log](maccatalyst-build.log) |
| Android demo Release build (`-f net10.0-android`) | PASS, 0 warnings/errors | [android-build.log](android-build.log) |
| Current behavior probe | Completed, defects observed | [repro.log](repro.log) |
| HEAD comparison probe | Completed, isolates regressions and B1 | [baseline-repro.log](baseline-repro.log) |
| UIKit pixel-layout native probe | Completed | [apple-pixels.log](apple-pixels.log), [build log](apple-pixels-build.log) |
| Mac Catalyst UI spot checks | Completed, limited scope | Freshly launched built app; Single reduced-motion command reached 02/08 with stale “Turning” status; Spread command reached 03–04/08 with “Turn complete” and both buttons available. Screenshots were inspected in-session, not exported. Later independent UI state changes were not attributed to the code or used as defect evidence. |
| `git diff --check` | PASS | No whitespace errors at checkpoint. |

Not run: iOS simulator smoke, physical iOS/Android execution, Windows build or
runtime, full frame-time/native-memory profiling, long-duration soak. Windows
needs a Windows host; the others remain explicit follow-up work, not verified.

The renderer-only UIKit probe succeeds without an application host. An attempted
extension using `UIView.DrawViewHierarchy` failed with an application-host
precondition (`NSApplication has not been created yet`); see
[failed hostless extension](apple-hierarchy-probe-failure.log). That failure is
not a product defect. The saved probe was returned to its renderer-only scope
and rerun successfully; full MAUI native pixel assertions remain in Task 2.

The first baseline-harness attempt accidentally included archived child source
through default globs, causing harness compilation errors. The harness was moved
to a sibling directory, its project reference was corrected, and the completed
rerun above passed. No repository implementation was changed for that failure.

## Reproduce the targeted checks

```sh
/Users/marius/.dotnet/dotnet run --project docs/validation/review-2026-09-15/repro/ReviewRepro.csproj -c Release -f net10.0 -p:TargetFrameworks=net10.0

SDK="$(xcrun --sdk macosx --show-sdk-path)"
mkdir -p .artifacts/review-2026-09-15
xcrun clang -fobjc-arc -target arm64-apple-ios15.0-macabi \
  -isysroot "$SDK" \
  -iframework "$SDK/System/iOSSupport/System/Library/Frameworks" \
  -F"$SDK/System/iOSSupport/System/Library/Frameworks" \
  -framework UIKit -framework Foundation -framework CoreGraphics \
  docs/validation/review-2026-09-15/apple-pixels.m \
  -o .artifacts/review-2026-09-15/apple-pixels
.artifacts/review-2026-09-15/apple-pixels
```

Baseline archive and harness are local ignored artifacts under
`.artifacts/review-2026-09-15/baseline/`. The authoritative comparison result is
preserved above; reconstruct from `git archive 80bc1a3 src` if those temporary
files are removed. Do not reset or stash the user's working tree to reproduce it.
