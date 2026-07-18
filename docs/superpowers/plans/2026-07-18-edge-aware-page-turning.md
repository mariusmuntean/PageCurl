# Edge-Aware Page Turning Implementation Plan

> **Status:** Implementation record. The remaining unchecked items are manual
> or device evidence gates; do not treat this file as the current project
> policy or validation command source.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Make PageCurl support natural curls from all four edges and corners, optional stationary hinge edges, aspect-preserving content, and a responsive accessible demo.

**Architecture:** Keep navigation direction (`Forward`/`Backward`) separate from the physical hinge (`PageEdge`). The core controller selects and tracks the hinge; geometry transforms page coordinates into a hinge-local frame, builds the existing reusable cylinder mesh, and transforms vertices back. The MAUI adapter receives a content rectangle, while the demo owns edge-lock controls, gesture hints, diagnostics layout, and accessibility copy.

**Tech Stack:** .NET 10, C# `System.Numerics`, .NET MAUI 10, SkiaSharp 4.150.1, dependency-free executable core tests, iOS simulator smoke validation.

## Global Constraints

- Preserve the existing `Forward`/`Backward`, `TryTurn`, `PageTurnCompleted`, `ReduceMotion`, and image ownership APIs.
- Keep the core independent of MAUI, SkiaSharp, dispatchers, and device APIs.
- Do not allocate per frame in controller, geometry, or the normal renderer path.
- Keep diagnostics opt-in and disabled in normal demo/library use.
- Treat `LockedEdge` as the stationary hinge; `Auto` selects the hinge from the initial pointer and drag vector.
- Use `AspectFit` in the demo so portrait source pages retain their aspect ratio.
- Validate with the user-local SDK at `/Users/marius/.dotnet/dotnet` and prefer iOS simulator testing.

### Task 1: Add edge-aware core contracts and failing tests

**Files:**
- Modify: `src/PageCurl.Core/PageTurnTypes.cs`
- Modify: `src/PageCurl.Core/PageTurnController.cs`
- Modify: `src/PageCurl.Core/PageCurlGeometry.cs`
- Modify: `tests/PageCurl.Core.Tests/PageTurnControllerTests.cs`
- Modify: `tests/PageCurl.Core.Tests/PageCurlGeometryTests.cs`

**Interfaces:**

```csharp
public enum PageEdge
{
    Auto,
    Top,
    Right,
    Bottom,
    Left
}

public bool BeginDrag(Vector2 pointer, Vector2 pageSize, PageEdge lockedEdge = PageEdge.Auto);
```

Add the selected hinge edge to `PageTurnSnapshot` and `CurlPose`. Keep existing constructors and overloads source-compatible where practical.

- [x] Add tests for all edge centers and all corners.
- [x] Add tests for Auto corner tie-breaking from the first movement vector.
- [x] Add tests for every locked edge and every free-edge start.
- [x] Add tests for locked-edge rejection or reassignment using one explicit documented rule.
- [x] Add tests for finite/bounded meshes for all edges.
- [x] Run `/Users/marius/.dotnet/dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj -c Release` and verify the new tests fail because the edge contract is not implemented.

### Task 2: Implement edge-aware controller behavior

**Files:**
- Modify: `src/PageCurl.Core/PageTurnController.cs`
- Modify: `src/PageCurl.Core/PageTurnTypes.cs`
- Test: `tests/PageCurl.Core.Tests/PageTurnControllerTests.cs`

- [x] Store the selected hinge edge for the active gesture.
- [x] Select the nearest edge in Auto mode, using the initial drag vector to resolve corner ties.
- [x] Accept every non-locked edge and corner.
- [x] Project pointer movement onto the selected hinge normal to calculate progress.
- [x] Preserve existing left/right behavior for callers using the old overload.
- [x] Run the focused core suite and verify all edge tests pass.
- [x] Commit `feat(core): support edge-aware page turn state` and push the current branch.

### Task 3: Generalize page curl geometry

**Files:**
- Modify: `src/PageCurl.Core/PageCurlGeometry.cs`
- Modify: `src/PageCurl.Core/PageTurnController.cs`
- Test: `tests/PageCurl.Core.Tests/PageCurlGeometryTests.cs`
- Test: `tests/PageCurl.Core.Tests/PageTurnPerformanceTests.cs`

- [x] Convert each hinge edge to a local horizontal frame.
- [x] Build the cylinder mesh in that frame and transform positions back to canvas coordinates.
- [x] Preserve UV ranges, shading, depth sorting, flat-page fallback, and buffer reuse.
- [x] Verify corner drags do not produce NaN, infinity, or unbounded vertices.
- [x] Extend the warmed allocation guard to all four hinge edges.
- [x] Run the full core suite and inspect allocation output.
- [x] Commit `feat(core): generalize curl geometry to every edge` and push.

### Task 4: Add aspect-preserving renderer support

