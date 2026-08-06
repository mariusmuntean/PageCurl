# Stiff Paper Solver Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace positional curl blending with a fixed-buffer, constraint-based sheet solver that supports arbitrary valid free-edge/locked-hinge combinations without stretching or sharp folds.

**Architecture:** Keep the controller and public mesh API. Store persistent 3-D particles and fixed constraint topology inside `PageMeshBuffer`; use the existing cylinder as a target pose, then project hard stretch/shear constraints, compliant bend constraints, hinge pins, and an interpolated pointer constraint. Derive triangle orientation and shading from the solved surface.

**Tech Stack:** .NET 10, `System.Numerics`, XPBD-style constraint projection, dependency-free executable tests.

## Global Constraints

- Keep `PageCurl.Core` free of MAUI, SkiaSharp, dispatchers, and device dependencies.
- Keep caller-owned image behavior and the public controller/view API compatible.
- Preserve every valid free-edge/locked-hinge pairing.
- Reject the locked edge and its exact endpoints as gesture starts.
- Allocate particles, constraints, multipliers, grid vertices, and triangle buffers once.
- Use zero compliance for stretch and shear; use low, scale-aware compliance for bending.
- Do not add tearing, plasticity, gravity, collision, a new renderer, or public low-level solver controls.
- Keep the page flat at inactive and completed endpoints.

---

### Task 1: Make locked-edge starts physically consistent

**Files:**
- Modify: `tests/PageCurl.Core.Tests/PageTurnControllerTests.cs`
- Modify: `src/PageCurl.Core/PageTurnController.cs`

**Interfaces:**
- Consumes: `PageTurnController.BeginDrag(Vector2 pointer, Vector2 pageSize, PageEdge lockedEdge)`.
- Produces: points on the locked edge return `false`; centers of the other three edges remain valid.

- [ ] **Step 1: Add controller regressions**

Register `Locked_edge_and_endpoints_do_not_start_a_drag()` and
`Unlocked_edges_still_start_a_drag()`. Iterate all four physical edges. For a
locked edge, test its center and two corners; each call must return `false` and
leave `Snapshot.State` at `Idle`. For each other edge center, assert `true` and
use a fresh controller for the next case.

- [ ] **Step 2: Run the core runner and verify RED**

```bash
dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj -c Release
```

Expected: a locked corner is accepted by the current corner exception.

- [ ] **Step 3: Implement the minimal guard**

In the edge-aware `BeginDrag`, replace the nearest-edge/corner condition with:

```csharp
if (lockedEdge != PageEdge.Auto && IsOnEdge(clampedPointer, pageSize, lockedEdge))
{
    return false;
}
```

Keep Auto selection and the two-argument overload unchanged.

- [ ] **Step 4: Run the core runner and verify GREEN**

Run the command from Step 2. Expected: all groups pass.

- [ ] **Step 5: Commit**

```bash
git add src/PageCurl.Core/PageTurnController.cs tests/PageCurl.Core.Tests/PageTurnControllerTests.cs
git commit -m "fix: keep locked page edges stationary"
```

---

### Task 2: Define inextensible paper behavior with failing tests

**Files:**
- Modify: `tests/PageCurl.Core.Tests/PageCurlGeometryTests.cs`

**Interfaces:**
- Consumes: public `FrontVertices`, `BackVertices`, texture coordinates, screen positions, and `Depth`.
- Produces: tests that reconstruct solved grid samples as `Vector3(position, depth)` and measure edge lengths, pointer attachment, and local bend angles.

- [ ] **Step 1: Add a mesh-sample helper**

Add `FindVertexAtTexture(Vector2 texture)` that scans both vertex streams and
returns the closest matching `MeshVertex`. Convert it with:

```csharp
private static Vector3 ToPosition3(MeshVertex vertex) =>
    new(vertex.Position, vertex.Depth);
```

Fail the test when the closest texture-coordinate distance exceeds `0.001f`.

- [ ] **Step 2: Add a failing stretch/shear regression**

Register `Solved_sheet_preserves_structural_and_diagonal_lengths()`. Use a
12-by-12 mesh, left hinge, top free edge, drag start `(75, 0)`, pointer
`(62, 64)`, and progress `0.4`. Build the same pose ten times to allow the
future iterative solver to converge.

For every horizontal, vertical, and cell diagonal neighbor pair, compare the
3-D solved distance to the flat-page rest distance. Assert relative error is at
most `0.005f`.

- [ ] **Step 3: Add a failing adjacent-edge pointer regression**

Register `Adjacent_edge_grab_stays_attached_to_pointer()`. Use the same pose and
assert that the mesh point at texture `(0.75, 0)` is within `1.5f` screen units
of the pointer.

- [ ] **Step 4: Add a stiff-bend regression**

