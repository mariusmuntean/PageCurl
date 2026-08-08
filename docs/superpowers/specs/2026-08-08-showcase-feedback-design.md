# Showcase feedback design

Status: approved on 2026-08-08

## Goal

The sample must prove the library's content, backside, spread, and anchor APIs without duplicating scenes or hiding broken behavior. The renderer must keep the responsive paper motion that passed device review while removing the dark faceted band on the underside.

## Scope

This pass changes the demo, the spread composition, and the cylinder lighting profile. It preserves the public content and navigation APIs. `PageAnchor` and the shared anchor picker continue to support corner anchors.

## Scene structure

The sample will contain four scenes: Single, Spread, Content, and Backside.

Single keeps the shared locked-edge-or-corner picker. Users can select all four edges and all four corners there, so the separate Corner pin scene adds no capability. The desktop rail, mobile strip, navigation state, labels, smoke checks, and documentation will drop the fifth scene.

## Content scene

The Content scene will keep one stable view instance for each content type. Reusing the views avoids repeated handler attachment and image-loading races.

- Image opens by default and shows a real Big Buck Bunny frame inside a designed paper page.
- View keeps the live `Add a mark` control.
- Video plays the silent excerpt from 01:00 through 01:08 of the Blender Foundation's 320x180 Big Buck Bunny release. The app will embed the clip and will not need a network connection at runtime.

The implementation will download `BigBuckBunny_320x180.mp4.zip` from Blender's official movie directory and produce `Resources/Raw/big_buck_bunny_demo.mp4` plus `Resources/Images/big_buck_bunny_still.jpg`. The still will show the frame at 01:04. `THIRD-PARTY-NOTICES.md` and the in-app caption will credit Blender Foundation and the film's creators under Creative Commons Attribution 3.0. The notice will state that the demo trims, mutes, and transcodes the source.

`MediaElement` will run on Android, iOS, and Mac Catalyst. The demo will remove its Mac Catalyst playback block. A poster remains beneath the native video surface so snapshot capture and playback failure leave a useful page instead of an empty rectangle.

The flat content host will hide the Skia renderer with `IsVisible`, rather than leaving an opacity-zero native surface above the live controls. The renderer will appear during a turn. This prevents an old GPU snapshot from flashing while the counter changes.

Opening the compact options sheet will cancel any active turn and make the stage input-transparent until the sheet closes. The content selector will remain usable after page interaction. A content change will wait for the new view's layout before the demo requests a replacement snapshot.

## Backside scene

Backside will open in `CustomContent` mode. Its reverse will use a persistent, high-contrast `CustomBacksideView` with an explicit `CUSTOM BACKSIDE` label, different typography, and a different paper color. Mirror and Color remain available so users can compare all three library modes.

The demo will capture the persistent backside once per relevant layout change instead of constructing another view after each page completion.

## Spread composition

`PageSpreadView` will keep the untouched page and gutter visible while the other page turns. Its full-spread overlay needs a transparent surface because the overlay spans both columns so the moving sheet can cross the gutter. The overlay will draw the destination page and curled sheet while the static host remains visible underneath.

Forward turns hide the flat right-page host and preserve the left host. Backward turns hide the flat left-page host and preserve the right host. Completion restores both hosts through the current completion path.

## Paper lighting

The cylinder mesh will use a readable ambient term across the underside. The darkest geometry shade will stay near 0.55 instead of 0.15, while `BacksideDimFactor` will continue to control paper-side dimming. The tangent intersection will use the same lighting function as neighboring vertices so the front/back split has no shade jump.

The default fixed mesh will increase from 48 by 48 to 64 by 64. The buffers remain bounded, reused, and allocation-free after warm-up. The combination of a softer light curve and smaller facets must remove the sawtooth band without changing the curl pose or pointer response.

## Tests and evidence

Core regressions will cover the underside ambient floor, tangent lighting continuity, finite geometry, buffer reuse, and warmed allocation guard with the new default mesh.

Neutral MAUI regressions will cover the hidden flat renderer, content replacement, transparent spread overlay, and restoration of the untouched spread page. Demo-specific behavior that depends on native controls will use repeatable visual scenarios.

Validation will run:

- the dependency-free Core and neutral MAUI runners;
- the neutral MAUI build plus explicit Android and Mac Catalyst builds;
- the full iOS simulator smoke with updated four-scene screenshots;
- a signed iPhone build, install, launch, and manual checks for content switching, the live counter, video playback, spread turns, backsides, and curl lighting.

The README, testing notes, compatibility status, and iOS validation record will match the completed evidence. The validation record will not claim device behavior that the run did not exercise.
