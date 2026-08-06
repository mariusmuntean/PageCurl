---
type: Validation Record
title: iOS release-readiness validation
description: Repeatable simulator smoke evidence for the PageCurl.Maui demo.
tags:
  - validation
  - ios
  - release
timestamp: 2026-07-18T00:00:00Z
---

# Scope

This record covers the iOS simulator gates from
[`docs/release-readiness.md`](../release-readiness.md).
It separates repeatable simulator evidence from physical-device and store-signing
work that cannot be proven by this local run.

## Command

From the repository root:

```sh
./tools/ios-smoke.sh
```

The optional first argument is an iPhone simulator UDID. Set
`IOS_SMOKE_OUTPUT` to choose a durable output directory. The script uses the
user-local SDK at `/Users/marius/.dotnet`, builds the explicit
`net10.0-ios` simulator target, installs `com.example.pagecurldemo`, launches
it, verifies the foreground accessibility surface including the interactive
page description and edge-lock selector, rejects decorative hint glyphs,
captures light/dark portrait screenshots, and captures Testing mode with its
diagnostics and navigation commands visible when `idb` and `jq` are available.
The script cleans generated native artifacts before the build, verifies the
demo is still foregrounded through the accessibility tree when `idb` is
available, and waits 10 seconds after that check so the first Skia surface has
completed its simulator compositor warm-up.

## Latest simulator run

Validation date: 2026-07-18. Output: `.artifacts/ios-smoke/latest/`.

| Evidence | Result | Location/notes |
| --- | --- | --- |
| SDK/workload alignment | Pass | User-local .NET 10 SDK and explicit iOS target restore/build completed |
| iOS simulator build | Pass | Release `iossimulator-arm64`; 0 warnings, 0 errors |
| Install and launch | Pass | Bundle `com.example.pagecurldemo`; foreground accessibility check passed |
| Light appearance | Pass | `.artifacts/ios-smoke/latest/light.png` |
| Dark appearance | Pass | `.artifacts/ios-smoke/latest/dark.png` |
| Portrait frame | Pass | iPhone 17 Pro simulator |
| Accessibility surface | Pass | Interactive page, hinge selector, testing switch, and navigation commands exposed; decorative rails/arrows absent |
| Testing mode surface | Pass | `.artifacts/ios-smoke/latest/testing.png`; runtime probe and navigation commands visible, then switch returned to off |
| Aspect-preserving page | Pass | Portrait source page remains undistorted in the fitted content rectangle |
| MacCatalyst responsive build | Pass | Debug `net10.0-maccatalyst` build and resizable-window visual check completed locally; wide windows use a centered bounded reading stage |
| Navigation and reduced motion | Pass | Simulator accessibility taps moved `01 / 04` to `02 / 04` and back, and toggled Reduce Motion `0` to `1` and back to `0` |
| Rotation, large page, gestures, and device performance | Follow-up | Manual/device gates remain below |

## Manual simulator review

After the script launches the app:

1. Use the Previous and Next buttons and confirm their enabled states at the
   first and last pages.
2. Enable Reduce motion and confirm a command changes page state without a
   settle animation; disable it and confirm the animated turn returns.
3. Drag from every edge and corner, cancel a drag, repeat turns, and navigate
   through the large fourth page.
4. Select each hinge option and confirm the selected edge remains stationary
   while every other edge/corner can start a gesture.
5. Use the Simulator menu `Device > Rotate Left` and `Device > Rotate Right`,
   then repeat command, lock, and drag checks in landscape.
6. Switch light/dark appearance and inspect page edges, surface contrast,
   labels, shadows, and texture scaling.
7. Enable Testing mode and confirm that the runtime probe reports state,
   pointer coordinates, free/hinge edges, pose, completion result, and render
   metrics while a gesture is active or settling. Disable it and confirm the
   panel disappears.

## Performance evidence

The platform-free core guard measures 1,000 warmed geometry builds and 1,000
warmed controller drag/settle cycles. It currently passes with zero managed
bytes allocated on the measured thread. The MAUI control exposes opt-in
`PageTurnPerformanceSnapshot` metrics; enabling them is intentionally excluded
from the default render path.

The installed Instruments templates available for a follow-up run are
`Time Profiler` and `Allocations`. No timed profile is claimed by this smoke
run.

For an interactive simulator profile, use an installed Instruments template:

```sh
xcrun xctrace list templates | rg 'Time Profiler|Allocations'
```

Record the selected template and run duration here after exercising the manual
review scenarios. Simulator timing is evidence for correctness and relative
regressions, not a physical-device frame-rate claim.

## Not proven locally

- Physical-device GPU frame pacing, low-end Android performance, and texture
  pressure remain release gates.
- VoiceOver/TalkBack behavior needs a screen-reader pass on supported hardware
  or a clearly documented simulator accessibility run.
- App Store signing, archive validation, provisioning, and distribution are not
  part of the local smoke script.
