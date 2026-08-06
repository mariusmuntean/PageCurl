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
