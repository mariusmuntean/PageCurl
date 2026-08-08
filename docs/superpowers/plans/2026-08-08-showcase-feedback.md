# Showcase Feedback Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Repair the four showcase scenes, embed licensed visual media, preserve live interaction, and remove the faceted dark backside band.

**Architecture:** Keep geometry in `PageCurl.Core`, native composition in `PageCurl.Maui`, and showcase content in `PageCurl.Demo`. Use fixed mesh buffers and the current completion path. Package a short local Big Buck Bunny excerpt and a still from the same source.

**Tech Stack:** .NET 10, .NET MAUI, SkiaSharp, CommunityToolkit.Maui.MediaElement, H.264 MP4, Core and neutral MAUI executable test runners.

## Global Constraints

- Work in `/Users/marius/Projects/PageCurl` on the current branch without a worktree.
- Keep `PageCurl.Core` free of MAUI, SkiaSharp, dispatcher, and device dependencies.
- Keep warmed geometry and render buffers bounded and allocation-free.
- Keep gesture, command, haptic, and accessibility turns on `PageTurnCompleted`.
- Credit Big Buck Bunny under Creative Commons Attribution 3.0 and describe the trim, mute, and transcode.
- Preserve the public PageCurl API unless a regression needs a narrow additive property.

---

### Task 1: Licensed image and video content

**Files:**
- Create: `src/PageCurl.Demo/Resources/Images/big_buck_bunny_still.jpg`
- Replace: `src/PageCurl.Demo/Resources/Raw/paper-motion.mp4`
- Create: `src/PageCurl.Demo/Resources/Raw/big_buck_bunny_demo.mp4`
- Create: `THIRD-PARTY-NOTICES.md`
- Modify: `src/PageCurl.Demo/Views/VideoPosterPageView.cs`
- Create: `src/PageCurl.Demo/Views/ImagePageView.cs`
- Modify: `src/PageCurl.Demo/MauiProgram.cs`

**Interfaces:**
- Consumes: `MediaElement`, MAUI image resources, and `ContentView`.
- Produces: `ImagePageView`, a cross-platform `VideoPosterPageView`, `big_buck_bunny_demo.mp4`, and `big_buck_bunny_still.jpg`.

- [ ] **Step 1: Download and verify the official source**

Download `https://download.blender.org/peach/bigbuckbunny_movies/BigBuckBunny_320x180.mp4.zip` into a temporary directory. Unzip it, inspect the codec and duration with `ffprobe`, and retain the source URL plus source SHA-256 in `THIRD-PARTY-NOTICES.md`.

- [ ] **Step 2: Produce the local assets**

Use `ffmpeg` to export seconds 60 through 68 as H.264 with no audio and `faststart`. Export the frame at second 64 as JPEG. Keep the MP4 below 2 MB and the JPEG below 500 KB. Remove `paper-motion.mp4` after the new clip exists.

- [ ] **Step 3: Add the stable image page**

Implement `ImagePageView : ContentView` with an `Image` whose source is `big_buck_bunny_still.jpg`, `AspectFit`, a paper background, and an attribution caption.

- [ ] **Step 4: Enable native video on Mac Catalyst**

Change `MauiProgram.CreateMauiApp()` so Android API 26+, iOS, and Mac Catalyst call `UseMauiCommunityToolkitMediaElement()`. Remove the `MACCATALYST` rejection from `VideoPosterPageView.SupportsVideoPlayback()`. Point the media source at `big_buck_bunny_demo.mp4`, retain the still underneath, and show attribution in the caption.

- [ ] **Step 5: Build the affected platform targets**

Run the explicit Android Release and Mac Catalyst Debug demo builds. Confirm the resources appear in each app bundle.

- [ ] **Step 6: Commit the media task**

Commit with `feat: add licensed showcase media`.

### Task 2: Stable live content and options input

**Files:**
- Modify: `tests/PageCurl.Maui.Tests/PageCurlViewTests.cs`
- Modify: `src/PageCurl.Maui/PageCurlView.cs`
- Modify: `src/PageCurl.Demo/MainPage.xaml.cs`

**Interfaces:**
- Consumes: `PageCurlView.Renderer`, `RefreshSnapshotAsync`, and the compact options overlay.
- Produces: hidden renderer state while flat, stable content instances, and stage input suppression while options are open.

