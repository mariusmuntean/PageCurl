---
type: Architecture Decision
title: PageCurl.Maui architecture
description: How interaction, geometry, rendering, and sample application concerns are separated.
tags:
  - architecture
  - separation-of-concerns
  - performance
timestamp: 2026-07-16T00:00:00Z
---

# Layered design

The solution has four deliberately narrow layers:

1. [`PageCurl.Core`](../../src/PageCurl.Core/PageCurl.Core.csproj) is a
   `net8.0` library with no UI dependency. [`PageTurnController`](../../src/PageCurl.Core/PageTurnController.cs)
   owns pointer state, direction, progress, cancellation, spring settling, and
   completion events. [`PageCurlGeometry`](../../src/PageCurl.Core/PageCurlGeometry.cs)
   turns a snapshot into a bounded cylinder-like mesh.
2. [`PageCurl.Maui`](../../src/PageCurl.Maui/PageCurl.Maui.csproj) adapts the
   core to MAUI. [`PageTurnView`](../../src/PageCurl.Maui/PageTurnView.cs) owns
   the fixed Skia buffers, image shaders, touch subscription, invalidation, and
   dispatcher-driven settling loop. It does not reimplement page math.
3. [`PageCurl.Demo`](../../src/PageCurl.Demo/PageCurl.Demo.csproj) shows the
   public API with generated pages. It is useful for visual inspection but is
   not a library dependency.
4. [`PageCurl.Core.Tests`](../../tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj)
   exercises the core with a small executable runner. Tests do not need MAUI,
   a graphics context, a device, or a test adapter.

## Frame flow

`SKTouchEventArgs` enters the MAUI adapter, which forwards only validated
coordinates to the controller. A paint callback reads a snapshot, asks the
geometry layer to fill a reusable mesh, and draws the under-page followed by
depth-ordered back and front triangle streams. On release, the adapter starts a
single 60 Hz timer only while the controller is settling. The timer stops when
the spring reaches its endpoint and one completion event is raised.

## Ownership and portability

The view never disposes caller-owned `SKImage` values. Shader instances are
owned by the view and are replaced when an image reference changes. The core
uses `System.Numerics` and fixed-size arrays, so another renderer can consume
the same pose and mesh without taking a MAUI or Skia dependency.

This boundary is the principal portability strategy: Android and iOS share the
same tested math while the adapter follows the graphics implementation supplied
by the MAUI workload.

## Performance constraints

The default mesh is intentionally modest and bounded. Mesh, point, UV, and
color buffers are allocated at construction, not per frame. The render loop
does not run while idle, and the geometry path uses finite-value clamps so bad
input cannot create unbounded coordinates. Any future visual detail should be
introduced behind an option and measured on representative low- and high-end
devices.
