# Platform notes

The repository pins .NET SDK 10.0.401 and the .NET 10 MAUI workload line.
`PageCurl.Maui` also exposes a neutral `net10.0` target for source-level checks.
Native demo targets are selected conditionally so a host does not evaluate
platforms it cannot build.

Use an explicit target framework for platform builds. On macOS, the installed
workload-enabled SDK may live outside the system-wide .NET root; inspect the
selected executable and workload list before diagnosing source. Android needs
the matching SDK platform and JDK. iOS and Mac Catalyst need the matching Xcode
and simulator/device runtimes. Windows validation needs a Windows host.

The project pins `SkiaSharp.Views.Maui.Controls` 4.150.1. Keep SkiaSharp and
MAUI on the same .NET major line when upgrading. Package restore success does
not prove that the native workload, signing, simulator, or device path works.

The control currently uses `SKGLView`. Apple OpenGL deprecation, GPU driver
differences, large textures, high-DPI mesh cost, and device thermal behavior
remain platform risks that require measurement.

The iOS simulator can reject the first on-demand GL draw while its native
window is still becoming active. `PageTurnView` schedules one generation-
guarded retry after handler attachment. This recovers an idle page without a
permanent render loop or a callback that can outlive a disconnected handler.

The demo initializes CommunityToolkit `MediaElement` on iOS and Mac Catalyst,
and on Android API 26 or newer. Its local H.264 clip has a still image beneath
the native video layer so page snapshots and playback failures do not produce
an empty page.

A physical iOS build requires the demo `ApplicationId` to match a development
provisioning profile from the selected Apple team. The repository uses
`de.marius.PageCurlDemo`; do not reuse an unrelated application's bundle ID or
profile. Rider and the .NET iOS targets can select an installed matching
profile automatically after Xcode has created or downloaded it.

Apple snapshots normalize UIKit output through an explicit sRGB bitmap context
with 8-bit premultiplied BGRA channels before copying into Skia. Grayscale,
RGBA, and extended-color source images are converted rather than reinterpreted.

The demo Apple targets declare the MAUI scene configuration and use
`MauiUISceneDelegate`. The configuration name must match MAUI's default key;
otherwise a scene can open without creating the MAUI window. See the
[MAUI scene implementation](https://github.com/dotnet/maui/blob/10.0.20/src/Core/src/Platform/iOS/MauiUISceneDelegate.cs).

## Local builds in Rider and CI

The project pins SDK 10.0.401 and workload set 10.0.401.1 in `global.json`,
with MAUI 10.0.110 packages. This selects Apple SDK 27.0.10722, compatible with
Xcode 27.0. iOS and Mac Catalyst now require platform version 17 or later.
Mac Catalyst 17 corresponds to macOS 14. Keep Xcode's version check enabled.
See the [Microsoft release](https://github.com/dotnet/macios/releases/tag/dotnet-10.0.1xx-xcode27.0-10722).

In Rider's **Toolset and Build**, select the workload-enabled .NET CLI and
**Auto detected** MSBuild. On this machine that CLI is
`/Users/marius/.dotnet/dotnet`. Do not pin an older `MSBuild.dll` path separately;
`global.json` selects the SDK. Use Rider's normal demo project configurations
and choose the emulator, simulator, or Mac target from the toolbar. The
`PageCurl.Maui` project is a library and should not be a launch target.
The Mac app bundle name matches the assembly name so Rider finds it; its
visible title remains Page Curl Demo.

For Android, use JDK 21 and SDK platform 36. For iOS development, select an
available simulator. Physical devices must be unlocked and have Developer
Mode and the matching provisioning profile; a developer-disk-image error on a
locked phone is a deployment failure rather than a project build error.

GitHub uses the `xcode-27` arm64 image, explicitly selects Xcode 27.0, installs
the pinned workload set, and builds Android, the unsigned iOS simulator app,
and Mac Catalyst. It also runs the core and neutral adapter tests. That image
is currently a public preview; the workflow uses an explicit label rather
than relying on the moving `macos-latest` default. Runner image availability
is documented by [GitHub](https://github.com/actions/runner-images#available-images).
The Linux job evaluates only neutral .NET projects and disables the MAUI workload
resolver. This avoids requiring the pinned native workload set for Core and
package-consumer checks; native resolution stays enabled on macOS. The workflow
project checker uses standard Unix tools so it also runs on stock hosted images.
See [compatibility](compatibility.md) for completed hosted evidence.

After tests, terminate the apps and emulator processes started for verification,
shut down newly booted simulators, and remove temporary test files after retaining
concise validation evidence. Preserve unrelated user sessions.
