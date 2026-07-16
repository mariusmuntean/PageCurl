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
Skia adapter, demo, and deterministic core tests. Source-level neutral and
Android library builds are the first validation gates. Apple builds and final
Android packaging require the corresponding native SDK/workload host.

This is a strong engineering baseline, not a claim of Kindle-level polish.

# Release gates

Before shipping a package or embedding the control in a flagship reader:

1. Build and package Android and iOS in clean CI environments with pinned .NET,
   MAUI, and SkiaSharp versions.
2. Run the automated core suite and device smoke tests on supported OS versions.
3. Profile gesture and settle paths on low-end Android and current iOS devices;
   verify no sustained frame drops, allocations, or texture leaks.
4. Complete visual review for page edges, shadows, texture scaling, dark mode,
   rotation, safe areas, large pages, and reduced motion.
5. Validate touch/accessibility behavior, including an alternate non-gesture
   command for screen-reader users and users who enable reduced motion.
6. Document image lifetime, threading, and navigation/recycling rules in the
   consuming application.
7. Choose and publish a license, versioning policy, compatibility matrix, and
   support policy before public distribution.

# Known risks

`SKGLView` is a practical cross-platform backend but relies on the graphics
path exposed by each MAUI target. Apple OpenGL deprecation, unusual GPU driver
behavior, very large textures, and high-DPI mesh density need real-device
measurement. The core approximation does not model every physical fold; a
product requiring realistic self-shadowing should add a native/Metal backend
behind the same core contract. These risks are acceptable for the baseline only
when they remain visible in release tracking.
