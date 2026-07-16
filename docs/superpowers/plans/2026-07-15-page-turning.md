# Page Turning Control Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a tested analytic page-curl core, a SkiaSharp MAUI control, and a MAUI demo app.

**Architecture:** A pure `PageCurl.Core` project owns the interaction state, cylinder pose and reusable mesh buffers. `PageCurl.Maui` adapts that core to an `SKGLView`, while `PageCurl.Demo` consumes the control. Tests target the core so they run without a mobile device.

**Tech Stack:** C#/.NET 8, .NET MAUI, SkiaSharp.Views.Maui.Controls, a dependency-free core test runner, System.Numerics.

## Global Constraints

- The core has no UI or rendering-package dependency.
- Geometry calculations must be deterministic and finite for valid page sizes and pointer positions.
- Mesh generation reuses caller-owned arrays after construction.
- The control renders only during a drag or settle and never decodes images per frame.
- Android and iOS are the minimum MAUI targets; desktop targets are included where MAUI supports them.

---

### Task 1: Establish the solution and failing core tests

**Files:**
- Create: `PageCurl.sln`
- Create: `src/PageCurl.Core/PageCurl.Core.csproj`
- Create: `tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj`
- Create: `tests/PageCurl.Core.Tests/PageTurnControllerTests.cs`
- Create: `tests/PageCurl.Core.Tests/PageCurlGeometryTests.cs`

**Interfaces:**
- Tests define the required `PageTurnController`, `PageTurnSnapshot`, `PageTurnState`, `PageTurnDirection`, `PageCurlGeometry`, `PageCurlOptions`, `CurlPose` and `PageMeshBuffer` contracts.

- [x] Write tests for direction/progress, settling, finite poses, mesh bounds/UVs, front/back output and buffer reuse.
- [x] Run the dependency-free test project and confirm the expected missing-type failures before implementation.

### Task 2: Implement the pure page-turn core

**Files:**
- Create: `src/PageCurl.Core/PageTurnTypes.cs`
- Create: `src/PageCurl.Core/PageTurnController.cs`
- Create: `src/PageCurl.Core/PageCurlGeometry.cs`

**Interfaces:**
- `PageTurnController` produces `PageTurnSnapshot` and `CurlPose`.
- `PageCurlGeometry.Build(PageMeshBuffer, Vector2, CurlPose, PageCurlOptions)` fills reusable buffers.

- [x] Implement public value types, options and state transitions.
- [x] Implement the analytic cylinder pose solver with finite-value guards and endpoint clamping.
- [x] Implement regular-grid transformation, front/back classification, depth ordering and normalized UV output.
- [x] Run the core tests and adjust only production code until they pass.

### Task 3: Add the SkiaSharp MAUI control

**Files:**
- Create: `src/PageCurl.Maui/PageCurl.Maui.csproj`
- Create: `src/PageCurl.Maui/PageTurnView.cs`

**Interfaces:**
- `PageTurnView` exposes image properties, `Options`, `Controller` and `PageTurnCompleted`.

- [x] Add MAUI/Skia references and mobile/desktop target frameworks.
- [x] Translate Skia touch events into controller calls.
- [x] Draw under-page and front/back mesh parts using reusable Skia arrays and image shaders.
- [x] Start a short dispatcher timer only while settling. Images remain caller-owned, so the view never disposes them.

### Task 4: Add the MAUI demo application

**Files:**
- Create: `src/PageCurl.Demo/PageCurl.Demo.csproj`
- Create: `src/PageCurl.Demo/App.xaml`
- Create: `src/PageCurl.Demo/App.xaml.cs`
- Create: `src/PageCurl.Demo/MainPage.xaml`
- Create: `src/PageCurl.Demo/MainPage.xaml.cs`
- Create: `src/PageCurl.Demo/MauiProgram.cs`

- [x] Configure MAUI handlers and SkiaSharp.
- [x] Put `PageTurnView` on the main page via XAML.
- [x] Generate small demo textures in memory and update the page label after a committed turn.

### Task 5: Verify and document limitations

**Files:**
- Modify: `README.md`
- Create: `Directory.Build.props`

- [x] Restore and run the core tests.
- [x] Build the core; the runner has no MAUI workload, so platform compilation is documented as a host prerequisite.
- [x] Run static checks for missing assets, target-framework mistakes and accidental per-frame managed array allocations.
- [x] Document how to run the demo on Android/iOS and the current renderer limitations.
