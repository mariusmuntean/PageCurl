---
type: Validation Record
title: iOS release-readiness validation
description: Repeatable simulator smoke evidence for the PageCurl.Maui demo.
tags:
  - validation
  - ios
  - release
timestamp: 2026-08-08T00:00:00Z
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
user-local SDK at `$HOME/.dotnet` when available, builds the explicit
`net10.0-ios` simulator target, reads the bundle identifier from the demo
project, installs `de.marius.PageCurlDemo`, launches
it, verifies the compact foreground accessibility surface, rejects decorative
hint glyphs, captures light/dark portrait screenshots, opens the options sheet,
and visits the Spread, Content, and Backside scenes when `idb` and `jq` are
available. It switches Content through Image, View, and Video, updates the live
counter, records spread/backside drags, and extracts active-curl frames with
`ffmpeg`.
The script performs a normal incremental build, verifies the foreground
accessibility tree when `idb` is available, and allows the first Skia surface
to finish simulator compositor warm-up. Its exit trap stops the test app and
recording, removes temporary movies and new idb helpers, and shuts down newly
booted simulators. Existing simulator sessions keep their boot state and
original appearance.

## Latest simulator run

The [2026-10-04 Rider and interaction run](rider-and-interaction-2026-10-04/README.md)
built and launched the Release app on iPhone 17e / iOS 27 with the supported
Xcode 27 workload. Light/dark captures passed; automated gestures failed
because idb requires the removed SimulatorKit framework. No Xcode override
was used. The older completed feature-scene run below remains historical
evidence rather than a pass for the current implementation.

## Previous feature-scene run

Validation date: 2026-09-15. Output:
`.artifacts/ios-smoke/20260915T135939Z/`. Device: iPhone 17e (iOS 26.5,
booted simulator). This run exercised the grab-relative drag model, the
book-model spread back faces, the coalesced spread snapshot refreshes, and
the direct pixel-copy Apple snapshotter.

| Evidence | Result | Location/notes |
| --- | --- | --- |
| SDK/workload alignment | Pass | User-local .NET 10 SDK and explicit iOS target restore/build completed |
| iOS simulator build | Pass | Release `iossimulator-arm64`; 0 warnings, 0 errors |
| Install and launch | Pass | Bundle `de.marius.PageCurlDemo`; foreground accessibility check passed |
| Light appearance | Pass | `light.png`; generated page art and compact controls visible |
| Dark appearance | Pass | `dark.png`; page surface recovered after simulator compositor warm-up |
| Portrait frame | Pass | iPhone 17e simulator (iOS 26.5) |
| Accessibility surface | Pass | Four scene buttons, Previous/Next, Options, all anchor settings, threshold slider, flick, haptics, and reduced motion exposed; decorative curl hints absent |
| Compact options sheet | Pass | `options.png`; anchor picker, percentage threshold, flick, ratio, haptics, reduced motion, and Close visible |
| Showcase scenes | Pass | `spread.png`, `content-image.png`, `content-view.png`, `content-video.png`, and `backside.png` captured after accessibility-driven scene changes |
| Active spread curl | Pass | `spread-curl.mov` and `spread-curl.png`; stationary left page and gutter remain visible while the right page crosses them |
| Custom backside and lighting | Pass | `backside-curl.mov` and `backside-curl.png`; custom dark-green content is visible through a smooth, readable underside without the former sawtooth band |
| Bundled image | Pass | The JPEG is present in the app bundle and visible by default in `content-image.png` |
| Bundled video | Pass | The local MP4 is present in the app bundle; `content-video.png` and `content-video-later.png` show different Big Buck Bunny frames |
| Aspect-preserving page | Pass | Portrait source page remains undistorted in the fitted content rectangle |
| Spread capture performance | Pass | Direct pixel-copy snapshotter; alternating turns keep resident memory flat on Mac Catalyst (see `maccatalyst-interaction.md`) |
| Programmatic page navigation | Follow-up | Controls are exposed and boundary states render correctly; this smoke does not yet wait through and assert a completed animated turn |
| Rotation, large page, gestures, and device performance | Follow-up | Manual/device gates remain below |

Previous run (2026-08-08, iPhone 17 Pro): `.artifacts/ios-smoke/20260808-showcase-feedback/`.

## Latest physical-device deployment

Validation date: 2026-08-08. Target: iPhone 17 Pro, iOS 26.6.

| Evidence | Result | Notes |
| --- | --- | --- |
| Development identity | Pass | Current Apple Development certificate is valid through March 2027 |
| Provisioning | Pass | Xcode-managed development profile matches `de.marius.PageCurlDemo`, includes the target device, and expires in August 2027 |
| Current clean device build | Pass | Debug `ios-arm64`; 0 warnings, 0 errors; includes the showcase corrections and bundled media |
| Earlier install and launch | Pass | `devicectl` installed and launched the provisioning checkpoint with bundle identifier `de.marius.PageCurlDemo` |
| Current install and launch | Follow-up | The paired phone is connected but passcode-locked; the install connection was reset before transfer |

## Manual simulator review

After the script launches the app:

1. Use Previous and Next in Single, Spread, Content, and Backside and
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
7. In Content, switch Image/View/Video after a curl, use the live counter,
   refresh its snapshot, play the local video, and curl each. In Backside,
   inspect mirror, color, and custom view.
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
  pressure remain release gates; the device build/install above is deployment
  evidence, not a performance pass.
- VoiceOver/TalkBack behavior needs a screen-reader pass on supported hardware
  or a clearly documented simulator accessibility run.
- App Store signing, archive validation, provisioning, and distribution are not
  part of the local smoke script.
