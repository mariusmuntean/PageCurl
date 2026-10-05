# Review-fix validation — 2026-10-04

All nine tasks in the [September fix plan](../../superpowers/plans/2026-09-15-library-demo-review-fixes.md)
are implemented. Existing uncommitted edits were preserved; no staging, commits,
pushes, stashes or resets were performed. Final independent review found no
remaining Important or Critical issues. This record describes implementation
validation, not completion of the full release matrix.

## Behavior covered

- Touch processing performs no filesystem logging; owned release/cancel events
  remain handled.
- Apple captures normalize color space, channel order, alpha and stride through
  an explicit sRGB BGRA context. Native assertions cover red, blue, white, gray,
  half-transparent red, Display-P3 red and Retina image dimensions.
- Interior grabs preserve the pointer/material position for all four edges;
  poses scale consistently between input and output dimensions.
- Spread captures reject obsolete content/size revisions. Explicit refresh
  recaptures all slots, including in-place content changes; automatic refresh
  drains dirty slots. Removed backs immediately use the configured fallback.
- Active turns retain their image handles until completion. Refresh waits are
  cancellable without breaking serialization of queued work. Lifecycle changes
  prevent obsolete requests joining a new turn.
- The demo removes unused eager PNG encoding, clears all six spread slots and
  guards callbacks after disappearance. All four scenes preserve synchronous
  completion status; the first programmatic command uses the fitted page size.
- Standard MAUI scene delegates/manifests allow the Apple demo to launch with
  the current runtime.

## Completed validation

CLI: `/Users/marius/.dotnet/dotnet`, SDK 10.0.301. Apple host: Xcode 27.0;
installed Apple workload requires Xcode 26.6.

| Command / check | Result | Evidence |
| --- | --- | --- |
| `dotnet run --project tests/PageCurl.Core.Tests -c Release` | 12 groups pass, including warmed allocation checks | [log](core-final.log) |
| `dotnet run --project tests/PageCurl.Maui.Tests -c Release` | 4 groups pass | [log](maui-final.log) |
| `dotnet build src/PageCurl.Maui -f net10.0 -c Release` | Pass, zero warnings/errors | [log](neutral-final.log) |
| `dotnet build src/PageCurl.Demo -f net10.0-android -c Release` | Pass, zero warnings/errors | [log](android-final.log) |
| Mac Catalyst demo standard build | Blocked by Xcode/workload version mismatch | [first error](maccatalyst-baseline.log) |
| `dotnet build src/PageCurl.Demo -f net10.0-maccatalyst -c Debug -p:ValidateXcodeVersion=false` | Diagnostic pass, zero warnings/errors | [log](maccatalyst-final.log) |
| `dotnet build tests/PageCurl.Apple.Tests -f net10.0-maccatalyst -c Debug -p:ValidateXcodeVersion=false` | Diagnostic pass, zero warnings/errors | [log](apple-fixture-build.log) |
| Native fixture binary with `PAGECURL_TEST_OUTPUT` | Exit 0; all native snapshot and linked demo assertions pass | [assertions](apple-green.log) |
| `dotnet run --project docs/validation/review-fixes-2026-10-04/probe/ReviewRepro.csproj -c Release` | Exit 0; corrected behavior observations | [log](probe-final.log), [source](probe/Program.cs) |
| `ValidateXcodeVersion=false IOS_SMOKE_OUTPUT=… ./tools/ios-smoke.sh` | Exit 1; build/install/launch and light/dark capture complete, HID stage blocked | [log](ios-smoke.log) |
| Actual Mac Catalyst demo UI | Visible page and controls; Next advances 01 / 08 to 02 / 08 and reports Turn complete | Live window observation |

The native fixture exits nonzero on assertion failure. The behavior probe is a
supplemental observation tool, not an assertion runner. Red logs in this folder
record failures before their fixes; they are deliberately failing evidence.
The [historical review](../review-2026-09-15/README.md) remains unchanged.

## Performance observations

The final warmed core probe reports zero managed bytes across 1,000 iterations.
Unused encoded page payloads were removed (386,446 bytes observed in the initial
probe). Native demo page creation now reports 53,368 managed bytes. Cold startup
timings vary with font/runtime caches and are not a native frame benchmark or
proof of a specific speedup. GPU/texture soak profiling remains outstanding.

## Remaining gates

`ValidateXcodeVersion=false` is a local diagnostic workaround. Re-run Apple
checks with a supported Xcode/workload pairing before release. The iOS scripted
interaction step cannot load idb's SimulatorKit framework from Xcode 27; its
successful screenshots do not certify the missing interaction stages.
Windows runtime, current physical-device validation, accessibility hardware
passes, low-end performance and long-run native profiling were not run here.
See [release criteria](../../release-readiness.md) for the complete gate list.
