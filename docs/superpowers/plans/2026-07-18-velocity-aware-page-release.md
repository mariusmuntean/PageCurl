# Velocity-Aware Page Release Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make page turns stay attached to the finger during dragging and make release snap-back/snap-forward preserve a bounded flick velocity.

**Architecture:** Keep the existing direct edge-distance mapping and cylinder mesh unchanged. `PageTurnController` will derive a smoothed normalized progress velocity from monotonic drag timestamps, use it for a bounded snap decision near the threshold, and seed the existing critically damped spring with velocity aligned to the selected endpoint. `PageTurnView` will supply monotonic timestamps without adding a public API or moving motion logic into MAUI.

**Tech Stack:** .NET 10, C# `System.Numerics`, dependency-free executable core tests, .NET MAUI `SKGLView` adapter.

## Global Constraints

- Keep geometry and interaction code free of MAUI, SkiaSharp, dispatcher, and device dependencies.
- Preserve direct finger-to-progress tracking and existing reduced-motion/programmatic completion behavior.
- Do not change the mesh, shaders, demo layout, or public API in this pass.
- Keep all values finite, normalized, bounded, and allocation-free in warmed interaction loops.
- Preserve unrelated dirty worktree changes and stage only files belonging to this feature if staging is needed.
- Validate with `dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj -c Release` and `dotnet build src/PageCurl.Maui/PageCurl.Maui.csproj -c Release -f net10.0 -p:TargetFrameworks=net10.0`.

---

### Task 1: Add failing controller tests for velocity-aware release

**Files:**
- Modify: `tests/PageCurl.Core.Tests/PageTurnControllerTests.cs`

**Interfaces:**
- Consumes: the existing `PageTurnController.BeginDrag`, `UpdateDrag`, `EndDrag`, `Tick`, and `Snapshot` APIs.
- Produces: deterministic regression coverage that defines the release behavior before production code changes.

- [ ] **Step 1: Add test registrations and focused test cases**

Add these calls to `RunAll()`:

```csharp
Flick_release_commits_below_the_threshold();
Flick_release_cancels_above_the_threshold();
Non_monotonic_timestamps_do_not_create_release_velocity();
Timestamped_drag_remains_directly_attached_to_the_finger();
```

The tests should use `PageSize = new Vector2(100, 200)` and the forward edge:

```csharp
private static void Flick_release_commits_below_the_threshold()
{
    var controller = new PageTurnController();
    controller.BeginDrag(new Vector2(90, 100), PageSize);
    controller.UpdateDrag(new Vector2(70, 100), 1.000);
    controller.UpdateDrag(new Vector2(58, 100), 1.016);
    controller.EndDrag();

    TestAssert.True(controller.Snapshot.Progress < 0.5f);
    TestAssert.Equal(1f, controller.Snapshot.TargetProgress);
}

private static void Flick_release_cancels_above_the_threshold()
{
    var controller = new PageTurnController();
    controller.BeginDrag(new Vector2(90, 100), PageSize);
    controller.UpdateDrag(new Vector2(42, 100), 1.000);
    controller.UpdateDrag(new Vector2(50, 100), 1.016);
    controller.EndDrag();

    TestAssert.True(controller.Snapshot.Progress > 0.5f);
    TestAssert.Equal(0f, controller.Snapshot.TargetProgress);
}

private static void Non_monotonic_timestamps_do_not_create_release_velocity()
{
    var controller = new PageTurnController();
    controller.BeginDrag(new Vector2(90, 100), PageSize);
    controller.UpdateDrag(new Vector2(60, 100), 1.000);
    controller.UpdateDrag(new Vector2(30, 100), 0.500);
    controller.EndDrag();

    TestAssert.Equal(1f, controller.Snapshot.TargetProgress);
}

private static void Timestamped_drag_remains_directly_attached_to_the_finger()
{
    var controller = new PageTurnController();
    controller.BeginDrag(new Vector2(90, 100), PageSize);
    controller.UpdateDrag(new Vector2(63, 100), 1.000);

    TestAssert.Equal(new Vector2(63, 100), controller.Snapshot.PointerPosition);
    TestAssert.Equal(0.37f, controller.Snapshot.Progress, 3);
}
```

Use a short timestamp interval and enough movement that the release velocity is unambiguously above the implementation threshold. The non-monotonic case must prove an invalid timestamp cannot turn a slow, above-threshold release into a cancellation or a non-finite state.

- [ ] **Step 2: Run the core test runner and verify the new tests fail for the missing behavior**

Run:

```bash
dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj -c Release
```

Expected: the existing test groups still run, but at least one of the new flick assertions fails because `UpdateDrag` currently ignores `timestampSeconds`, resets release velocity, and applies only the fixed snap threshold.

---

