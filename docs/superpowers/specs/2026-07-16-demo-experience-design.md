# PageCurl Demo Experience Design

**Date:** 2026-07-16
**Status:** Historical design record. The implemented demo behavior is documented
in the current source and validation records.
**Scope:** Simplify the solution, improve automated confidence, and refresh the MAUI sample experience.

## Goal

Make the sample application feel like a focused interactive reading surface while reducing the solution to the projects that carry production or test value.

## Project structure

The solution will contain four projects:

- `PageCurl.Core` — platform-free page-turn geometry, controller, and reusable page navigation state.
- `PageCurl.Maui` — the Skia-backed MAUI control and platform-facing lifecycle boundary.
- `PageCurl.Demo` — the sample application, generated sample pages, and presentation XAML.
- `PageCurl.Core.Tests` — the dependency-free executable test suite.

`PageCurl.Demo.Model` is removed. Its one class becomes the generic `PageNavigationState` in Core so the production navigation contract remains directly testable without making the test project reference MAUI.

## Visual direction

The sample becomes a “Midnight Reader” showcase:

- deep ink background with indigo/violet accents and a restrained warm page surface;
- a compact product header with a status badge and a prominent page counter;
- a framed reading stage with a floating page surface and quiet depth shadows;
- explicit left and right edge rails showing where a side drag begins;
- top-corner and bottom-corner curl markers that make edge/corner interaction discoverable without intercepting touch input;
- labeled Previous/Next controls and a reduced-motion switch below the stage;
- generated pages with editorial typography, accent bands, a page number, and visible edge/corner motifs so the curl is easy to see during a demo;
- light and dark theme resources, safe-area-aware layout, and accessible labels remain first-class.

The affordance overlay is `InputTransparent` so the real `PageTurnView` keeps ownership of every pointer. Side and corner starts are demonstrated by the same controller contract: the horizontal start position determines direction and the vertical coordinate is clamped, allowing any page edge or corner to start a turn.

## Automated confidence

The existing allocation, controller, geometry, and pointer-capture groups remain. Coverage is extended for:

- invalid navigation counts and direction-aware under-page clamping;
- `INotifyPropertyChanged` notifications for successful navigation only;
- forward and backward starts from all four page corners;
- finite, bounded geometry for corner-origin drags;
- completion event exactly-once behavior for immediate and animated turns.

The MAUI XAML is validated through warning-free neutral, Android, and iOS Release builds. The iOS smoke path remains the visual and interaction gate for theme, safe area, rotation, Previous/Next commands, reduced motion, and the large-page scenario.

## Non-goals

- No new UI framework, icon package, or image dependency.
- No native renderer rewrite.
- No claim that simulator evidence replaces physical-device profiling.
- No external publishing, merging, or pushing.
