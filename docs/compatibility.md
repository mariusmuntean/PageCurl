# Compatibility matrix

The package targets the .NET 10/MAUI 10 line and pins
`SkiaSharp.Views.Maui.Controls` 4.150.1. Status values describe evidence in
this repository, not a promise that every application renderer behaves the
same on every device.

The matrix summarizes completed local and hosted evidence. Some hosted runs
precede the current source revision; use each record's date and scope when
assessing a release. Detailed commands, dates, screenshots and unverified gates
belong in
[`docs/validation/ios-release-readiness.md`](validation/ios-release-readiness.md)
and the release criteria belong in
[`release-readiness.md`](release-readiness.md).

| Layer/target | Minimum | Recorded validation | Evidence |
| --- | --- | --- | --- |
| PageCurl.Core | .NET 10 | Build | Core tests and allocation guard pass locally |
| PageCurl.Maui neutral | .NET 10 | Build | `net10.0` Release build passes locally |
| Android | API 24 | Build + Rider launch | Release build passes; Rider launches the demo on Pixel 9 / Android 15 |
| iOS | iOS 17 | Build + Rider simulator launch | Release simulator build passes on Xcode 27.0; Rider launches the demo on iPhone 17e / iOS 27.0. Automated HID interactions remain blocked by idb SimulatorKit |
| Mac Catalyst | macOS 14 / Catalyst 17 | Build + Rider launch + native assertions | Supported Xcode 27.0 Debug build and Rider launch pass; native fixture checks snapshots, rendered interaction, immediate scene switching and teardown |
| Windows | Windows 10.0.17763 | Hosted Release build + package checks | Windows demo build, assembly/XML assets and transitive Core dependency passed; runtime evidence remains open |

The package remains platform-portable at the core/API boundary. Physical-device
performance, low-end hardware, store signing, VoiceOver/TalkBack hardware
passes, and long-run GPU/texture profiling are separate release gates.

The [Rider and interaction validation on 4 October](validation/rider-and-interaction-2026-10-04/README.md)
uses SDK 10.0.401, workload set 10.0.401.1 and Xcode 27.0 with version checking
enabled. Earlier October records used a diagnostic Xcode override; those
historical results do not describe the current toolchain. The
[production hardening](validation/production-hardening-2026-10-06/README.md) and
[rapid scene switching](validation/scene-switch-performance-2026-10-06/README.md)
records cover the later local lifecycle, capture and navigation checks.
The [public launch review](validation/public-launch-review-2026-10-06/README.md)
records local elapsed-time settling, background/video lifecycle and package
checks. The new complete-distribution workflow still needs a hosted run.

The hosted [Verify run on 5 October](https://github.com/mariusmuntean/PageCurl/actions/runs/37283932941)
passed both Linux and macOS jobs, including the isolated package consumer,
neutral API tests, Android/iOS/Mac Catalyst builds and native assertions.
These unsigned builds do not certify store packaging or physical-device behavior.

The [Windows job on 5 October](https://github.com/mariusmuntean/PageCurl/actions/runs/37342825899/job/111873916945)
also passed the unpackaged Release demo build and native package checks.
