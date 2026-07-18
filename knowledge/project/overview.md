---
type: Project Overview
title: PageCurl.Maui goals and scope
description: The problem PageCurl.Maui solves, the expected interaction, and the boundaries of the first production-oriented implementation.
tags:
  - goals
  - scope
  - maui
timestamp: 2026-07-18T00:00:00Z
---

# Goals

PageCurl.Maui makes an image feel like a page in a book. A user can start at
the appropriate edge, drag back and forth, see the finder position represented
continuously, and release to settle at the nearest page endpoint. The visual
model includes a curved sheet, a textured front and back, a page underneath,
and restrained per-vertex shading.

The first implementation targets Android and iOS through .NET MAUI and keeps
the platform-independent interaction and geometry reusable on other MAUI
targets. The demo provides a small, inspectable example rather than a second
application framework.

## Scope

- A deterministic drag/settle state machine with forward and backward turns.
- A bounded analytic page mesh suitable for a 60 Hz render loop.
- A SkiaSharp `SKGLView` adapter for MAUI.
- Caller-provided `SKImage` textures, so an application can choose its own
  decoding, caching, and memory policy.
- A dependency-free executable test runner for the core behavior.

## Non-goals

This repository is not a full document reader, image loader, pagination engine,
navigation system, or physics simulator. It does not promise that a source
image is safe to dispose while the control is drawing it. It also does not hide
the need for device profiling, accessibility review, or a native rendering
backend review before a high-profile release.

See the [architecture](architecture.md) and [release gates](release-readiness.md)
for the practical boundaries.