- [ ] **Step 1: Write the failing flat-renderer regression**

Add `Flat_content_hides_renderer_until_turn()` to `PageCurlViewTests.RunAllAsync()`. Assert `view.Renderer.IsVisible == false` after construction. Provide a front snapshot, start a programmatic turn, assert visibility, cancel the interaction, and assert the completion path restores hidden state.

- [ ] **Step 2: Run the neutral MAUI runner and verify RED**

Run `dotnet run --project tests/PageCurl.Maui.Tests/PageCurl.Maui.Tests.csproj -c Release -f net10.0`. Expect the visibility assertion to fail because the renderer starts visible with zero opacity.

- [ ] **Step 3: Implement renderer visibility state**

Initialize `Renderer.IsVisible = false`. `ShowRenderer()` sets `IsVisible = true` before opacity. `ShowLiveContent()` clears opacity and sets `IsVisible = false`. Preserve the current completion event ordering.

- [ ] **Step 4: Verify GREEN**

Run the neutral MAUI runner and confirm all groups pass.

- [ ] **Step 5: Stabilize demo content instances**

Add `_imagePage` beside `_interactivePage` and `_videoPage`. Create all three in `EnsureResources()`. `UpdateContentScene()` assigns one of those instances. Dispose the video and clear all references during teardown.

- [ ] **Step 6: Protect compact options input**

Replace direct overlay toggling with `SetMobileOptionsOpen(bool open)`. Opening calls `CancelAllInteractions()`, sets `StageHost.InputTransparent = true`, and shows the overlay. Closing hides the overlay, then restores stage input. Scene changes and desktop transitions close the overlay through the same method.

- [ ] **Step 7: Delay live snapshot replacement until layout settles**

In `OnInteractiveContentChanged`, dispatch the refresh after one UI cycle. Keep `PageSnapshotState` generation cancellation so repeated taps retain the newest frame.

- [ ] **Step 8: Commit the content task**

Commit with `fix: stabilize live showcase content`.

### Task 3: Four focused scenes and a distinct backside

**Files:**
- Modify: `src/PageCurl.Demo/ShowcaseScene.cs`
- Modify: `src/PageCurl.Demo/MainPage.xaml`
- Modify: `src/PageCurl.Demo/MainPage.xaml.cs`
- Modify: `src/PageCurl.Demo/Views/CustomBacksideView.cs`
- Modify: `tools/ios-smoke.sh`

**Interfaces:**
- Consumes: shared anchor picker, `PageBacksideMode`, and `CustomBacksideView`.
- Produces: four scenes and a default custom backside.

- [ ] **Step 1: Remove the Corner pin scene**

Delete `CornerPin` from `ShowcaseScene`, the desktop and mobile buttons, `CornerPageView`, `CornerPinMarker`, corner-only options, navigation state, handlers, labels, selection styles, cancellation, and teardown code. Keep corner values in `AnchorValues`.

- [ ] **Step 2: Default Backside to custom content**

Initialize `_backsideMode` to `PageBacksideMode.CustomContent`. Add one `_customBacksidePage` instance in `EnsureResources()` and reuse it in `UpdateBacksideScene()`.

- [ ] **Step 3: Strengthen the backside design**

Change the label to `CUSTOM BACKSIDE`, retain the green paper, and add copy that identifies a separate MAUI view. Keep the design readable after the renderer applies dimming.

- [ ] **Step 4: Update the smoke scene list**

Remove corner accessibility navigation and `corner.png`. Keep image, view, video, backside, spread, light, dark, and options captures.

- [ ] **Step 5: Commit the scene task**

Commit with `refactor: focus the showcase scenes`.

### Task 4: Preserve the stationary spread page

**Files:**
- Modify: `tests/PageCurl.Maui.Tests/PageSpreadViewTests.cs`
- Modify: `src/PageCurl.Maui/PageSpreadView.cs`

**Interfaces:**
- Consumes: `PageSpreadView.Overlay` and the existing static page hosts.
- Produces: transparent overlay composition with one static page visible during turns.

- [ ] **Step 1: Write the failing overlay regression**

Add `Overlay_uses_a_transparent_surface()` to `RunAllAsync()`. Construct a spread and assert `spread.Overlay.SurfaceColor.Alpha == 0`.

