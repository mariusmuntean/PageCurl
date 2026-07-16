# PageCurl Demo Experience Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Reduce PageCurl to four projects, increase deterministic test confidence, and deliver a polished MAUI sample that visibly demonstrates side and corner page curls.

**Architecture:** Move the platform-free navigation state from the demo-only project into `PageCurl.Core` as `PageNavigationState`; the demo and test harness both reference Core directly. Keep the renderer unchanged at its public boundary, and compose the visual refresh entirely from theme-aware MAUI resources, XAML layout, generated Skia page art, and input-transparent affordances.

**Tech Stack:** .NET 10.0.301, .NET MAUI 10, C#, XAML, SkiaSharp 4.150.1, dependency-free executable tests, iOS simulator smoke script.

## Global Constraints

- Preserve the existing `global.json` SDK pin and the .NET 10 / MAUI 10 / SkiaSharp 4.150.1 compatibility line.
- Keep `TreatWarningsAsErrors` enabled for production projects.
- Keep `PageCurl.Core.Tests` dependency-free and runnable with `dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj`.
- Do not add third-party UI dependencies or make the affordance overlay capture touch input.
- Preserve caller-owned `SKImage` lifetime and the existing PageTurnView lifecycle behavior.
- Keep physical-device profiling and native Metal rendering explicitly outside this change.
- Preserve unrelated worktree changes; stage only files belonging to this implementation.

---

### Task 1: Replace the demo-only model project with reusable Core navigation state

**Files:**
- Create: `src/PageCurl.Core/PageNavigationState.cs`
- Delete: `src/PageCurl.Demo.Model/DemoNavigationState.cs`
- Delete: `src/PageCurl.Demo.Model/PageCurl.Demo.Model.csproj`
- Modify: `src/PageCurl.Demo/PageCurl.Demo.csproj`
- Modify: `tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj`
- Modify: `PageCurl.sln`
- Rename test: `tests/PageCurl.Core.Tests/DemoNavigationStateTests.cs` to `tests/PageCurl.Core.Tests/PageNavigationStateTests.cs`

**Interfaces:**
- Produces `PageCurl.Core.PageNavigationState(int pageCount)` with `PageIndex`, `PageCount`, `CanTurnForward`, `CanTurnBackward`, `TryCommit(PageTurnDirection)`, and `GetUnderPageIndex(PageTurnDirection)`.

- [x] **Step 1: Write the failing tests**

  Update the navigation tests to use `PageNavigationState`, assert `PageCount`, reject zero and negative counts, and verify successful commits raise exactly the three expected property names while rejected commits raise none.

- [x] **Step 2: Run the focused test group to verify it fails**

  Run `dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj`.

  Expected: compilation fails because `PageNavigationState` does not exist yet.

- [x] **Step 3: Implement the minimal Core type and project references**

  Move the existing state logic into `PageCurl.Core` under the neutral name `PageNavigationState`, expose `PageCount`, update the demo and test references, and remove the old project from all solution configuration and nesting sections.

- [x] **Step 4: Run the full test suite**

  Run `dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj`.

  Expected: every existing test group plus `PageNavigationStateTests` passes.

### Task 2: Add regression coverage for side and corner starts

**Files:**
- Modify: `tests/PageCurl.Core.Tests/PageTurnControllerTests.cs`
- Modify: `tests/PageCurl.Core.Tests/PageCurlGeometryTests.cs`

**Interfaces:**
- Consumes the existing `PageTurnController.BeginDrag`, `PageTurnController.GetCurlPose`, and `PageCurlGeometry.Build` contracts.

- [x] **Step 1: Add failing corner-origin assertions**

  Add tests that press at `(0, 0)`, `(0, height)`, `(width, 0)`, and `(width, height)`, assert the expected direction and clamped drag-start position, then sample the resulting pose through `PageCurlGeometry.Build` and assert all emitted vertices remain finite and within the existing bounds.

- [x] **Step 2: Run the focused suite**

  Run `dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj`.

  Expected: the new tests pass against the existing controller, confirming the visual affordance requirement is backed by the actual interaction contract.

