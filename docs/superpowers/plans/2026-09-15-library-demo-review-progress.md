# Library and demo review progress

## Current checkpoint — 2026-10-04

All nine implementation tasks are finished. The final independent review has no
remaining Important or Critical findings. Tests cover queued cancellation,
Retina coordinate scaling, consumed release/cancel events, stale captures,
active-turn image ownership, explicit refresh, and demo lifecycle behavior.

See the [implementation evidence](../../validation/review-fixes-2026-10-04/README.md)
for completed commands and platform limitations. The demo is visibly running
and a Next command advanced it from 01 / 08 to 02 / 08 with Turn complete.
Changes remain uncommitted alongside the preserved pre-existing working patch.

The next release work is a supported Apple toolchain run, complete iOS scripted
interaction evidence, and the outstanding device, Windows, accessibility and
soak gates. The historical review below describes the September baseline,
not the current implementation status.

## Historical checkpoint — 2026-09-15

### Resume instructions at that checkpoint

1. Read the [review record](../../validation/review-2026-09-15/README.md).
2. Read the [prioritized fix plan](2026-09-15-library-demo-review-fixes.md).
3. Inspect `git status --short`. Preserve the changes in the
   [initial inventory](../../validation/review-2026-09-15/initial-status.txt).
4. Run the existing test suites and saved behavior probe before editing.
5. Start Task 1 (touch-path filesystem removal), then Task 2 (Apple format
   conversion). For asynchronous snapshotter/Windows shipping, prioritize
   Task 7's existing active-turn image-lifetime defect before the P2 items.

**Implementation has not started.** No production source, existing tests,
staging, commits, pushes, stashes, or resets were performed by this review.
The only new deliverables are these plan/progress files and the dated evidence
folder. Local build products and the HEAD comparison live in ignored folders.

Baseline: HEAD `80bc1a3` plus the pre-existing working-tree source diff.
The final source diff was compared byte-for-byte with the saved initial source
diff and matched. Review additions are uncommitted so they can be inspected
alongside the existing work without mixing ownership in a commit.

## Stage status

| Stage | Status | Evidence / next action |
| --- | --- | --- |
| Inventory, guidance, source/diff review | finished | Core geometry/controller/navigation, MAUI renderer/snapshot/content/spread/native adapters, demo navigation/resource/media code and tests inspected. Root instructions apply; no scoped override found. |
| Baseline tests and neutral build | finished | 12 core groups, 4 MAUI groups; neutral Release build, zero warnings/errors. Logs in review folder. |
| Targeted regression investigation | finished | Six actionable regressions (R1–R6); reproductions distinguish old snapshot disposal issue B1 from new failures. Native renderer byte-layout probe completed. |
| Demo simplicity/performance first pass | finished | Unused PNG payload confirmed; resource creation and warmed core timings recorded. Lifecycle cleanup and immediate-completion status have explicit follow-up tasks. |
| Native build checks | finished | Mac Catalyst Debug and Android Release both pass, zero warnings/errors. |
| Native runtime spot checks | finished | Limited Mac Catalyst navigation and reduced-motion status checks; renderer-only UIKit format probe. No broad visual certification. |
| Full native release/device/performance matrix | not_started | iOS smoke, Windows runtime, physical devices, native paint/memory profiling, video fallback and soak still required. |
| Prioritized written plan | finished | Nine bounded tasks, regression sources, acceptance criteria, validation routing, and release follow-through saved. |
| Implementation | not_started | Begin Task 1 after revalidating current working-tree state. |

## Task status

| Task | Priority / origin | Status | Next action |
| --- | --- | --- | --- |
| 1: Touch logging | P1 / R1 | not_started | Add input-path regression, remove temporary synchronous append. |
| 2: Apple pixels | P1 / R2 | not_started | Capture colored/grayscale content in a real native test host; normalize output format. |
| 3: Grab coordinates | P2 / R3 | not_started | Add the saved 400×500 interior-grab assertion to the core runner. |
| 4: Slot revisions | P2 / R4 | not_started | Convert STALE probe into assertions for newer content/size invalidation. |
| 5: Explicit refresh | P2 / R5 | not_started | Assert an in-place label change recaptures via the public API. |
| 6: Removed backs | P2 / R6 | not_started | Convert REMOVED_BACK probe; require immediate fallback or blocked readiness. |
| 7: Active image lifetime | P1 / existing B1 | not_started | Convert DURING_TURN probe; retained overlay images must not be disposed. |
| 8: Demo resources/lifecycle | P2 / B2–B3 | not_started | Verify unused encoding call sites and all-six-slot teardown. |
| 9: Immediate completion UI | P3 / existing B4 | not_started | Assert synchronous completion does not leave the status as Turning. |

## Evidence caveats

- Probe exit 0 means it ran; its output deliberately includes failing product
  observations. Do not report these as passing regression tests.
- A failed native hostless hierarchy extension is recorded separately from the
  successful renderer-only format probe; native application-host assertions are
  still needed. No product source was changed to work around the host failure.
- Initial archived-baseline harness compilation failed because of harness
  layout/project references. The corrected final run completed and is saved.
- One cold demo resource run and one warmed core pose are not a native frame
  benchmark. No before/after optimization was applied or measured.
- Mac Catalyst UI state later changed independently of the scripted scenario;
  no additional bug or navigation count was inferred from that change.
- The review did not wait to consume an arbitrary quota. Quota remaining is not
  observable; this is a resumable completed review/plan checkpoint, not a claim
  that five hours elapsed or the account allowance was exhausted.

## Handoff summary

Current patch: **not ready to merge**. Existing suites and build targets pass,
but six regressions need fixes. The separate existing active-turn image-lifetime
issue also deserves priority. Continue with small test-first changes, preserve
all unrelated edits, and replace each task's next action with completed-command
logs as work progresses.
