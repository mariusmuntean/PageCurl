---
type: Engineering Workaround
title: Workarounds and known solutions
description: Explicit handling for MAUI workload behavior, rendering lifecycle, image ownership, and constrained development environments.
tags:
  - workarounds
  - tooling
  - platform
timestamp: 2026-07-18T00:00:00Z
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

`global.json` pins SDK 10.0.301. On a machine where the system SDK is
root-owned or has no workloads, use a writable user-local SDK installation and
install `maui-ios maui-android` there. Set `DOTNET_ROOT` or `PATH` for that
installation rather than copying a machine-specific path into shared project
knowledge. Explicit mobile restores/builds keep each referenced project on its
own target assets; overriding a multi-target root project can otherwise leave a
referenced `net10.0` core assets file without its neutral target.

Runtime ownership, cancellation, image lifetime, and diagnostics behavior are
documented in [runtime contracts](runtime-contracts.md). Keeping those rules
separate from host/toolchain workarounds makes each concept easier to retrieve
and update.
