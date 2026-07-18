# Realistic Page Physics and Responsive Stage Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the visibly broken page-fold deformation with a continuous, physically plausible curl and make the demo stage consume the available window instead of stopping at a small fixed landscape rectangle.

**Architecture:** Keep the existing allocation-free controller and renderer boundary. Rework only the core page-to-cylinder/cone mapping so the page is nearly flat at the endpoints, tightest around the middle of the turn, and continuous across the mesh; keep the grabbed edge/corner and selected hinge anchored. Make the demo stage fill its `Grid.Row="2"` and let `PageTurnView` aspect-fit the page inside that full stage.

**Tech Stack:** .NET 10, C# `System.Numerics`, fixed PageCurl mesh buffers, .NET MAUI XAML/C# layout, SkiaSharp texture mapping.

## Global Constraints

- Preserve `PageCurl.Core` independence from MAUI, SkiaSharp, dispatchers, and devices.
- Keep caller-owned `SKImage` handling and the public `PageTurnView`/controller API compatible.
- Preserve direct pointer tracking, edge selection, hinge anchoring, reduced motion, and programmatic completion behavior.
- Do not introduce per-frame allocations, unbounded coordinates, or a full mass-spring simulation in the renderer.
- Preserve the checkpoint commit `1de46e6` as the rollback point; do not revert unrelated files.
- Validate core tests, neutral MAUI build, and an explicit Mac Catalyst or iOS visual path when the workload permits.

---

### Task 1: Add deterministic geometry regressions for physical continuity

**Files:**
- Modify: `tests/PageCurl.Core.Tests/PageCurlGeometryTests.cs`
- Modify: `tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj` only if a new test source is required

**Interfaces:**
- Consumes: `PageCurlGeometry.ComputePose`, `PageCurlGeometry.Build`, `PageMeshBuffer`, `CurlPose`, and `MeshVertex`.
- Produces: tests that fail against the fixed-radius/linear-hinge deformation and define endpoint flatness, smooth row progression, hinge stationarity, pointer anchoring, and finite bounds.

- [ ] **Step 1: Add endpoint and continuity assertions**

Cover a representative right-edge turn and a top-edge turn. Assert that an almost-zero-progress pose produces a flat page, an almost-complete pose returns the free-side geometry toward the flat endpoint, the selected hinge vertices remain stationary, and adjacent rows do not jump by a page-sized displacement.

- [ ] **Step 2: Add a coarse-mesh artifact regression**

Use the production mesh options and a top-edge or bottom-edge pose that previously produced staircase bands. Measure neighboring row displacement for corresponding columns and reject isolated jumps larger than the page diagonal fraction allowed by the continuous mapping.

- [ ] **Step 3: Run the core runner and verify the new tests fail before the geometry fix**

Run:

```bash
dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj -c Release
```

Expected: the new physical continuity assertion fails against the current fixed-radius, linearly weighted deformation while existing finite/bounded tests remain informative.

---

### Task 2: Replace the fixed-radius, linearly weighted fold mapping

**Files:**
- Modify: `src/PageCurl.Core/PageCurlGeometry.cs`
- Modify: `src/PageCurl.Core/PageTurnTypes.cs` only if mesh defaults or a validated geometry option must change
- Modify: `README.md` and the relevant `knowledge/project/` concept if the documented mesh/geometry trade-off changes

**Interfaces:**
- Consumes: the existing `CurlPose` with selected free/hinge edges and pointer anchor.
- Produces: the same reusable `PageMeshBuffer` vertex streams with continuous, finite, bounded page positions and depth/shading values.

- [ ] **Step 1: Map each source point into a page-local curl frame**

Use the selected free edge as the curl direction and the perpendicular page coordinate as the axis. Preserve the source coordinate along the axis rather than blending the entire page by a coarse linear hinge weight.

- [ ] **Step 2: Vary curl radius and sweep with normalized progress**

Use a bounded cylinder/cone-style mapping whose radius tends toward a large flat-page radius at progress 0 and 1, reaches its minimum around the middle, and maps arc length to the page distance without stretching the sheet. Clamp every radius, angle, and mapped coordinate before writing the reusable grid.

- [ ] **Step 3: Anchor the selected hinge and grabbed edge/corner in the same mapping**

Apply the existing pointer anchor as a translation/constraint after the continuous mapping, and verify the selected hinge edge remains exactly stationary. Avoid a separate linear skinning weight that can create visible row discontinuities.

- [ ] **Step 4: Keep front/back ordering and shading stable**

Classify and sort triangles using the mapped depth, retain fixed buffers, and ensure both front and back streams remain finite. Do not add a z-buffer dependency or per-frame arrays.

- [ ] **Step 5: Run the core runner and inspect the regression output**

Run:

```bash
dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj -c Release
```

Expected: all existing geometry/controller groups and the new continuity tests pass.

---

### Task 3: Make the demo stage fill the available window

**Files:**
- Modify: `src/PageCurl.Demo/MainPage.xaml`
- Modify: `src/PageCurl.Demo/MainPage.xaml.cs`

**Interfaces:**
- Consumes: the existing row-2 star-sized stage and `PageViewportRect.AspectFit` behavior.
- Produces: a stage that fills the available row/window while preserving page aspect ratio inside it.

- [ ] **Step 1: Remove hard landscape caps from the stage**

Make `StageGrid` fill horizontally and vertically inside the existing bordered row. Remove its `MaximumWidthRequest`, fixed landscape `WidthRequest`, and fixed landscape height calculation; keep only a safe minimum for very small windows if required by the platform.

- [ ] **Step 2: Keep the responsive diagnostics surface usable**

When Testing mode is enabled, reserve only the minimum extra space needed by the diagnostics panel; do not shrink the page back to the old 720×360 cap. Keep the edge hint overlay aligned with the filled `PageTurnView`.

- [ ] **Step 3: Run the explicit Mac Catalyst layout build**

Run:

```bash
dotnet build src/PageCurl.Demo/PageCurl.Demo.csproj -c Debug -f net10.0-maccatalyst
```

Expected: the demo compiles for the desktop target so the full-window layout can be inspected.

---

### Task 4: Validate visual behavior and record the result

**Files:**
- Modify: `knowledge/project/testing.md` or `docs/validation/ios-release-readiness.md` only when the new visual evidence changes the documented validation contract.

**Interfaces:**
- Consumes: the geometry and layout behavior from Tasks 2–3.
- Produces: a reproducible validation record for edge/corner curls, endpoint settling, large pages, and wide-window layout.

- [ ] **Step 1: Run platform-independent validation**

Run:

```bash
dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj -c Release
dotnet build src/PageCurl.Maui/PageCurl.Maui.csproj -c Release -f net10.0 -p:TargetFrameworks=net10.0
```

- [ ] **Step 2: Run the available visual validation path**

Use `dotnet build src/PageCurl.Demo/PageCurl.Demo.csproj -c Debug -f net10.0-maccatalyst` and inspect a wide landscape window, or use `./tools/ios-smoke.sh` when simulator evidence is the available path. Exercise top/right/bottom/left edges, all four corners, slow drag, reversal, snap-back, snap-forward, and the large-page demo case.

- [ ] **Step 3: Review the scoped diff and preserve rollback**

Run:

```bash
git diff --check
git status --short
git log -2 --oneline
```

Confirm the checkpoint `1de46e6` remains an ancestor and unrelated files are untouched before committing the new fix.
