# Platform notes

The repository pins .NET SDK 10.0.301 and the .NET 10 MAUI workload line.
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
