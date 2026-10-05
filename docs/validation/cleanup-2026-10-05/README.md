# Code cleanup — 2026-10-05

This cleanup preserves the existing interaction and snapshot contracts. The
checkout contained substantial earlier work; this record covers only the
cleanup applied on top of it. No public APIs, package versions, geometry
formulas, or platform settings changed.

## Changes

- Controller drag and programmatic initialization share the pointer, progress,
  velocity, completion, and threshold reset logic.
- Renderer image changes share the existing visual invalidation callback.
  Direct touch movement and release reuse the wrapper interaction methods;
  release and cancellation retain their respective endpoint policies.
- Spread content properties share one slot-aware replacement callback.
  Both navigation directions use the same content/snapshot readiness rules.
  Capture serialization, revisions, disposal, and optional-back fallback are
  unchanged.

The production changes remove 112 net lines across four files. Added tests
exercise repeated controller turns and replacement of every spread slot,
including host retention, direction readiness, selective recapture, and the
actual front, destination, or back texture used during the turn. Both test
runners passed before and after the refactor.
Independent read-only review compared the six changed source/test files to
their pre-cleanup contents and found no actionable issues.

## Completed checks

Commands used `/Users/marius/.dotnet/dotnet`, SDK 10.0.401. Apple checks used
the repository's supported workload with Xcode validation enabled.

| Command / check | Result |
| --- | --- |
| `dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj -c Release` | All 12 groups passed, including zero-allocation warmed loops |
| `dotnet run --project tests/PageCurl.Maui.Tests/PageCurl.Maui.Tests.csproj -c Release -f net10.0` | All 4 groups passed, including added six-slot coverage |
| `dotnet build src/PageCurl.Maui/PageCurl.Maui.csproj -c Release -f net10.0 -p:TargetFrameworks=net10.0` | Passed, 0 warnings and 0 errors |
| `dotnet build src/PageCurl.Demo/PageCurl.Demo.csproj -c Release -f net10.0-android` | Passed, 0 warnings and 0 errors |
| `dotnet build tests/PageCurl.Apple.Tests/PageCurl.Apple.Tests.csproj -c Debug -f net10.0-maccatalyst` | Passed, 1 environment warning and 0 errors |
| Native fixture app binary with absolute `PAGECURL_TEST_OUTPUT` | Exit 0; snapshot, texture orientation, mesh tail, demo lifecycle, spread, and command assertions passed; [output](native-tests.log) |
| `git diff --check` | Passed |

The Apple build warning concerns the deprecated machine-local
`~/Library/Preferences/Xamarin/Settings.plist`. The fixture also emitted a
UIKit diagnostic about the inherited background-fetch delegate and missing
`fetch` background mode; it completed successfully. Neither diagnostic was
addressed by changing application behavior during this refactor.

Native timing and memory values in the fixture output are observations from
this run, including reflection overhead, rather than speedup claims or proof
of allocation-free native rendering. The fixture exited after its tests.

## Checks not run

iOS simulator smoke, Windows, physical devices, screen readers, and long-run
GPU/texture profiling were not run for this cleanup. See the
[release criteria](../../release-readiness.md) for those remaining gates.

## Pre-integration test-runner correction

A fresh pre-commit run exposed nondeterministic spread tests. The console
runner had no synchronization context, allowing captures and gestures to
resume on different pool threads despite the controls' UI-thread contract.
Repeated execution reproduced failures in 18 of 100 runs. The neutral runner
now pumps continuations on one thread.

That deterministic ordering also exposed a premature cancellation assertion:
the public caller wait cancels before internal capture disposal completes.
The assertion now waits for the internal capture chain before checking the
image handle. Both corrections affect tests only. All four MAUI groups passed
in 100 subsequent executions; [repeat results](maui-repeat.log).
