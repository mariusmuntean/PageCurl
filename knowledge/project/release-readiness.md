---
type: Release Readiness
title: PageCurl.Maui release gates
description: What is implemented today and what must be demonstrated before the control belongs in a production reading app.
tags:
  - release
  - risk
  - platform
timestamp: 2026-07-16T00:00:00Z
---

# Current status

The repository contains a working core state machine, analytic page mesh, MAUI
Skia adapter, modular demo, deterministic tests, pinned SDK/workloads, package
metadata, and an iOS simulator smoke workflow. The local macOS validation has
passed the core suite, neutral MAUI build, explicit iOS Release demo build,
iOS simulator install/launch, light/dark portrait captures, explicit Android
Release packaging, and the simulator accessibility/rotation pass. Physical
device profiling and hardware smoke remain separate evidence gates.

This is a strong engineering baseline, not a claim of Kindle-level polish.

# Release gates

## Local evidence

The current local evidence is recorded in
[`docs/validation/ios-release-readiness.md`](../../docs/validation/ios-release-readiness.md)
and the compatibility matrix in [`docs/compatibility.md`](../../docs/compatibility.md).
The core test runner includes warmed allocation guards for geometry and
controller loops; both report zero managed bytes over 1,000 iterations.

Before shipping a package or embedding the control in a flagship reader:

1. Build and package Android and iOS in clean CI environments with pinned .NET
   10, MAUI 10, and SkiaSharp 4.150.1 versions.
2. Run the automated core suite and device smoke tests on supported OS versions.
3. Profile gesture and settle paths on low-end Android and current iOS devices;
   verify no sustained frame drops, allocations, or texture leaks.
4. Complete visual review for page edges, shadows, texture scaling, dark mode,
   rotation, safe areas, large pages, and reduced motion.
5. Validate touch/accessibility behavior, including the labeled Previous and
   Next commands, semantic descriptions, and reduced-motion mapping for
   screen-reader users and users who enable reduced motion.
6. Document image lifetime, threading, and navigation/recycling rules in the
   consuming application.
7. Choose and publish a license, versioning policy, compatibility matrix, and
   support policy before public distribution. The repository now provides MIT
   licensing, semver 0.1.0 metadata, a compatibility matrix, and SUPPORT.md.

# Known risks

`SKGLView` is a practical cross-platform backend but relies on the graphics
path exposed by each MAUI target. Apple OpenGL deprecation, unusual GPU driver
behavior, very large textures, and high-DPI mesh density need real-device
measurement. The core approximation does not model every physical fold; a
product requiring realistic self-shadowing should add a native/Metal backend
behind the same core contract. These risks are acceptable for the baseline only
when they remain visible in release tracking.