- [ ] **Step 2: Verify RED**

Run the neutral MAUI runner. Expect the new assertion to fail because `PageTurnView` defaults to opaque `#EBEBEB`.

- [ ] **Step 3: Implement transparent spread composition**

Set `Overlay.SurfaceColor = Colors.Transparent` in the spread constructor. Keep the overlay spanning both columns. Preserve the current host visibility rules in `PrepareTurn()` and `RestoreStaticPages()`.

- [ ] **Step 4: Verify GREEN**

Run the neutral MAUI runner and confirm all groups pass.

- [ ] **Step 5: Commit the spread task**

Commit with `fix: preserve stationary spread page`.

### Task 5: Smooth readable paper lighting

**Files:**
- Modify: `tests/PageCurl.Core.Tests/PageCurlGeometryTests.cs`
- Modify: `tests/PageCurl.Core.Tests/PageTurnPerformanceTests.cs`
- Modify: `src/PageCurl.Core/PageCurlGeometry.cs`
- Modify: `src/PageCurl.Core/PageTurnTypes.cs`

**Interfaces:**
- Consumes: `MeshVertex.Shade`, `PageMeshBuffer`, and `PageCurlOptions`.
- Produces: `ComputeCylinderShade(float theta)` inside Core and a 64 by 64 default fixed mesh.

- [ ] **Step 1: Write failing lighting regressions**

Add `Cylinder_backside_retains_readable_ambient_light()` and `Cylinder_tangent_shading_is_continuous()` to the Core runner. Build a deep cylinder curl and assert each back vertex has `Shade >= 0.54f`. Compare vertices on the tangent seam and assert their shade delta stays below `0.001f`.

- [ ] **Step 2: Verify RED**

Run the Core runner. Expect the ambient test to fail with shades near `0.15f`.

- [ ] **Step 3: Implement one cylinder light function**

Use `0.55f + (0.45f * MathF.Abs(MathF.Cos(theta * 0.5f)))` for curled cylinder vertices. Use the same function at `theta = PI / 2` for clipped tangent vertices and `theta = PI` for the turned flat region.

- [ ] **Step 4: Increase the default fixed mesh**

Set `PageCurlOptions.MeshColumns` and `MeshRows` defaults to `64`. Do not change validation limits or allocate during `Build()`.

- [ ] **Step 5: Verify geometry and allocation guards**

Run the Core runner. Confirm geometry groups and warmed allocation checks pass with the new defaults.

- [ ] **Step 6: Commit the lighting task**

Commit with `fix: smooth page underside lighting`.

### Task 6: Native visual evidence and documentation

**Files:**
- Modify: `README.md`
- Modify: `docs/testing.md`
- Modify: `docs/compatibility.md`
- Modify: `docs/validation/ios-release-readiness.md`
- Modify: `docs/requirements-status.md`

**Interfaces:**
- Consumes: completed code, local assets, and completed validation output.
- Produces: current usage text and evidence records.

- [ ] **Step 1: Run the full validation matrix**

Run Core, neutral MAUI tests, neutral MAUI Release build, Android Release build, Mac Catalyst Debug build, and `tools/ios-smoke.sh` with a dated output directory.

- [ ] **Step 2: Inspect native screenshots**

Inspect light, dark, options, spread-during-curl, image, view, video, and backside captures. Reject blank media, a missing stationary spread page, unreadable backside content, or a sawtooth lighting band.

- [ ] **Step 3: Validate on the paired iPhone**

Build Debug `ios-arm64`, install and launch `de.marius.PageCurlDemo`, then exercise content switching, repeated marks, video playback, spread turns, custom backside, and a deep curl.

- [ ] **Step 4: Update documentation from completed evidence**

Describe four scenes, licensed assets, Mac Catalyst video, transparent spread composition, and the new lighting profile. Record follow-ups where automation cannot prove a manual result.

- [ ] **Step 5: Run final repository checks**

Run `git diff --check`, validate Markdown links, confirm no stale `CornerPin`, `paper-motion.mp4`, or five-scene claims remain, and inspect `git status`.

- [ ] **Step 6: Commit the validation task**

Commit with `docs: record corrected showcase validation`.
