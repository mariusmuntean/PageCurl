# Interaction and rendering quality — 2026-10-04

The [later interaction and Rider review](../rider-and-interaction-2026-10-04/README.md)
supersedes this record for diagonal geometry, immediate-switch timing and the
supported Xcode 27 toolchain. The results below retain the earlier run.

This review addresses the reported Single-to-Spread freeze, incomplete travel
across the spine, unrelated backside content, and page-boundary clipping.
Pre-existing uncommitted work was preserved. No commits or integration actions
were performed. Independent read-only review found no remaining important
findings in the final implementation.

## Changes and regression coverage

- Full spread turns reflect the sheet across its fixed spine in both directions.
  Release uses full-travel progress and the existing threshold/flick policy.
  Corner grabs align to the spine. Deterministic geometry tests check material
  lengths, fixed hinges, final positions, and commit/rollback decisions.
- Scene changes cancel immediately, release wrapper pointer ownership, reject
  inactive completion events, and coalesce obsolete refresh requests.
- Moving sheets retain snapshots until completion. Superseded capture failures
  are ignored; cancelled and obsolete captures dispose their images.
- Backside uses the adjacent numbered page with readable landing UVs. The
  wrapper renders curls over its enclosing stage, beyond the flat page frame.
- Native vertex copies are disposed deterministically. Tail clearing touches
  only previously used vertices. Unused dark backside content was removed.

## Completed checks

Commands used `/Users/marius/.dotnet/dotnet` (SDK 10.0.301).

| Check | Result | Evidence |
| --- | --- | --- |
| `dotnet run --project tests/PageCurl.Core.Tests -c Release` | 12 groups passed | [core](core-green.log) |
| `dotnet run --project tests/PageCurl.Maui.Tests -c Release` | 4 groups passed | [adapter](maui-green.log) |
| `dotnet build src/PageCurl.Maui -c Release -f net10.0 -p:TargetFrameworks=net10.0` | 0 warnings/errors | [neutral build](neutral-build.log) |
| `dotnet build src/PageCurl.Demo -c Release -f net10.0-android` | 0 warnings/errors | [Android](android-build.log) |
| `dotnet build src/PageCurl.Demo -c Release -f net10.0-ios -p:ValidateXcodeVersion=false` | Diagnostic pass, 0 warnings/errors | [final iOS build](ios-final-build.log) |
| Standard Mac Catalyst demo build | Xcode version check failed: requires 26.6, installed 27.0 | [first error](maccatalyst-supported.log) |
| `dotnet build src/PageCurl.Demo -c Debug -f net10.0-maccatalyst -p:ValidateXcodeVersion=false` | Diagnostic pass, 0 warnings/errors | [Mac Catalyst](maccatalyst-build.log) |
| `dotnet build tests/PageCurl.Apple.Tests -c Debug -f net10.0-maccatalyst -p:ValidateXcodeVersion=false` | Diagnostic pass, 0 warnings/errors | [native build](apple-final-build.log) |
| Native fixture app binary with absolute `PAGECURL_TEST_OUTPUT` | Exit 0; snapshot, texture, layout, input, lifecycle and command assertions passed | [assertions](apple-final.log) |
| `ValidateXcodeVersion=false IOS_SMOKE_OUTPUT=… ./tools/ios-smoke.sh` | Exit 1; build/install/launch and light/dark captures passed; gesture stage blocked by missing SimulatorKit | [smoke](ios-smoke.log) |
| Live Mac demo interaction | Single-to-Spread works; right-to-left and left-to-right full turns commit; short drag returns | Direct UI observation |

Red logs alongside this record retain the failing regression evidence before
fixes. They are intentionally failing observations, not final validation.

## Performance and renderer decision

Warmed core controller and geometry paths, including full spread turns, allocate
zero managed bytes. A 1,000-submission native software-surface probe completed
without assertion failures; its reflection and Skia wrappers allocated 106,944
managed bytes. Its approximately 12-second duration is not a GPU frame-rate
benchmark. Resident-memory deltas vary and do not prove absence of leaks.
Skia's public array API still copies vertex buffers; deterministic disposal
fixes native lifetime without adding private interop.

Keep Skia. [Renderer comparison](../../renderer-comparison.md) explains why
Simple3D's current lack of texture mapping and transparent materials would add
implementation work. No comparative speedup is claimed.

## Limits

Apple checks use the explicit local Xcode validation override; they do not
certify a supported workload/Xcode pairing. iOS automated gesture checks remain
blocked by idb's missing SimulatorKit framework. The Mac became locked after
live interaction testing, preventing further desktop UI checks. Windows,
physical devices, accessibility hardware and long-running GPU profiling were
not exercised in this review.
