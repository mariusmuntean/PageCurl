---
type: Roadmap
title: PageCurl.Maui next steps
description: Follow-up work that can improve visual fidelity, platform coverage, and distribution without weakening the core boundary.
tags:
  - roadmap
  - future
  - performance
timestamp: 2026-07-16T00:00:00Z
---

# Near-term

- Add a small benchmark harness for drag, mesh generation, and settle frames.
- Run Android and iOS device profiling and tune mesh density, spring frequency,
  and shading from measurements rather than screenshots alone.
- Add automated gesture/UI coverage on at least one Android emulator and one iOS
  simulator or device.
- Add explicit reduced-motion and keyboard/accessibility commands.

# Medium-term

- Introduce a renderer interface and evaluate a Metal/Canvas backend while
  keeping `PageTurnController` and `PageCurlGeometry` unchanged.
- Add optional image-to-texture helpers with documented decode, cache, and
  disposal ownership.
- Publish a compatibility matrix, package the library as a NuGet artifact, and
  add API compatibility checks.
- Improve optional page-edge shadows and perspective after profiling confirms
  the extra work fits the frame budget.

# Guardrail

New features should first state their effect on frame time, allocations,
memory, accessibility, and platform support. A visually impressive effect that
cannot meet those constraints belongs behind an opt-in backend or remains a
prototype.
