---
type: Release Readiness
title: PageCurl.Maui release gates
description: What is implemented today and what must be demonstrated before the control belongs in a production reading app.
tags:
  - release
  - risk
  - platform
timestamp: 2026-07-18T00:00:00Z
---

# Purpose

This concept defines the release gates and known risks. Dated command output,
screenshots, and environment details belong in `docs/validation/`; the
compatibility matrix belongs in `docs/compatibility.md`. This document should
not be treated as a replacement for fresh evidence.

# Release gates

## Local evidence

Current local evidence is recorded in
[`docs/validation/ios-release-readiness.md`](../../docs/validation/ios-release-readiness.md)
and the compatibility matrix in [`docs/compatibility.md`](../../docs/compatibility.md).
The validation record, not this concept, is the source of pass/fail claims for
the latest local run.

Before shipping a package or embedding the control in a flagship reader:

1. Build and package Android and iOS in clean CI environments with pinned .NET
   10, MAUI 10, and SkiaSharp 4.150.1 versions.
2. Run the automated core suite and device smoke tests on supported OS versions.
3. Profile gesture and settle paths on low-end Android and current iOS devices;
   verify no sustained frame drops, allocations, or texture leaks.
4. Complete visual review for page edges, shadows, texture scaling, dark mode,
   rotation, safe areas, large pages, reduced motion, aspect-fit content, and
   responsive MacCatalyst window sizes.
5. Validate touch/accessibility behavior, including the labeled Previous and
   Next commands, the interactive page description, edge-lock selector,
   diagnostics surface, decorative-hint suppression, and reduced-motion mapping
   for screen-reader users and users who enable reduced motion.
6. Document image lifetime, threading, and navigation/recycling rules in the
   consuming application.
7. Choose and publish a license, versioning policy, compatibility matrix, and
   support policy before public distribution. The repository's current
   distribution artifacts are `LICENSE`, `SUPPORT.md`, `docs/compatibility.md`,
   and the package metadata; verify them against the release being shipped.

# Known risks

`SKGLView` is a practical cross-platform backend but relies on the graphics
path exposed by each MAUI target. Apple OpenGL deprecation, unusual GPU driver
behavior, very large textures, and high-DPI mesh density need real-device
measurement. The core curl uses a reusable cylinder approximation with a
weighted stationary-hinge anchor for adjacent free-edge starts; a product
requiring physically exact multi-axis self-shadowing should add a native/Metal
backend behind the same core contract. These risks are acceptable for the
baseline only when they remain visible in release tracking.
