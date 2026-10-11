# Release readiness

The core behavior and public MAUI surface are implemented. Version 0.1.1 is
published on GitHub with complete packages and symbols; NuGet upload awaits
renewed account sign-in. Its hosted verification passed all four jobs and
122 native assertions, and both packages passed API compatibility against
0.1.0. See the [0.1.1 publication record](validation/release-0.1.1-2026-10-11/README.md).

The earlier 0.1.0 evidence follows. The hosted
[Verify run on 10 October](https://github.com/mariusmuntean/PageCurl/actions/runs/38048455583)
passed all four jobs for `9e759e0`: Core and neutral MAUI tests, documented
examples, Android/iOS simulator/Mac Catalyst/Windows Release builds, 119 native
PASS outcomes, package checks and isolated consumers. The distribution job
assembled and validated Core/MAUI packages and symbols with all five MAUI
frameworks. The external Windows consumer also built with a fresh package cache,
transitive Core, SkiaSharp registration and WinUI PRI generation.

The source repository and issue tracker are public, and the
[private security-reporting route](../SECURITY.md) is available. Core and MAUI
0.1.0 are published on NuGet.org with symbols. A fresh external consumer restored
and ran using NuGet.org as its only source; published payloads match the reviewed
distribution. See the
[10 October release record](validation/public-release-2026-10-10/README.md).

The [7 October Verify run](https://github.com/mariusmuntean/PageCurl/actions/runs/37581805987)
was blocked before execution by an account billing or spending limit. That is
historical evidence; the completed 10 October run supersedes the hosted-build
and distribution blocker.

Earlier records cover [lifecycle hardening](validation/production-hardening-2026-10-06/README.md),
[rapid scene switching](validation/scene-switch-performance-2026-10-06/README.md),
[package assembly](validation/public-launch-review-2026-10-06/README.md),
[runtime cleanup](validation/runtime-cleanup-2026-10-07/README.md) and
[Microsoft guidance](validation/microsoft-guidance-2026-10-07/README.md).
Use their dates and scope when assessing their local evidence.

The library's first public package release is complete. Future releases need
the same complete distribution and fresh remote-consumer checks, plus API
compatibility checks against the published baseline.

Representative application and device validation remains open:

- Run platform smoke paths on supported OS versions, including Windows runtime
  and iOS gesture interaction with a working SimulatorKit/device path.
- Profile drag and settling on low-end Android and physical iOS hardware for
  frame pacing, allocations, texture lifetime, memory growth, thermal behavior
  and long runs.
- Complete visual review for edges, tangent seams, shading, texture scaling,
  dark mode, rotation, safe areas, large pages, reduced motion, compact mobile
  layout and resizable desktop windows.
- Run VoiceOver and TalkBack journeys for scene selection, reader text,
  Previous/Next, anchors, threshold, haptics, reduced motion and the options sheet.
  Native accessibility assertions do not establish screen-reader certification.

Signed demo or store app releases additionally require Android/iOS signing,
archives and store packaging. The unsigned builds and NuGet library distribution
do not establish those app release gates or universal device performance.

The documentation site has a warning-free build, generated API reference and
checks for local links, anchors and publication boundaries. See
[documentation publishing](documentation.md). Package checks require assembly
and XML documentation assets for each requested framework and a transitive Core
dependency. Symbols use the SDK's Source Link support and snupkg format.
GitHub Actions updates have a monthly Dependabot schedule.

See [compatibility](compatibility.md) and [validation records](validation/) for
the evidence currently in the repository. Do not infer a passed gate from an
old plan or design document.
