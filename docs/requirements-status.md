# Requirements status

This page tracks current product requirements. Historical plans are preserved
in Git history and do not define unfinished work.

## Implemented

- Platform-free controller and geometry with a MAUI/Skia adapter.
- Direct, developable stiff-paper curl with bounded radius and no cross-frame
  cloth state.
- Every free edge and corner, explicit stationary edge anchors, pinned-corner
  anchors, and Auto selection in the core API.
- Direct pointer tracking, reversal, cancellation, configurable normalized
  thresholds, threshold transition events, configurable flicks, reduced
  motion, and programmatic turns through one completion path.
- Explicit, intrinsic, and fallback page width/height ratio layout in the core.
- MAUI properties for edge/corner anchors, page ratio, normalized threshold,
  threshold events, flick enablement, and flick velocity.
- Forward and backward destination images, caller-owned image lifetime, and
  aspect-fit rendering/pointer mapping.
- Mirrored/dim, solid-color, and custom-content backsides.
- Snapshot-backed MAUI front/back content with live flat-state controls,
  last-good-frame retention, explicit refresh, and Android/Apple/Windows
  capture implementations.
- A two-page spread with right-forward and left-backward turns, full-gutter
  overlay rendering, readiness-aware commands, and a restrained gutter shadow.
- A responsive five-scene showcase for single pages, spreads, images, live
  `ContentView` controls, bundled video with poster fallback, configurable
  backsides, and all four corner pins.
- Mobile haptics with distinct threshold and committed-completion patterns,
  plus a user-facing enable switch.
- Fixed render buffers, allocation-free warmed core paths, depth ordering,
  exact front/back tangent clipping, and deep-curl underside layering.
- Accessible Previous/Next commands, edge/corner settings, light and dark
  resources, safe-area layout, compact options sheet, desktop inspector, and
  generated high-resolution content.
- .NET 10 package metadata, MIT license, support policy, compatibility matrix,
  CI definitions, and iOS smoke tooling.

## Release evidence still required

- Physical-device frame, GPU, thermal, memory, and long-run texture profiling.
- Low-end Android and current physical iOS validation.
- VoiceOver and TalkBack review on supported hardware.
- Windows build and visual validation.
- Store signing, archive validation, publication rehearsal, and API
  compatibility automation.
- Reliable active-gesture screenshot or UI automation coverage.

## Deliberate non-goals

The library is not a document reader, paginator, image loader/cache, collision
solver, full cloth simulator, or physically complete multi-light renderer.
