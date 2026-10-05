# Library and Demo Review Fixes Implementation Plan

> **For implementers:** Use the executing-plans workflow to implement and review one task at a time. Check off steps only after the corresponding evidence is complete.

**Goal:** Fix the verified library regressions, then address the existing snapshot lifetime issue and narrowly scoped demo improvements.

**Architecture:** Retain the pure geometry/controller core, the MAUI snapshot adapters, and application-owned demo resources. Use explicit snapshot revision and turn-lifetime rules instead of extending the current task-chain/boolean interactions indiscriminately. Avoid a renderer rewrite, generic caching framework, or public API expansion unless a regression demonstrates the need.

**Tech Stack:** .NET 10.0.301, MAUI 10, SkiaSharp.Views.Maui.Controls 4.150.1, UIKit, dependency-free core and neutral MAUI test runners.

**Spec:** [Verified review findings and evidence](../../validation/review-2026-09-15/README.md).

## Global constraints

- Preserve all pre-existing working-tree changes; do not reset, stash, or stage unrelated work.
- Keep geometry/interaction free of MAUI, SkiaSharp, dispatchers, and devices.
- Avoid per-frame allocations and unbounded mesh growth.
- Keep supplied SKImages caller-owned; snapshots remain wrapper-owned and alive for every active consumer.
- Gesture and accessibility commands use the same one-shot completion path.
- Keep the small public API and stiff-paper, non-stretching behavior.
- Add deterministic regression coverage before each behavior fix.
- Use explicit platform frameworks and the repository validation skill.
- Record only completed validation; do not carry historical successes forward as new evidence.
- Implementation has not started. The review did not stage, commit, push, or fix code.

## How to resume

Read [progress](2026-09-15-library-demo-review-progress.md), the review record, and
root `AGENTS.md`. Run `git status --short` and compare with the recorded inventory.
Re-run the existing suites and the targeted probe before editing. The probe
prints failures; convert observations into assertions rather than treating its
exit code as a test result. Start with Task 1, then Task 2; B1 may be prioritized
before the P2 snapshot tasks if shipping Windows or asynchronous snapshotters.

Each task below is independently reviewable. Keep every task's state as
`not_started`, `in_progress` (with next action), or `finished` (with log paths)
in the progress document. Do not batch unrelated fixes in one commit. Commit
only after reviewing the staged diff and the existing uncommitted ownership.

---

## Task 1 — Remove filesystem work from touch dispatch (R1, P1)

**Files:** `src/PageCurl.Maui/PageTurnView.cs`; adapter/input regression coverage
under `tests/PageCurl.Maui.Tests/`; `docs/testing.md` if adding a new coverage category.
**Interface:** No public changes; preserve touch ownership, cancellation, and completion.

- [ ] Reproduce the existing unconditional append by inspecting `OnTouch`; identify it before any handler-state guard.
- [ ] Add a regression check that the default touch path does not depend on a writable filesystem. Exercise press/move/release/cancel on a target where `/tmp` is unavailable; do not create `/tmp` as a workaround.
- [ ] Remove the temporary append. Do not replace it with per-event logging or simply catch and ignore I/O exceptions. Existing opt-in performance diagnostics remain the diagnostics surface.
- [ ] Run neutral MAUI tests/build and explicit Android Release build. Exercise a mobile drag when a runtime is available, otherwise keep the runtime gate open.
- [ ] Record the outcome and review this narrow diff before an individual commit.

## Task 2 — Normalize Apple snapshot pixel layout (R2, P1)

**Files:** `src/PageCurl.Maui/PageContentSnapshotter.Apple.cs`;
`docs/validation/review-2026-09-15/apple-pixels.m` as existing reproduction;
a native capture regression fixture; `docs/platform-notes.md`.
**Interface:** Preserve `IPageContentSnapshotter.CaptureAsync(View, CancellationToken)` and caller-visible cancellation behavior.

- [ ] Run the recorded native probe. Verify grayscale, red RGBA, and extended-color results in its log instead of assuming the simulator/device uses one layout.
- [ ] Add a native fixture that captures solid red, blue, white, grayscale, half-transparent color, and Display-P3 MAUI content through the actual snapshotter. Assert non-null images, correct dimensions, red/blue channel identity, expected alpha, and readable color after conversion. White content must not fail due to row stride.
- [ ] Choose one explicit destination format/color space and convert using a correctly configured bitmap context. Match the resulting channel order and alpha semantics to `SKImageInfo`; do not reinterpret renderer bytes, and do not restore PNG encoding merely to conceal the issue.
- [ ] Preserve cancellation checks and dispose all temporary native buffers/contexts; keep exactly one owned returned SKImage.
- [ ] Run explicit Mac Catalyst build and native fixture, then `tools/ios-smoke.sh` for simulator evidence. Mark physical-device color behavior unverified until exercised.
- [ ] Update platform notes with the actual conversion contract and record an independently reviewed commit.

