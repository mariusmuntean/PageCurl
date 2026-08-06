# Release readiness

The core behavior and public MAUI surface are implemented. This repository has
local build, simulator, and deterministic test evidence, but it does not yet
establish production readiness on representative hardware.

Before a public or flagship-reader release:

1. Build and package Android and iOS in clean CI with pinned .NET, MAUI, and
   SkiaSharp versions.
2. Run the core suite and platform smoke paths on supported OS versions.
3. Profile drag and settling on low-end Android and current physical iOS
   hardware. Check frame pacing, allocations, texture lifetime, memory growth,
   thermal behavior, and long runs.
4. Complete visual review for edges, tangent seams, shading, texture scaling,
   dark mode, rotation, safe areas, large pages, reduced motion, compact mobile
   layout, and resizable desktop windows.
5. Run VoiceOver and TalkBack passes for the interactive page, Previous/Next
   commands, lock selector, reduced motion, settings, and diagnostics.
6. Validate signing, archives, store packaging, API compatibility, and package
   publication before distribution.

See [compatibility](compatibility.md) and [validation records](validation/) for
the evidence currently in the repository. Do not infer a passed gate from an
old plan or design document.
