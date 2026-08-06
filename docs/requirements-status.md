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
- Forward and backward destination images, caller-owned image lifetime, and
  aspect-fit rendering/pointer mapping.
- Fixed render buffers, allocation-free warmed core paths, depth ordering,
  exact front/back tangent clipping, and deep-curl underside layering.
- Accessible Previous/Next commands, edge settings, opt-in diagnostics, light
  and dark resources, safe-area layout, and generated large-page content.
- .NET 10 package metadata, MIT license, support policy, compatibility matrix,
  CI definitions, and iOS smoke tooling.

## In progress

- MAUI properties for corner anchors, aspect ratio, thresholds, and flicks.
- Snapshot-backed MAUI content, configurable backsides, and two-page spreads.
- Responsive five-scene sample for compact mobile and resizable desktop.

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
