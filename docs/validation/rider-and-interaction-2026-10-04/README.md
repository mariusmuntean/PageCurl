# Rider and interaction validation — 2026-10-04

This run follows the earlier [quality review](../quality-2026-10-04/README.md).
It addresses the reported diagonal Spread regression and repeats Single-to-Spread
switching immediately after a real rendered gesture.

## Changes

Spread bends now follow vertical and diagonal pointer movement while a rigid
rotation around the spine supplies the full cross-page travel. The whole spine
stays stationary and material lengths remain unchanged. Completed turns land
flat. Back faces use the adjacent numbered page and render over the full stage.

The demo no longer recreates unchanged spread content on scene entry or runs
its own timer-based snapshot retry loop. Controls observe content layout,
coalesce automatic requests, defer hidden captures, and yield between UIKit
captures so input can dispatch. Native vertex storage reserves extra capacity
only for cells crossing the facing seam. At 64 by 64 cells, each submitted
vertex-array capacity drops from 73,728 to 26,112. This is a memory/copy reduction,
not a measured GPU frame-rate claim.

Rider uses the installed workload-enabled CLI and auto-detected MSBuild.
`global.json` pins SDK 10.0.401 and workload set 10.0.401.1; packages use
MAUI 10.0.110. Apple SDK 27.0.10722 supports installed Xcode 27.0 with version
checking enabled. The Android manifest and explicit Core references make the
demo directly launchable and analyzable in Rider. Standard native project run
configurations suffice; redundant shared platform configurations were removed.

## Completed evidence

| Check | Result | Evidence |
| --- | --- | --- |
| Core runner, Release | All 12 groups passed, including diagonal pulls, fixed spines, full endpoints, narrow-grid capacity and allocation guards | [core](core-final.log) |
| Diagonal regression before implementation | Failed because vertical movement did not move the grabbed edge | [red regression](core-diagonal-red.log) |
| Neutral adapter runner, Release | All 4 groups passed | [adapter](maui-refresh.log) |
| Neutral MAUI Release build | 0 warnings, 0 errors | [build](neutral-final.log) |
| Native Mac Catalyst fixture build | Supported toolchain; 0 errors | [build](native-build-final.log) |
| Native fixture process | Exit 0; all snapshot, mesh, input, lifecycle and completion assertions passed | [assertions](native-final.log) |
| Android Release build | 0 warnings, 0 errors | [build](android-final.log) |
| iOS Release simulator build | 0 errors; Xcode check enabled | [build](ios-release-final.log) |
| Mac Catalyst Release / Debug builds | Both passed; Xcode check enabled | [Release](mac-release.log), [Debug](mac-debug-final.log) |
| iOS smoke | Build/install/launch and light/dark captures passed; HID stage failed because idb cannot find SimulatorKit | [smoke](ios-smoke.log), [light](ios-light.png), [dark](ios-dark.png) |
| Rider diagnostics | No errors in MainPage, PageSpreadView or PageCurlGeometry | Rider MCP file analysis |
| Rider launches | Mac Catalyst, iPhone 17e / iOS 27 simulator and Pixel 9 / Android 15 emulator displayed the demo | Direct UI observation |
| Latest live Mac interaction | Single drag then immediate Spread click; diagonal forward/back turns committed; short diagonal drag rolled back with flicks disabled | Direct UI observation |
| Workflow references / shell syntax | Project paths exist; workflow YAML parses; iOS smoke shell syntax passes | Local checks |

The native immediate-switch test renders five GL frames, releases and clicks
Spread in the same event-loop turn. The final run returned from the handler in
52.0 ms and dispatched again in 161.5 ms (limits 150 / 250 ms). Only one UIKit
capture occurred before the next dispatch, taking 54.8 ms. This replaces the
previous uninterrupted four-capture batch. Actual Single frames averaged
31.6 ms on this host; no device-wide performance guarantee is inferred.

The first iOS Release link after upgrading the SDK used stale generated native
output with old registrar symbols. Cleaning only the iOS Release target and
rebuilding resolved it. Apple builds report one machine-specific warning about
the deprecated Xamarin Settings.plist file; no Xcode validation override was used.

The linked light/dark screenshots record the compact initial layout and theme
contrast on that build. They do not demonstrate an active turn or certify
gesture behavior.

## Cleanup and remaining gates

Test apps were stopped. The Android emulator and newly booted iOS simulator
were shut down; the new idb helper was terminated. Existing unrelated sessions
were preserved. Temporary logs and samples were removed after retaining this
record. The iOS smoke now performs cleanup on success and failure, restores an
existing simulator's appearance, and removes temporary recordings and new idb
helpers.

GitHub uses the Xcode 27 arm64 preview image and pinned SDK/workloads, but the
hosted workflow has not been dispatched from this uncommitted checkout. iOS HID
coverage remains blocked by SimulatorKit. Physical-phone deployment was blocked
by the locked device's developer-disk-image mount; simulator deployment passed.
Windows, hardware accessibility checks, store signing and sustained GPU/device
profiling were not exercised. The [renderer comparison](../../renderer-comparison.md)
retains Skia: Simple3D currently lacks the textures and transparency needed here.