Register `Paper_solver_does_not_form_a_single_cell_crease()`. Reconstruct cell
normals from neighboring grid samples and compare normals of adjacent cells.
Assert no local angle exceeds 50 degrees for the aggressive adjacent-edge pose.

- [ ] **Step 5: Run the core runner and verify RED**

Run the core command. Expected: current hinge blending violates at least the
rest-length and adjacent pointer assertions. Confirm failures are assertions,
not missing texture samples.

---

### Task 3: Add reusable paper particles and fixed constraint topology

**Files:**
- Modify: `src/PageCurl.Core/PageTurnTypes.cs`
- Test: `tests/PageCurl.Core.Tests/PageCurlGeometryTests.cs`

**Interfaces:**
- Consumes: `PageCurlOptions.MeshColumns`, `MeshRows`, and current page size.
- Produces: internal fixed arrays on `PageMeshBuffer`: `Vector3[] Positions`, `PreviousPositions`, `Targets`; `PaperConstraint[] Constraints`; `float[] ConstraintMultipliers`; and simulation identity fields.

- [ ] **Step 1: Define constraint topology types**

Add internal enum values `Stretch`, `Shear`, and `Bend`. Add an internal readonly
`PaperConstraint` struct containing vertex indices `A` and `B`, integer grid
deltas `DeltaColumn` and `DeltaRow`, and the constraint kind.

- [ ] **Step 2: Allocate simulation storage in `PageMeshBuffer`**

Allocate three position arrays with `Grid.Length` entries. Build one constraint
array containing:

- every horizontal and vertical neighbor as `Stretch`;
- both diagonals of every cell as `Shear`;
- every horizontal and vertical second neighbor as `Bend`.

Allocate one float multiplier per constraint. Store no delegates or per-frame
collections.

- [ ] **Step 3: Add simulation identity and reset state**

Store last page size, hinge edge, free edge, and a boolean initialized flag.
Add internal `ResetSimulation()` that clears the identity and multiplier span;
do not allocate or resize arrays.

- [ ] **Step 4: Run the core runner**

Expected: the project compiles and existing tests still pass except the intended
RED tests from Task 2.

- [ ] **Step 5: Commit storage and topology**

```bash
git add src/PageCurl.Core/PageTurnTypes.cs
git commit -m "refactor: add reusable paper solver storage"
```

---

### Task 4: Implement the fixed-buffer stiff-paper solver

**Files:**
- Modify: `src/PageCurl.Core/PageCurlGeometry.cs`
- Test: `tests/PageCurl.Core.Tests/PageCurlGeometryTests.cs`

**Interfaces:**
- Consumes: `CurlPose`, fixed particle/constraint arrays, and the existing cylinder target equations.
- Produces: converged 3-D particle positions with hard hinge, stretch, shear, and pointer constraints plus compliant bending.

- [ ] **Step 1: Extract analytic target mapping**

Create a private `MapCurlTarget` method that returns `Vector3`. Reuse the current
flat, curled, and turned cylinder regions, with screen X/Y in the first two
components and current depth in Z. Remove `HingeWeight`; the solver pins the
hinge instead of blending positions.

- [ ] **Step 2: Initialize or reset flat particles deterministically**

When page size, hinge, or free edge changes, fill `Positions`,
`PreviousPositions`, and `Targets` from the regular flat grid and clear all
multipliers. For inactive, non-finite, zero-progress, or completed poses, call
`WriteFlatPage` and reset simulation state.

- [ ] **Step 3: Predict toward the analytic target**

For an active pose, fill `Targets` with `MapCurlTarget`. Predict each non-hinge
particle from its previous solved position toward the target with a fixed,
bounded guide factor. Copy the current solved positions to `PreviousPositions`
before prediction.

- [ ] **Step 4: Implement XPBD distance projection**

For each constraint, compute the current distance, flat-page rest distance from
its grid delta, and inverse masses (`0` for hinge particles, `1` otherwise).
Use:

```csharp
var alpha = compliance / (TimeStep * TimeStep);
var deltaLambda = (-constraintValue - (alpha * lambda)) /
    (inverseMassA + inverseMassB + alpha);
```

Accumulate the multiplier and apply mass-weighted corrections along the
normalized edge direction. Use zero compliance for `Stretch` and `Shear`.
Derive a small scale-aware bend compliance from `RadiusScale` for `Bend`.

- [ ] **Step 5: Implement exact hinge pins**

Before and after each solver iteration, write every locked-edge particle to its
flat X/Y position with Z equal to zero. The helper must work for top, right,
bottom, and left hinges without allocating an index list.

- [ ] **Step 6: Implement the interpolated pointer constraint**

Project `pose.DragStart` onto the selected free-edge grid segment. Store the two
neighbor indices and interpolation weight. On every iteration, correct the
weighted X/Y point to `pose.Position`, distributing correction by squared
interpolation weights and inverse mass. Preserve the interpolated target Z so
the grabbed edge can rise while remaining under the pointer on screen.