- [x] **Step 3: Add exactly-once completion assertions**

  Count completion events for immediate and animated programmatic turns and assert each path raises one event, never two.

- [x] **Step 4: Run the suite again**

  Expected: all test groups pass with zero allocation-guard regressions.

### Task 3: Create the visual direction companion and update theme resources

**Files:**
- Create: `/Users/marius/.codex/visualizations/2026/07/16/019f6bfd-65bf-7bc3-b759-d754a94a0742/pagecurl-reader.html`
- Modify: `src/PageCurl.Demo/App.xaml`

**Interfaces:**
- Produces the visual language that the MAUI XAML mirrors: ink background, violet accent, warm page surface, compact status badge, stage rails, corner markers, and readable action controls.

- [x] **Step 1: Build the small interactive visual companion**

  Use semantic controls and one dominant reading stage. Allow Previous/Next and a reduced-motion toggle to change the displayed page and affordance state. Use theme variables and keyboard-accessible buttons.

- [x] **Step 2: Render and inspect the companion**

  Run `python3 /Users/marius/.codex/plugins/cache/openai-bundled/visualize/1.0.11/scripts/render.py /Users/marius/.codex/visualizations/2026/07/16/019f6bfd-65bf-7bc3-b759-d754a94a0742/pagecurl-reader.html /Users/marius/.codex/visualizations/2026/07/16/019f6bfd-65bf-7bc3-b759-d754a94a0742/pagecurl-reader-rendered.html` and inspect the rendered result.

- [x] **Step 3: Translate the direction into MAUI resources**

  Add theme-aware colors, brushes, typography styles, and control styles without adding packages or changing the control API.

### Task 4: Refresh the MAUI sample presentation

**Files:**
- Modify: `src/PageCurl.Demo/MainPage.xaml`
- Modify: `src/PageCurl.Demo/MainPage.xaml.cs`
- Modify: `src/PageCurl.Demo/DemoPageSet.cs`

**Interfaces:**
- Consumes `PageNavigationState` and the existing `PageTurnView` commands.
- Preserves `PageTurnView.PageTurnCompleted`, `TryTurn`, and image ownership semantics.

- [x] **Step 1: Add presentation-state tests where behavior is non-visual**

  Keep navigation behavior in Core; use the existing executable suite for all index and direction decisions rather than attempting brittle XAML snapshots.

- [x] **Step 2: Implement the polished XAML shell**

  Replace the plain four-row layout with a safe-area-aware header, page counter, stage, input-transparent left/right rails, four corner markers, labeled command buttons, and the reduced-motion setting. Keep all accessible descriptions explicit.

- [x] **Step 3: Improve deterministic sample page art**

  Add a page number, eyebrow, accent band, content rhythm, and corner/edge marks to the generated pages. Keep the large page definition and deterministic dimensions.

- [x] **Step 4: Update lifecycle and status copy**

  Instantiate `PageNavigationState`, use the new namespace, keep completion as the single commit point, and make status text identify side and corner drags without claiming unsupported behavior.

- [x] **Step 5: Build the demo targets**

  Run the neutral, Android Release, and iOS Release builds with warnings treated as errors.

### Task 5: Verify, review, stage, and commit

**Files:**
- Verify all modified source, project, solution, docs, and visual companion files.

- [x] **Step 1: Run the full dependency-free test suite**

  Run `dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj` and retain the output as fresh evidence.

- [x] **Step 2: Run the iOS-first smoke validation**

  Run `bash tools/ios-smoke.sh`, install/launch on the available iPhone simulator, exercise Previous/Next, reduced motion, rotation, and boundary states, and capture light/dark screenshots.

- [x] **Step 3: Review the diff and solution shape**

  Run `dotnet sln PageCurl.sln list`, `git diff --check`, `git status --short`, and inspect the final diff for unrelated changes, stale project references, clipped UI copy, and accidental ownership changes.

- [x] **Step 4: Stage only intended files**

  Stage the four-project simplification, tests, demo UI, visual companion, and plan/spec documents.

- [x] **Step 5: Create the final commit**

  Use the neutral commit message `Polish the demo and simplify the solution`.
