# Runtime readiness and cleanup — 7 October 2026

This review preserves the public library API, mesh quality, native capture
resolution and caller-owned images. Changes have local regression evidence;
they do not establish the remaining hardware or distribution gates.

## Fixes and regression evidence

- Core handles extreme finite aspect-fit sizes and spring frequencies, and the
  final spread of an `int.MaxValue` page count, without overflow or extra completion.
- Snapshot replacement preserves reentrant cancellation ownership. Spread
  reconnects wait for unfinished provider cleanup before another capture.
- Curl commands and touches reject stale front snapshots. Layout waits end on
  cancellation, disconnect and hide/re-entry; replacing content follows its layout.
- Delayed automatic captures preserve explicit requests and later edits.
  Overlapping completion cannot leave a completed predecessor blocking refresh.
- Unavailable optional backs use fallback instead of stale page textures.
  Same-content last-good images remain usable.
- Video pause intent survives attachment. The native regression first failed
  against the original implementation after checking actual `AVPlayer` readiness
  and rate; the public media state alone had missed playback. The
  [pinned toolkit source](https://github.com/CommunityToolkit/Maui/blob/37e20e14b232cbb80d54d5e7c03e8a553babece1/src/CommunityToolkit.Maui.MediaElement/Views/MediaManager.macios.cs)
  also checks `ShouldAutoPlay` when loading the source.

The demo failure/re-entry tests now use actual Add one events. This removes a
test-only production timing parameter. Shared test and package helpers replace
duplication, and the starter documents cancellation, window lifecycle and retry.

## Completed local checks

| Check | Result |
| --- | --- |
| Core Release runner | All 12 groups, including warmed allocation guards |
| Neutral MAUI Release runner | All 5 groups |
| Python tooling tests | 52 passed |
| Native Mac Catalyst assertion host | 110 PASS outcomes; exit 0 and final suite marker |
| Demo Mac Catalyst Release | Built with supported Xcode; no version override |
| Demo Android Release | Built with zero warnings/errors |
| Demo iOS simulator Release | Built for `iossimulator-arm64`; no version override |
| Documented quick start | Unchanged XAML/code-behind compile; zero warnings/errors |
| Fresh local packages and isolated consumer | Neutral packages/symbols checked; transitive Core and representative API calls passed |
| Documentation site | DocFX zero warnings/errors; links, anchors and publication boundaries passed |

Behavior changes have failing-before/passing-after regressions. Local logs are retained under
`.artifacts/readiness-*`. Initial native failures exposed stale test setup:
journeys now wait for automatic snapshot readiness before starting a turn.
The measured delays after interaction and timing limits are unchanged.

## Native timing and quality

The final run issued scene requests 0/8/16/24/40/80 ms after rendered drag
release, during settling and between captures. Maximum queue delay was 61.5 ms,
handler duration 43.7 ms, next dispatch 103.6 ms and first paint 90.4 ms.
Queue/handler limits remain 150 ms; dispatch/paint limits remain 250 ms.
Destination readiness now includes current snapshots, not only boundary buttons.

Five live inputs produced one latest-value capture. Exact native pixels and
resolution, full navigation in both layouts, ownership, shrinking meshes,
window lifecycle and native video assertions passed. No computer control was used.

These timings describe this local Mac Catalyst run. Physical devices, screen
readers, Windows runtime, signing, hosted distribution and public package
consumption still require their respective [release gates](../../release-readiness.md).
