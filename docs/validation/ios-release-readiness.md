---
type: Validation Record
title: iOS release-readiness validation
description: Repeatable simulator smoke evidence for the PageCurl.Maui demo.
tags:
  - validation
  - ios
  - release
timestamp: 2026-08-07T00:00:00Z
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
it, verifies the compact foreground accessibility surface, rejects decorative
hint glyphs, captures light/dark portrait screenshots, opens the options sheet,
and visits the spread, video, backside, and pinned-corner scenes when `idb` and
`jq` are available.
The script cleans generated native artifacts before the build, verifies the
demo is still foregrounded through the accessibility tree when `idb` is
available, and waits 10 seconds after that check so the first Skia surface has
completed its simulator compositor warm-up.

## Latest simulator run

Validation date: 2026-08-07. Output:
`.artifacts/ios-smoke/20260807-showcase-final/`.

| Evidence | Result | Location/notes |
| --- | --- | --- |
| SDK/workload alignment | Pass | User-local .NET 10 SDK and explicit iOS target restore/build completed |
| iOS simulator build | Pass | Release `iossimulator-arm64`; 0 warnings, 0 errors |
| Install and launch | Pass | Bundle `com.example.pagecurldemo`; foreground accessibility check passed |
| Light appearance | Pass | `light.png`; generated page art and compact controls visible |
| Dark appearance | Pass | `dark.png`; page surface recovered after simulator compositor warm-up |
| Portrait frame | Pass | iPhone 17 Pro simulator |
| Accessibility surface | Pass | Five scene buttons, Previous/Next, Options, all anchor settings, threshold slider, flick, haptics, and reduced motion exposed; decorative curl hints absent |
| Compact options sheet | Pass | `options.png`; anchor picker, percentage threshold, flick, ratio, haptics, reduced motion, and Close visible |
| Showcase scenes | Pass | `spread.png`, `content-view.png`, `content-video.png`, `backside.png`, and `corner.png` captured after accessibility-driven scene changes |
| Bundled video | Pass | The local MP4 is present in the app bundle; Content → Video remains foregrounded and renders a moving frame over the poster fallback |
| Aspect-preserving page | Pass | Portrait source page remains undistorted in the fitted content rectangle |
| MacCatalyst responsive build | Pass | Debug `net10.0-maccatalyst` build and wide-window visual review completed; video uses the stable poster fallback on this target |
| Programmatic page navigation | Follow-up | Controls are exposed and boundary states render correctly; this smoke does not yet wait through and assert a completed animated turn |
| Rotation, large page, gestures, and device performance | Follow-up | Manual/device gates remain below |

## Manual simulator review

After the script launches the app:

1. Use Previous and Next in Single, Spread, Content, Backside, and Corner and
   confirm their enabled states at both boundaries.
2. Enable Reduce motion and confirm a command changes page state without a
   settle animation; disable it and confirm the animated turn returns.
3. Drag from every edge and corner, cancel a drag, repeat turns, and navigate
   through the high-resolution seventh page.
4. Select each edge and corner anchor. Confirm an edge stays stationary and a
   pinned corner remains fixed while another free point follows the finger.
5. Use the Simulator menu `Device > Rotate Left` and `Device > Rotate Right`,
   then repeat command, lock, and drag checks in landscape.
6. Switch light/dark appearance and inspect page edges, surface contrast,
   labels, shadows, and texture scaling.
7. In Content, use the live counter, refresh its snapshot, play the local
   video, and curl each. In Backside, inspect mirror, color, and custom view.
8. Cross the page-change threshold in both directions and confirm the light
   haptic cue; complete a turn and confirm the stronger cue.

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