### Task 2: Implement bounded progress velocity and endpoint-aligned settling

**Files:**
- Modify: `src/PageCurl.Core/PageTurnController.cs`

**Interfaces:**
- Consumes: timestamped `UpdateDrag` calls from core callers and the existing `PageCurlOptions.SpringFrequency` spring.
- Produces: the unchanged public controller API with velocity-aware release behavior.

- [ ] **Step 1: Add controller state and constants**

Add private state for the last accepted drag timestamp, last sampled progress, smoothed normalized progress velocity, and whether a timestamp sample exists. Use private constants for a release flick threshold, velocity smoothing factor, and maximum normalized release velocity. Keep these values finite and bounded; do not add new public options.

- [ ] **Step 2: Reset velocity state at every new interaction**

Reset the timestamp/sample state in both `BeginDrag` overloads and `BeginProgrammaticTurn`. Keep programmatic turns starting with zero velocity. `Cancel` must explicitly settle with zero velocity so pointer cancellation cannot inject stale gesture momentum.

- [ ] **Step 3: Update progress directly while deriving velocity from valid timestamps**

Keep the current edge-based `CalculateProgress` result as the displayed progress. After calculating it, accept a timestamp only when it is finite, monotonic, and separated from the last accepted sample by a positive, bounded interval. Compute normalized progress velocity from progress delta over that interval, smooth it without allocations, and clamp it to the private maximum. Ignore invalid/non-monotonic timestamps for velocity while still updating pointer position and direct progress.

- [ ] **Step 4: Select the release endpoint from threshold plus flick direction**

Retain the current `SnapThreshold` decision for slow releases. If the smoothed velocity exceeds the positive flick threshold, commit; if it is below the negative flick threshold, cancel. This allows quick intentional reversals to override the midpoint without changing slow-release semantics.

- [ ] **Step 5: Seed the existing spring without allowing an initial movement away from its endpoint**

Before `BeginSettling`, copy the bounded progress velocity into the spring velocity only when it points toward the selected target; otherwise use zero. Leave the existing critically damped `Tick` integration and completion criteria intact. This preserves bounded, allocation-free settling and avoids an unnatural first-frame reversal.

- [ ] **Step 6: Run the focused and full core tests**

Run:

```bash
dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj -c Release
```

Expected: all test groups pass, including the new flick, timestamp, and direct-tracking regressions.

---

### Task 3: Pass monotonic touch timestamps through the MAUI adapter

**Files:**
- Modify: `src/PageCurl.Maui/PageTurnView.cs`

**Interfaces:**
- Consumes: `System.Diagnostics.Stopwatch`, already imported by `PageTurnView` for render diagnostics.
- Produces: `Controller.UpdateDrag` calls carrying the monotonic elapsed-seconds value for each owned move event.

- [ ] **Step 1: Add a timestamp helper or inline monotonic conversion**

Use `Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency` at the move-event boundary. Do not use wall-clock time or allocate a `DateTime`/`TimeSpan` per move.

- [ ] **Step 2: Pass the timestamp only for owned pointer moves**

Change the owned `SKTouchAction.Moved` path to call:

```csharp
Controller.UpdateDrag(
    ToPagePointer(e.Location),
    Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency);
```

Leave pointer capture, release, cancellation, reduced motion, and invalidation behavior unchanged.

- [ ] **Step 3: Run the neutral MAUI build**

Run:

```bash
dotnet build src/PageCurl.Maui/PageCurl.Maui.csproj -c Release -f net10.0 -p:TargetFrameworks=net10.0
```

Expected: exit code 0 with the adapter compiling against the unchanged core API.

---

### Task 4: Record the new validation contract

**Files:**
- Modify: `knowledge/project/testing.md`

**Interfaces:**
- Consumes: the deterministic behavior established by Tasks 1–3.
- Produces: project knowledge that identifies release velocity and timestamp edge cases as part of the core regression surface.

- [ ] **Step 1: Extend the current automated coverage list**

Add a concise bullet stating that tests cover direct finger progress tracking, monotonic timestamp velocity, fast commit/cancel flicks around the threshold, and invalid timestamp rejection without non-finite motion.

- [ ] **Step 2: Review the scoped diff and rerun both validation commands**

Run:

```bash
git diff -- src/PageCurl.Core/PageTurnController.cs src/PageCurl.Maui/PageTurnView.cs tests/PageCurl.Core.Tests/PageTurnControllerTests.cs knowledge/project/testing.md
dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj -c Release
dotnet build src/PageCurl.Maui/PageCurl.Maui.csproj -c Release -f net10.0 -p:TargetFrameworks=net10.0
```

Expected: only the four feature files plus this plan are changed by this work, all core groups pass, and the neutral MAUI build exits successfully. Do not stage or revert the unrelated existing worktree changes.
