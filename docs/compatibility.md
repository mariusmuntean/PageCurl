# Compatibility matrix

The package targets the .NET 10/MAUI 10 line and pins
`SkiaSharp.Views.Maui.Controls` 4.150.1. Status values describe evidence in
this repository, not a promise that every application renderer behaves the
same on every device.

| Layer/target | Minimum | Current status | Evidence |
| --- | --- | --- | --- |
| PageCurl.Core | .NET 10 | Build | Core tests and allocation guard pass locally |
| PageCurl.Maui neutral | .NET 10 | Build | `net10.0` Release build passes locally |
| Android | API 24 | Build | Explicit `net10.0-android` build is run in local validation/CI |
| iOS | iOS 15 | Simulator | iPhone 17 Pro simulator smoke passes on Xcode 26.6 / iOS 26.5 |
| Mac Catalyst | macOS 15 / Catalyst 15 | Not validated | No local Mac Catalyst workload gate in this validation run |
| Windows | Windows 10.0.19041 | Not validated | No Windows host in this validation run |

The package remains platform-portable at the core/API boundary. Physical
device, low-end hardware, store signing, VoiceOver/TalkBack hardware passes,
and long-run GPU/texture profiling are separate release gates.
