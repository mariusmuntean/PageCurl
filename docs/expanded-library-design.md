# Expanded PageCurl library and showcase design

**Status:** Approved
**Date:** 2026-08-06

## Goal

PageCurl should cover the common reading surfaces without turning its basic API
into a framework. It must support one page, a two-page book spread, images,
snapshotted MAUI content, video previews, configurable backsides, edge and
corner hinges, exact pointer tracking, flicks, thresholds, aspect ratios, and
mobile haptic feedback.

The demo uses one responsive Showcase shell. Desktop gets a slim scene rail and
an optional inspector. Mobile gets a bottom scene strip and bottom-sheet
options. The page remains the visual focus.

## Control layers

### `PageTurnView`

The existing image-based `SKGLView` remains the low-level renderer. Existing
`SKImage` properties and completion behavior remain compatible. It gains only
the renderer-level properties and events needed by the higher controls:

- `LockedAnchor`
- `PageChangeThreshold`
- flick configuration
- backside presentation
- threshold-transition events
- explicit interaction methods used by composite controls

`LockedEdge` and `PageCurlOptions.SnapThreshold` remain compatibility facades.

### `PageCurlView`

`PageCurlView` is the single-page MAUI content control. It accepts:

- `FrontContent`, any MAUI `View`, including `Image`, `ContentView`, and video
  controls;
- optional `BackContent`;
- `BacksideMode`, `BacksideColor`, and `BacksideDimFactor`;
- `PageAspectRatio`, expressed as width divided by height;
- interaction, threshold, and flick settings forwarded to the renderer.

Content stays live while the page rests. The control caches a texture after
layout or content changes. On gesture start, it shows the cached texture in the
curl renderer without waiting for a new capture. After completion or
cancellation, it returns to live content.

`RefreshSnapshotAsync()` lets applications refresh dynamic content while idle.
The video demo refreshes its cached preview on a low-frequency idle timer. If a
platform cannot capture the video layer, the application supplies a poster view
or image. The curl never blocks on video decoding or screen capture.

Interactive page content remains usable. Composite controls start page gestures
from edge/corner activation strips; the center stays available to child views.
The low-level image control retains its existing anywhere-on-page interaction.

### `PageSpreadView`

`PageSpreadView` hosts left and right `PageCurlView` content and a full-spread
curl overlay. Forward turns curl the right sheet across the gutter; backward
turns curl the left sheet. The overlay spans both halves so the moving sheet is
not clipped at the gutter.

The spread exposes one completion event and direction-aware content properties
for the current and destination spread. A restrained, configurable gutter
shadow sits above the static pages and below the active sheet.

## Geometry

`PageAnchor` contains `Auto`, four edges, and four corners.

- An edge anchor pins the full edge and uses the direct developable cylinder.
- A corner anchor pins one point and uses a direct developable cone.
- The locked anchor cannot initiate a drag.
- Other valid edges and corners can start a gesture.

Both mappings are analytic, bounded, and allocation-free. They preserve sheet
distance by construction and contain no cross-frame cloth state. The page
tracks the finger directly until it reaches the physical mapping limit, then
remains bounded rather than stretching.

The renderer retains exact tangent clipping, front/underside streams, depth
ordering, fixed storage, and stationary hinge tests. Corner-pin work adds
deterministic distance, pin, pointer, orientation, and finite-bound coverage.

## Threshold and flick behavior

`PageChangeThreshold` is a normalized value from zero to one. It represents the
fraction of page travel required to make completion the resting intent. The
demo renders it as a percentage slider.

Crossing the threshold raises `PageChangeThresholdCrossed` once per transition
with the direction and whether the gesture is now a commit candidate. Reversing
across the threshold raises the opposite transition. Repeated move samples on
the same side do not repeat the event.

Flick intent uses normalized page-progress velocity. `FlickEnabled` defaults to
true. `FlickVelocityThreshold` remains an advanced setting. Flicks choose the
target but do not detach the page from the finger or skip the visible settle.

## Backside presentation

`PageBacksideMode` supports:

- `MirroredFront`: flip the front texture horizontally and multiply its shade
  by `BacksideDimFactor`;
- `SolidColor`: render `BacksideColor` without a content texture;
- `CustomContent`: render the cached `BackContent` snapshot.

The default is `MirroredFront` with mild dimming. Custom backside content uses
the same ownership and snapshot lifecycle as front content.

## Aspect ratio

`PageAspectRatio` means width divided by height. A positive value explicitly
controls the page rectangle. Zero selects automatic sizing from captured
content or image dimensions, with a 0.75 fallback when no intrinsic ratio is
available. Single and spread layouts use the same ratio calculation for
rendering, hit testing, capture, shadows, and diagnostics.

## Haptics

Core raises semantic events and remains platform-independent. The demo maps
them to MAUI haptics on supported mobile targets:

- threshold entry: short click;
- threshold exit: a lighter click when the platform exposes one, otherwise the
  same short click;
- committed completion: long press or stronger feedback;
- cancelled completion: no completion haptic.

The demo performs haptics only on state transitions, never on every move. A
setting disables haptics. Unsupported platforms silently skip feedback.

## Showcase scenes

The shell contains five scenes:

1. **Single page:** forward/backward paging, every edge/corner anchor, threshold
   slider, flicks, and aspect ratio.
2. **Book spread:** right-page forward and left-page backward turns with a soft
   gutter shadow.
3. **Content:** image, interactive MAUI `ContentView`, and video/poster snapshot
   examples.
4. **Backside:** mirrored/dim, solid-color, and custom-content backsides.
5. **Corner pin:** focused conical-curl demonstration with clear anchor state.

Diagnostics stay behind the options surface. Every visible button performs a
real action and reflects disabled boundary states.

## Responsive presentation

The shell uses warm canvas and paper colors, ink typography, and one cobalt
accent. It uses no gradients, decorative badges, or dense card dashboard.

Desktop shows a compact scene rail, centered stage, short footer, and optional
right-side inspector. Mobile shows a compact header, dominant stage, short
status line, Previous/Next row, and four-item bottom scene strip. Additional
scenes and settings open from `More` as a bottom sheet.

The same state model drives both layouts. Width breakpoints change composition,
not behavior. Safe areas, orientation, keyboard focus, screen readers, pointer
input, and touch targets remain part of validation.

## Failure and lifecycle behavior

- A failed content capture keeps the last valid snapshot and raises a
  non-fatal snapshot error event. With no cached frame, the control uses its
  configured placeholder and does not begin a curl.
- Content replacement cancels the active gesture before replacing snapshots.
- The controls never dispose caller-owned images or views.
- Handler disconnection cancels interaction, stops capture timers, releases
  renderer-owned textures, and leaves caller content intact.
- Spread navigation commits content only from the shared completion event.

## Validation

Core tests cover anchors, cone/cylinder material distances, direct pointer
tracking, threshold transitions, flicks, backside UV/shade decisions, aspect
ratio layout, spread navigation, finite bounds, fixed-buffer reuse, and managed
allocations.

Neutral MAUI builds cover the public controls and snapshot abstractions.
Mac Catalyst visual checks cover wide and compact desktop windows. iOS and
Android checks cover portrait/landscape layout, safe areas, content snapshots,
video fallback, spread turns, haptics, reduced motion, accessibility, and
lifecycle transitions. Physical-device performance remains a release gate.

## Non-goals

- Live video deformation during a curl.
- A document paginator, reader data source, image cache, or video player.
- Cloth simulation, tearing, creases, plasticity, or multi-page collision.
- Physically complete lighting or self-shadowing.
