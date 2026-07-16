---
type: Testing Strategy
title: Testing and validation
description: The test layers, invariants, commands, and device checks needed to keep page turning smooth and correct.
tags:
  - testing
  - quality
  - performance
timestamp: 2026-07-16T00:00:00Z
---

# Why testing matters

A page turn is continuously visible: a one-pixel discontinuity, a reversed
texture, a missed cancellation, or a frame-time spike is immediately apparent
to a reader. Deterministic tests protect the geometry and state transitions
while visual and device tests protect the feel. Both are required for a
high-profile reading app; passing unit tests alone is not a performance claim.

## Current automated coverage

[`PageCurl.Core.Tests`](../../tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj)
is a dependency-free executable runner. It checks:

- edge selection, non-finite input rejection, and pointer clamping;
- forward and backward normalized progress;
- threshold snapping, cancellation, spring convergence, and one-shot
  completion events;
- immediate reduced-motion completion and forward/backward programmatic turns;
- reusable page-navigation boundaries, property notifications, and
  direction-aware under-page selection;
- finite and bounded mesh coordinates, UV and shading ranges, and front/back
  triangle counts;
- pointer capture accepts one finger, ignores other fingers, and releases the
  owner on completion or cancellation;
- reuse of mesh buffers so a frame does not allocate geometry arrays;
- warmed geometry and controller loops that assert zero managed allocations over
  1,000 iterations.

Run it with:

```bash
dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj -c Release
```

The core and explicit .NET 10 Android MAUI builds should also be part of
pull-request CI:

```bash
dotnet build src/PageCurl.Core/PageCurl.Core.csproj -c Release
dotnet workload install maui-android
dotnet build src/PageCurl.Maui/PageCurl.Maui.csproj -c Release -f net10.0 -p:TargetFrameworks=net10.0
dotnet build src/PageCurl.Maui/PageCurl.Maui.csproj -c Release -f net10.0-android
dotnet build src/PageCurl.Demo/PageCurl.Demo.csproj -c Release -f net10.0-android
./tools/ios-smoke.sh
```

The iOS smoke script builds the explicit simulator target, installs and launches
the demo, and captures light/dark portrait screenshots. Manual follow-up still
covers button activation, reduced motion, cancellation, repeated turns,
rotation, large pages, image replacement, and safe-area review.

## Device and visual validation

On every supported release target, exercise a short drag, a reversal, a
threshold release in both directions, cancellation during settling, repeated
turns, rotation, background/foreground transitions, image replacement, and
both direction-specific under-pages.
Profile cold and warm frames on representative low-end Android and current iOS
hardware. Record frame time, allocations, memory growth, and GPU/CPU usage.
Review texture orientation, backside readability, shadows, touch ownership,
reduced-motion behavior, VoiceOver/TalkBack alternatives, and dynamic page
sizes. Add an emulator or device UI test when the test infrastructure is
available; keep the core tests fast enough to run on every commit.

## Regression policy

Every geometry or interaction bug should gain a small deterministic regression
case before the fix is merged. Changes that alter visual output should include
before/after captures or a reproducible manual test note in the pull request.
