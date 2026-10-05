# Platform support

PageCurl uses .NET 10, MAUI 10 and SkiaSharp.Views.Maui.Controls 4.150.1.

| Target | Minimum | Validation scope |
| --- | --- | --- |
| Core and neutral MAUI | .NET 10 | Deterministic tests and isolated package consumer |
| Android | API 24 | Hosted Release build; recorded emulator launch |
| iOS | iOS 17 | Hosted simulator build; recorded simulator launch |
| Mac Catalyst | macOS 14 / Catalyst 17 | Hosted build and native rendering/lifecycle assertions; recorded pointer interaction |
| Windows | Windows 10.0.17763 | CI build gate added; runtime validation still required |

Minimum OS values come from the projects. A compile target such as
`net10.0-windows10.0.19041.0` is not the minimum runtime OS version.

## Before shipping your reader

Test on your supported hardware and OS versions. Measure frame pacing, memory
and texture lifetime during long reading sessions, particularly on low-end
Android and current iOS devices. Test VoiceOver/TalkBack, rotation, safe areas,
background/foreground transitions, reduced motion and video poster behavior.

Validate your app's signing, archives and store package independently. This
library's unsigned CI builds and neutral package consumer do not certify store
submission or hardware performance. Windows has a build gate, but no recorded
Windows UI or snapshot-pixel certification yet.
