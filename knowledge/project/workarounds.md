---
type: Engineering Workaround
title: Workarounds and known solutions
description: Explicit handling for MAUI workload behavior, rendering lifecycle, image ownership, and constrained development environments.
tags:
  - workarounds
  - tooling
  - platform
timestamp: 2026-07-16T00:00:00Z
---

# Tooling and build workarounds

The core project is intentionally independent of MAUI so it can be built and
tested on any machine with the .NET 10 SDK. MAUI projects still require the
matching .NET 10 workload and native SDKs. On Linux, install
`maui-android` and build the explicit `net10.0-android` target. The project
also selects targets conditionally so Linux does not evaluate iOS, Mac Catalyst
or Windows workload packs. Final APK/device validation still needs the Android
SDK, JDK and a device or emulator. Keep the complete diagnostic log for CI and
packaging failures.

The demo references the MAUI library through a single-target Android project
reference when building on a host that cannot evaluate all Apple target
frameworks. The conditional project settings keep the shared code intact while
avoiding unrelated target-framework discovery during that build. A normal
developer workstation should prefer the multi-target project as declared in
the project files.

Package restores may need the configured NuGet feed to be reachable. A local
cache or an approved internal mirror can make reproducible offline checks
possible; it must not silently change the .NET 10 MAUI or
`SkiaSharp.Views.Maui.Controls` 4.150.1 version selected by the project.

# Runtime workarounds

- `PageTurnView.CancelInteraction()` drives the controller back to the resting
  endpoint. Call it when a page is removed, the view is recycled, or navigation
  interrupts a gesture.
- Supply `PreviousPageImage` as well as `UnderPageImage` when backward turns
  should reveal a different page. If the former is omitted, the control falls
  back to the latter for compatibility.
- The controller ignores non-finite coordinates and clamps all input to the
  current page. This is a defensive boundary for pointer and layout races.
- The control treats images as caller-owned. Keep them alive until the view no
  longer uses them; dispose them from the application image cache, not from a
  bindable-property callback.
- `SKGLView` is a pragmatic baseline. If a target deprecates its OpenGL path or
  the measured frame budget is missed, retain the core API and replace only the
  adapter with a Metal, Canvas, or platform-native renderer.

These workarounds are recorded so they are visible during review rather than
being mistaken for invisible production guarantees.