- [ ] **Step 7: Solve with a fixed iteration budget**

Use ten iterations in this order: hinge pins, pointer constraint, distance
constraints, hinge pins, pointer constraint. Clear multipliers at the start of
each rendered frame; keep arrays fixed. After the last iteration, store solved
positions as the next frame's state.

- [ ] **Step 8: Write solved grid vertices**

Write screen position from X/Y and depth from Z. Keep texture coordinates from
the regular grid. Set shade temporarily to `1`; Task 5 derives it from normals.

- [ ] **Step 9: Run the core runner and tune only physical constants**

Run the core command. Adjust the internal guide factor and bend compliance only
until rest-length, pointer, bend, finite, bounded, and existing hinge tests pass.
Do not weaken the `0.5%` length tolerance or add public solver controls.

- [ ] **Step 10: Commit the solver**

```bash
git add src/PageCurl.Core/PageCurlGeometry.cs tests/PageCurl.Core.Tests/PageCurlGeometryTests.cs
git commit -m "feat: simulate a stiff constrained page"
```

---

### Task 5: Derive surface orientation, shading, and fast depth order

**Files:**
- Modify: `src/PageCurl.Core/PageCurlGeometry.cs`
- Modify: `src/PageCurl.Core/PageTurnTypes.cs`
- Test: `tests/PageCurl.Core.Tests/PageCurlGeometryTests.cs`

**Interfaces:**
- Consumes: solved `Vector3` particle grid.
- Produces: triangle front/back classification from normal Z, bounded vertex shade, and ascending in-place depth order.

- [ ] **Step 1: Add orientation regressions**

Extend the mid-curl test to assert both front and back streams exist and every
shade is in `[0.1, 1]`. Reconstruct representative triangle normals and assert
front entries face the viewer while back entries face away, allowing a small
epsilon for edge-on triangles.

- [ ] **Step 2: Run the core runner and verify RED where classification differs**

Expected: current depth-threshold classification or all-one shading fails the
new surface-orientation assertion.

- [ ] **Step 3: Compute bounded grid normals and shade**

For each grid point, use available left/right and up/down solved neighbors.
Compute `normal = Normalize(Cross(horizontal, vertical))`, falling back to
`Vector3.UnitZ` for a degenerate cross product. Derive a bounded diffuse shade
from a fixed normalized light direction without allocating arrays.

- [ ] **Step 4: Classify triangles by orientation**

In `AddTriangle`, compute the 3-D cross product from the solved particle
positions. Set `IsBack` when its Z component is negative. Keep average Z as the
depth sort key.

- [ ] **Step 5: Replace insertion sort**

Add one cached `IComparer<PageMeshBuffer.TriangleOrder>` instance and replace
the quadratic loop with:

```csharp
orders.AsSpan(0, count).Sort(TriangleDepthComparer.Instance);
```

- [ ] **Step 6: Run the core runner and verify GREEN**

Expected: all geometry, orientation, finite-value, and buffer-reuse tests pass.

- [ ] **Step 7: Commit surface rendering data**

```bash
git add src/PageCurl.Core/PageCurlGeometry.cs src/PageCurl.Core/PageTurnTypes.cs tests/PageCurl.Core.Tests/PageCurlGeometryTests.cs
git commit -m "perf: derive and sort the solved page surface"
```

---

### Task 6: Prove warmed allocation behavior and adapter compatibility

**Files:**
- Modify: `tests/PageCurl.Core.Tests/PageTurnPerformanceTests.cs`
- Review: `src/PageCurl.Core/`
- Review: `src/PageCurl.Maui/`

**Interfaces:**
- Consumes: completed solver and rendering data.
- Produces: zero-allocation warmed loops and warning-free adapter compilation.

- [ ] **Step 1: Expand the allocation scenarios**

Measure 1,000 repeated builds for opposite and adjacent hinge/free-edge cases on
all four orientations. Warm each case for 100 builds. Keep the exact
`allocated == 0` assertion.

- [ ] **Step 2: Run the core runner**

```bash
dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj -c Release
```

Expected: all groups pass and every measured solver loop allocates zero bytes.

- [ ] **Step 3: Run the neutral MAUI build**

```bash
dotnet build src/PageCurl.Maui/PageCurl.Maui.csproj -c Release -f net10.0 -p:TargetFrameworks=net10.0
```

Expected: 0 warnings and 0 errors.

- [ ] **Step 4: Review YAGNI and API constraints**

Confirm no solver type is public, no collection is created per frame, and no
material controls beyond existing `PageCurlOptions` were added. Confirm the
legacy positional hinge blend and insertion sort are gone.

- [ ] **Step 5: Commit performance coverage**

```bash
git add tests/PageCurl.Core.Tests/PageTurnPerformanceTests.cs
git commit -m "test: guard paper solver allocations"
```
