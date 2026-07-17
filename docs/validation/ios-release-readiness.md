---
title: iOS release-readiness validation
description: Repeatable simulator smoke evidence for the PageCurl.Maui demo.
---

# Scope

This record covers the iOS simulator gates from
[`knowledge/project/release-readiness.md`](../../knowledge/project/release-readiness.md).
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
it, verifies the foreground accessibility surface including the testing-mode
switch when `idb` is available, and captures light/dark portrait screenshots.
The script cleans generated native artifacts before the build, verifies the
demo is still foregrounded through the accessibility tree when `idb` is
available, and waits 10 seconds after that check so the first Skia surface has
completed its simulator compositor warm-up.

## Latest simulator run

| Evidence | Result | Location/notes |
| --- | --- | --- |
| SDK/workload alignment | Pass | `.NET --version` = `10.0.301`; Xcode `26.6`; `maui-ios` and `maui-android` installed |
| iOS simulator build | Pass | Release `iossimulator-arm64`; 0 warnings, 0 errors |
| Install and launch | Pass | Bundle `com.example.pagecurldemo`; foreground accessibility check passed |
| Light appearance | Pass | `.artifacts/ios-smoke/latest/light.png` (1206×2622 PNG) |
| Dark appearance | Pass | `.artifacts/ios-smoke/latest/dark.png` (1206×2622 PNG) |
| Portrait frame | Pass | iPhone 17 Pro on iOS 26.2 simulator |
| Previous/Next commands | Pass | Accessibility tree exposed both labeled commands; Next advanced Page 1 to Page 2 |
| Reduced motion | Pass | Switch reported Value 1; Next advanced Page 2 to Page 3 without the settle path |
| Testing mode surface | Pass | Opt-in switch and runtime probe labels are exposed through the accessibility tree; diagnostics are disabled by default |
| Rotation and safe area | Pass | Landscape rotation kept controls/page inside the safe-area container |
| Large page | Pass | Page 4 of 4 was reached and displayed the generated large page |
| Image replacement/lifecycle | Code-validated | `MainPage.OnDisappearing` clears images, cancels interaction, unsubscribes, and disposes the page set |

## Manual simulator review

After the script launches the app:

1. Use the Previous and Next buttons and confirm their enabled states at the
   first and last pages.
2. Enable Reduce motion and confirm a command changes page state without a
   settle animation; disable it and confirm the animated turn returns.
3. Drag from both edges, cancel a drag, repeat turns, and navigate through the
   large fourth page.
4. Use the Simulator menu `Device > Rotate Left` and `Device > Rotate Right`,
   then repeat the command and drag checks in landscape.
5. Switch light/dark appearance and inspect page edges, surface contrast,
   labels, shadows, and texture scaling.
6. Enable Testing mode and confirm that the runtime probe reports state,
   pointer coordinates, pose, completion result, and render metrics while a
   gesture is active or settling. Disable it and confirm the panel disappears.

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
