# Compatibility matrix

The package targets the .NET 10/MAUI 10 line and pins
`SkiaSharp.Views.Maui.Controls` 4.150.1. Status values describe evidence in
this repository, not a promise that every application renderer behaves the
same on every device.

Version 0.1.1 also passed all four [hosted Verify jobs](https://github.com/mariusmuntean/PageCurl/actions/runs/38127006005),
including 122 native assertions and complete distribution checks. Its packages
passed backward API compatibility against 0.1.0 and are attached to the
[GitHub release](https://github.com/mariusmuntean/PageCurl/releases/tag/v0.1.1).
NuGet upload is pending; see the [publication record](validation/release-0.1.1-2026-10-11/README.md).

The matrix includes the completed
[10 October Verify run](https://github.com/mariusmuntean/PageCurl/actions/runs/38048455583)
for `9e759e0`. Earlier launches retain their recorded device and toolchain scope.
See the [release record](validation/public-release-2026-10-10/README.md),
[iOS evidence](validation/ios-release-readiness.md) and
[release criteria](release-readiness.md).

| Layer/target | Minimum | Recorded validation | Evidence |
| --- | --- | --- | --- |
| PageCurl.Core | .NET 10 | Hosted Release tests + package consumer | Core tests and warmed allocation guards pass |
| PageCurl.Maui neutral | .NET 10 | Hosted Release build + tests | API/lifecycle tests, documented examples and isolated package consumption pass |
| Android | API 24 | Hosted Release build + prior Rider launch | Release build passes; prior Rider launch used Pixel 9 / Android 15 |
| iOS | iOS 17 | Hosted Release simulator build + prior Rider launch | Build passes on Xcode 27.0; prior Rider launch used iPhone 17e / iOS 27.0. Automated HID interaction remains blocked by idb SimulatorKit |
| Mac Catalyst | macOS 14 / Catalyst 17 | Hosted Release build + native assertions | 119 native PASS outcomes cover snapshots, interaction, both navigation layouts, lifecycle and reader accessibility; prior Rider launch is recorded separately |
| Windows | Windows 10.0.17763 | Hosted Release build + external package consumer | Demo build and package assets pass; fresh-cache consumer verifies transitive Core, SkiaSharp registration and WinUI PRI generation. Runtime evidence remains open |

The hosted distribution job assembled and checked Core/MAUI packages and
symbols, including neutral, Android, iOS, Mac Catalyst and Windows MAUI assets.
Both 0.1.0 packages and symbols are published on NuGet.org; a fresh consumer
restored and ran using that feed alone. This package
evidence does not certify signed demo apps, store packaging, Windows runtime,
physical-device performance, VoiceOver/TalkBack or long-run GPU/texture behavior.

The [Rider and interaction validation on 4 October](validation/rider-and-interaction-2026-10-04/README.md)
uses SDK 10.0.401, workload set 10.0.401.1 and Xcode 27.0 with version checking
enabled. Earlier October records used a diagnostic Xcode override; those
historical results do not describe the current toolchain.

The [7 October Verify run](https://github.com/mariusmuntean/PageCurl/actions/runs/37581805987)
was blocked before execution by an account billing or spending limit. The
completed 10 October run supersedes that hosted-build and distribution blocker.