**Files:**
- Modify: `src/PageCurl.Maui/PageTurnView.cs`
- Modify: `src/PageCurl.Maui/PageTurnPerformanceSnapshot.cs` only if diagnostics need a content-size field.
- Modify: `tests/PageCurl.Core.Tests` only if a platform-free viewport helper is needed.

Add a public `PageContentScale` option with `Fill` and `AspectFit`. Use a reusable content rectangle. Pass the same content rectangle to the controller and geometry so the mesh and texture align.

- [x] Add `AspectFit` behavior without allocating in the paint loop.
- [x] Keep caller-owned `SKImage` lifetime unchanged.
- [x] Keep the placeholder and idle states aligned with the same content rectangle.
- [x] Build the neutral MAUI target with zero warnings and errors.
- [x] Commit `feat(maui): preserve page content aspect ratio` and push.

### Task 5: Redesign the demo gesture surface

**Files:**
- Modify: `src/PageCurl.Demo/MainPage.xaml`
- Modify: `src/PageCurl.Demo/MainPage.xaml.cs`
- Modify: `src/PageCurl.Demo/App.xaml`
- Modify: `src/PageCurl.Demo/DemoPageSet.cs` if page art needs aspect-aware composition.

- [x] Add top, right, bottom, and left edge rails plus four corner handles.
- [x] Keep the overlay input transparent.
- [x] Fade hints after the first successful gesture and provide a way to show them again.
- [x] Add an Auto/Top/Right/Bottom/Left edge-lock selector.
- [x] Highlight the selected locked edge on the stage.
- [x] Replace the decorative top-left `↗` mark with a non-actionable brand mark or a labeled action.
- [x] Preserve the current theme tokens and visual language.
- [x] Use `AspectFit` for the demo page.
- [x] Commit `feat(demo): add edge hints and lock control` and push.

### Task 6: Make Testing mode responsive and accessible

**Files:**
- Modify: `src/PageCurl.Demo/MainPage.xaml`
- Modify: `src/PageCurl.Demo/MainPage.xaml.cs`
- Modify: `tools/ios-smoke.sh`

- [x] Keep the stage, edge-lock control, and Previous/Next buttons inside the safe area when diagnostics are visible.
- [x] Use a compact diagnostics summary and an independently scrollable or expandable details view.
- [x] Keep diagnostics timers and frame measurements disabled when Testing mode is off.
- [x] Add an accessible description to the interactive page surface.
- [x] Remove decorative rails/arrows from the accessibility tree.
- [x] Ensure accessibility labels include current page, status, selected edge, lock state, and commands.
- [x] Add smoke assertions for testing-mode visibility and button frames.
- [x] Capture default light/dark, Testing mode, and landscape screenshots.
- [ ] Capture active corner, top-edge, and bottom-edge curl screenshots through a reliable gesture harness; the current simulator swipe injection does not preserve Skia move events consistently enough to be evidence.
- [x] Commit `fix(demo): make diagnostics responsive and accessible` and push.

### Task 7: Final verification and documentation

**Files:**
- Modify: `README.md`
- Modify: `knowledge/project/testing.md`
- Modify: `knowledge/project/release-readiness.md`
- Modify: `docs/validation/ios-release-readiness.md`

- [x] Run the full core test runner.
- [x] Run the neutral MAUI Release build.
- [x] Run the Android Release build and the iOS simulator smoke with the user-local SDK.
- [x] Run the MacCatalyst Debug build and responsive-window visual check.
- [x] Run `git diff --check`.
- [x] Review the accessibility tree on the iOS simulator.
- [ ] Manually test each edge, corner, lock mode, cancellation, rotation, theme, large page, and Testing mode state on a real interactive surface; simulator swipe injection is not a reliable substitute for this gesture review.
- [x] Update docs with the physical edge model, lock semantics, UI controls, and remaining physical-device performance gates.
- [x] Commit `docs: document edge-aware page turning and validation` and push.

## Final acceptance criteria

- Every edge center and every corner starts a valid curl.
- Auto mode resolves corner ambiguity from the initial drag direction.
- A selected locked edge remains stationary while every other edge/corner can initiate a turn.
- Forward/backward navigation and existing public APIs continue to work.
- The page preserves its source aspect ratio in the demo.
- Testing mode never clips the stage or hides its commands on supported iOS simulator sizes.
- Accessibility exposes the page interaction, lock control, reduced-motion switch, diagnostics switch, and navigation commands without exposing decorative glyph noise.
- Core tests pass, warmed core paths retain the allocation guard, the neutral MAUI build passes with zero warnings/errors, and iOS smoke validation produces the required evidence.

The remaining unchecked items are deliberately device/manual evidence gates, not
unimplemented core behavior. The release-readiness record keeps physical-device
profiling, rotation, large-page review, and reliable active-gesture captures
visible until they can be exercised on a real touch surface or a gesture harness
that preserves Skia pointer-move events.