## Task 3 — Align controller and geometry progress coordinates (R3, P2)

**Files:** `src/PageCurl.Core/PageTurnController.cs`,
`src/PageCurl.Core/PageCurlGeometry.cs`,
`tests/PageCurl.Core.Tests/PageTurnControllerTests.cs`,
`tests/PageCurl.Core.Tests/PageCurlGeometryTests.cs`,
`docs/interaction-and-rendering.md`.
**Interface:** Keep `GetCurlPose`/`ComputePose` signatures unless a proven compatibility constraint requires otherwise.

- [ ] Add a failing regression based on the complete GRAB block in the saved probe. The essential assertion is:

```csharp
var controller = new PageTurnController(new PageCurlOptions { FlickEnabled = false });
var size = new Vector2(400, 500);
TestAssert.True(controller.BeginDrag(new Vector2(200, 250), size, PageAnchor.Left));
controller.UpdateDrag(new Vector2(150, 250));
TestAssert.InRange(controller.GetCurlPose(size).Position.X, 149.99f, 150.01f);
```

- [ ] Extend coverage to all four free edges, diagonal interior grabs, unchanged-pointer samples, and committed/rollback settling. Confirm the sampled mesh grab vertex, not just the controller progress, stays attached while physically reachable.
- [ ] Make pose interpolation use the same grab-relative normal travel as the controller. Handle zero travel without moving an idle grab toward an outer edge. Retain bounded motion beyond the physical curl limit and preserve existing edge-start/programmatic behavior.
- [ ] Run the full core Release runner, including warmed allocation tests. Compare the current GRAB probe with the baseline observation.
- [ ] Inspect a native interior drag and rollback; update the interaction contract only after observed behavior matches it, then review and commit.

## Task 4 — Reject capture completion for an obsolete slot revision (R4, P2)

**Files:** `src/PageCurl.Maui/PageSpreadView.cs`,
`tests/PageCurl.Maui.Tests/PageSpreadViewTests.cs`, `docs/runtime-contracts.md`.
**Interface:** Preserve public content properties and readiness notifications; internal capture revision bookkeeping may change.

- [ ] Port the complete STALE scenario and `DelayedSnapshotter` from the saved probe into an asserting neutral MAUI test. Capture width 11, replace the content with width 22 while suspended, queue another refresh, then complete the old capture.
- [ ] Assert the slot remains unready until width 22 is captured, and assert the final stored image is width 22. Also cover removal and a size change while capturing the same view reference.
- [ ] Track a per-slot invalidation revision captured before the await; only acknowledge or publish results if that revision and lifecycle still match. A bool alone cannot distinguish an older capture from newer invalidation. Dispose rejected results without replacing the last good current frame.
- [ ] Cover stale failure after newer success, cancellation, teardown/reconnect, and both current/destination/back-face slots. Retain independent-direction readiness when safe.
- [ ] Run neutral MAUI tests/build and the probe. Document versioned freshness and review this fix independently of the public refresh behavior below.

## Task 5 — Preserve explicit refresh semantics (R5, P2)

**Files:** `src/PageCurl.Maui/PageSpreadView.cs`,
`tests/PageCurl.Maui.Tests/PageSpreadViewTests.cs`, `docs/runtime-contracts.md`,
`docs/testing.md`.
**Interface:** Public `RefreshSnapshotAsync(CancellationToken)` must refresh in-place content changes; automatic property/layout refresh remains coalesced.

- [ ] Port IN_PLACE from the probe: capture four Label views, mutate the right label text without replacing it, call public refresh, and assert a new snapshot is captured for the updated content.
- [ ] Add a separate automatic-refresh test proving that replacing just one content property does not recapture unchanged slots. Do not keep the current public-method test as proof of automatic coalescing.
- [ ] Separate explicit invalidation from the internal dirty-slot drain. Use the existing public method for explicit invalidation rather than adding an unneeded force flag or another public refresh API. Preserve force intent if a turn is active.
- [ ] Verify repeated automatic requests coalesce, explicit refresh retries failures, cancellation propagates, and dynamic content becomes visible on the next curl.
- [ ] Run neutral MAUI tests/build, update the exact public/automatic contract, and review and commit.

## Task 6 — Stop selecting removed optional sheet backs (R6, P2)

**Files:** `src/PageCurl.Maui/PageSpreadView.cs`,
`tests/PageCurl.Maui.Tests/PageSpreadViewTests.cs`.
**Interface:** Omitted sheet-back content uses configured mirrored/solid fallback.

