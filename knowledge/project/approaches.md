---
type: Design Decision
title: Rendering approaches considered
description: The alternatives considered for a cross-platform .NET MAUI page turn and the rationale for the selected baseline.
tags:
  - decisions
  - rendering
  - tradeoffs
timestamp: 2026-07-16T00:00:00Z
---

# Decision

The baseline is a pure C# interaction and geometry core rendered by a small
SkiaSharp MAUI adapter. It is the best balance for this control because the
hard part—pointer tracking, clamping, mesh generation, shading inputs, and
settling—can be tested without a device, while SkiaSharp supplies a mature
cross-platform drawing surface.

## Alternatives

### A native 3-D engine or platform renderer

Metal/SceneKit on Apple platforms, OpenGL/Vulkan/Filament on Android, or a
cross-platform 3-D engine could deliver richer lighting, depth, and shadows.
The cost is multiple renderers, platform-specific lifecycle code, larger app
footprints, and more difficult MAUI composition. This is a good future backend
when profiling demonstrates that the analytic mesh cannot meet a product bar.

### A WebView and JavaScript canvas/WebGL

WebGL libraries make a page curl easy to prototype and can offer polished
shaders. A WebView introduces another runtime, bridge latency for touch and
image transfer, platform-specific composition behavior, and a larger memory
budget. It is better suited to content already delivered as web UI than to a
native reading surface.

### A custom Skia-only renderer

Drawing the entire effect into a 2-D canvas is portable and small, but it puts
all texture mapping, perspective, hit testing, and animation math in one
place. The current split keeps that math in a reusable core and leaves room for
another renderer without rewriting interaction behavior.

## Consequences

The chosen approximation is intentionally not a physically complete cloth
simulation. It favors stable 60 Hz interaction, predictable memory use, and a
small API. It needs visual tuning, device profiling, and a future native/Metal
path if a product requires more realistic self-shadowing or very large pages.
