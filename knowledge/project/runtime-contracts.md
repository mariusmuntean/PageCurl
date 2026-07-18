---
type: Runtime Contract
title: Runtime ownership and lifecycle contracts
description: Stable runtime rules for image ownership, cancellation, recycling, and opt-in diagnostics.
tags:
  - runtime
  - lifecycle
  - ownership
  - rendering
timestamp: 2026-07-18T00:00:00Z
---

# Ownership and lifecycle

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
- Enable `PageTurnView.PerformanceDiagnosticsEnabled` only while collecting
  render evidence. The default paint path avoids stopwatch/allocation reads and
  the diagnostics values are exposed as an immutable snapshot.

These contracts are intentionally separate from current machine and workload
workarounds. They describe behavior a consuming application must preserve.