- [ ] Port REMOVED_BACK from the probe. Keep an unrelated left capture pending, remove `NextLeftContent`, queue its clear, then attempt a ready forward turn.
- [ ] Assert either the turn waits for the clear or it starts with `BackImage == null` and the configured fallback mode. Mirror the scenario for `PreviousRightContent` and backward turns.
- [ ] Make readiness and `PrepareTurn` agree about omitted content. Do not select an old cached custom image merely because it has not yet been cleared by the queue; do not dispose an image already used by a turn.
- [ ] Test solid and mirrored fallback, re-addition, capture failure, and both directions. Run neutral MAUI tests/build and review the change.

## Task 7 — Protect active-turn snapshot ownership (B1, existing P1)

**Files:** `src/PageCurl.Maui/PageSpreadView.cs`,
`src/PageCurl.Maui/PageSnapshotState.cs` only if ownership requires it,
`tests/PageCurl.Maui.Tests/PageSpreadViewTests.cs`, `docs/runtime-contracts.md`.
**Interface:** Owned images assigned to `Overlay` remain valid until unassigned.

- [ ] Port DURING_TURN from the probe and use the existing reflection/controller helpers to avoid requiring a dispatcher in neutral tests.
- [ ] Assert `turningImage.Handle != IntPtr.Zero` throughout settling, even after the previously started capture completes and right content changes. Confirm this currently fails on both HEAD and the working tree.
- [ ] Suspend capture publication/replacement while any turn references those slots. Re-check turn/lifecycle state after awaited work, not only when entering the refresh method. Resume pending work after completion/cancellation with current content revisions.
- [ ] Add backward-turn, sheet-back, destination, cancellation, handler-teardown, and asynchronous failure variants. Ensure callers can await the eventual refresh without hanging.
- [ ] Run the neutral MAUI suite/build and a Windows native test when a Windows host is available. Record the host limitation rather than claiming cross-platform verification.
- [ ] Document clear-before-dispose ordering and review and commit separately from regressions.

## Task 8 — Simplify demo resources and lifecycle (B2/B3)

**Files:** `src/PageCurl.Demo/DemoPageSet.cs`,
`src/PageCurl.Demo/MainPage.xaml.cs`, demo regression coverage,
`docs/testing.md` if adding a dedicated fixture.
**Interface:** Preserve the demo's eight visible pages, scene behavior, and image ownership.

- [ ] Search every call site of `GetImageSource` again. If still unused, remove eager PNG storage and its unused helper rather than adding a new cache abstraction; if a consumer was added, encode lazily on that path.
- [ ] Measure cold resource creation before/after using the same host; report total creation time separately from any isolated encoding measurement. Keep high-resolution page 7 intact.
- [ ] Extend `ClearAllContent` to clear both new back-face content properties alongside the original four. Add a disappearance/reappearance regression that asserts all six are detached and newly created resources work after return.
- [ ] Test a queued retry/interactive refresh that resumes after disappearance. Add lifecycle guards only for demonstrated stale callbacks; avoid broad exception suppression.
- [ ] Run explicit Android and Mac Catalyst demo builds; spot-check all four scenes and repeated navigation. Record memory observations without claiming an unmeasured leak fix.
- [ ] Review and commit the resource simplification and lifecycle fix separately if their tests are independent.

## Task 9 — Preserve completion status for immediate turns (B4, P3)

**Files:** `src/PageCurl.Demo/MainPage.xaml.cs`, demo command regression coverage.
**Interface:** Same gesture/programmatic completion event; no new navigation state framework.

- [ ] With Reduce motion enabled, invoke Next from Single 01/08 and verify the current incorrect post-command “Turning” status after reaching 02/08.
- [ ] Add a test/fixture with synchronous completion during `TryTurn`; verify the completed status and correct button availability survive the command return.
- [ ] Set transient state before command execution or consult actual controller state afterward, without overwriting completion. Preserve normal animated command behavior and failed-start handling.
- [ ] Validate Single, Spread, Content and Backside reduced-motion commands and ordinary animated commands; record the UI evidence and review the narrow change.

## Release and performance follow-through

- [ ] Re-run all core and neutral MAUI checks after the fixes, not only targeted tests.
- [ ] Build explicit Android and Mac Catalyst targets; run iOS simulator smoke.
- [ ] Use a Windows host for asynchronous native snapshot/lifetime regressions.
- [ ] Collect complete-frame timing/native-memory evidence before changing full-capacity draw buffers or mesh resolution. Preserve zero-allocation warmed core paths.
- [ ] Review all docs against observed behavior, including the existing README 64×64 versus interaction-page 48×48 mismatch; this documentation discrepancy is not a functional patch finding.
- [ ] Review staged files for unrelated pre-existing edits and record per-task completion evidence. Do not mark unavailable physical-device, long-soak, or video gates as passed.
