# Compatibility matrix

The package targets the .NET 10/MAUI 10 line and pins
`SkiaSharp.Views.Maui.Controls` 4.150.1. Status values describe evidence in
this repository, not a promise that every application renderer behaves the
same on every device.

The matrix summarizes the latest recorded local evidence. Detailed commands,
dates, screenshots, and unverified gates belong in
[`docs/validation/ios-release-readiness.md`](validation/ios-release-readiness.md)
and the release criteria belong in
[`release-readiness.md`](release-readiness.md).

| Layer/target | Minimum | Current status | Evidence |
| --- | --- | --- | --- |
| PageCurl.Core | .NET 10 | Build | Core tests and allocation guard pass locally |
| PageCurl.Maui neutral | .NET 10 | Build | `net10.0` Release build passes locally |
| Android | API 24 | Build | Explicit `net10.0-android` Release demo build passes locally |
| iOS | iOS 15 | Device build + simulator | Signed Debug build and install pass on an iPhone 17 Pro running iOS 26.6; simulator showcase smoke passes on Xcode 26.6 / iOS 26.2 |
| Mac Catalyst | macOS 15 / Catalyst 15 | Build + visual | Debug `net10.0-maccatalyst` build and resizable-window visual check pass locally |
| Windows | Windows 10.0.19041 | Not validated | No Windows host in this validation run |

The package remains platform-portable at the core/API boundary. Physical-device
performance, low-end hardware, store signing, VoiceOver/TalkBack hardware
passes, and long-run GPU/texture profiling are separate release gates.
