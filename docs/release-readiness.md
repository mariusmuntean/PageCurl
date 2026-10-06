# Release readiness

The core behavior and public MAUI surface are implemented. This repository has
local build, simulator, and deterministic test evidence, but it does not yet
establish production readiness on representative hardware.

The hosted [Verify workflow](https://github.com/mariusmuntean/PageCurl/actions/runs/37283932941)
now passes clean Android, iOS simulator and Mac Catalyst builds, neutral package
consumption and native Mac Catalyst assertions. The showcase also has recorded
computer-control journeys. These support public discussion of the implementation
and demonstrated behavior; avoid claims of universal device performance or
completed screen-reader certification.

The [6 October hardening record](validation/production-hardening-2026-10-06/README.md)
adds interruption and teardown regressions, cancellable layout waits and full
native command journeys in both layouts. These are local checks; the final
changes still need a hosted Verify pass and the hardware gates below.

The [rapid-switch performance record](validation/scene-switch-performance-2026-10-06/README.md)
reproduces the timing-sensitive scene lag and records reduced dispatch latency,
obsolete-capture suppression and unchanged rendering/snapshot quality contracts.

Before a public or flagship-reader release:

1. Retain clean hosted builds with pinned .NET, MAUI and SkiaSharp, including
   the Windows build and native package asset checks; extend
   the current unsigned build evidence to distributable Android/iOS packages.
2. Run the core suite and platform smoke paths on supported OS versions.
3. Profile drag and settling on low-end Android and current physical iOS
   hardware. Check frame pacing, allocations, texture lifetime, memory growth,
   thermal behavior, and long runs.
4. Complete visual review for edges, tangent seams, shading, texture scaling,
   dark mode, rotation, safe areas, large pages, reduced motion, compact mobile
   layout, and resizable desktop windows.
5. Run VoiceOver and TalkBack passes for scene selection, the interactive page,
   Previous/Next, edge/corner anchors, threshold, haptics, reduced motion, and
   the options sheet.
6. Validate signing, archives, store packaging, API compatibility against a
   published baseline, and package publication before distribution. Native
   packages packed on one host contain that host's selected targets; assemble
   and verify a complete distribution before publishing a cross-platform package.

The documentation site has a warning-free build, generated API reference and
checks for local links, anchors and publication boundaries. See
[documentation publishing](documentation.md). Package checks require assembly
and XML documentation assets for each requested framework and a transitive Core
dependency. Symbols use the SDK's Source Link support and snupkg format.
GitHub Actions updates have a monthly Dependabot schedule.

See [compatibility](compatibility.md) and [validation records](validation/) for
the evidence currently in the repository. Do not infer a passed gate from an
old plan or design document.
