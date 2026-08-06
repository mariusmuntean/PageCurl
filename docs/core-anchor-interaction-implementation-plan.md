# Core anchors and interaction implementation plan

**Goal:** Add pinned corners, configurable thresholds, threshold transitions,
flick configuration, and aspect-ratio layout primitives without weakening the
allocation-free core.

**Architecture:** `PageAnchor` extends the hinge model while `PageEdge` remains
compatible. Edge anchors keep the current cylinder; corner anchors use an
analytic developable cone centered on the pinned point. Controller events
describe threshold semantics without platform dependencies.

**Tech stack:** .NET 10, `System.Numerics`, dependency-free executable tests.

## Global constraints

- Keep `PageCurl.Core` free of MAUI, SkiaSharp, dispatchers, and device APIs.
- Preserve direct pointer tracking and fixed mesh storage.
- Add a deterministic regression before each behavior change.
- Keep existing `PageEdge`, `LockedEdge`, and `SnapThreshold` callers working.
- Do not add cloth state, collision, gravity, tearing, or per-frame collections.

---

### Task 1: Define anchors, configurable intent, and ratio layout

**Files:**
- Modify: `src/PageCurl.Core/PageTurnTypes.cs`
- Create: `src/PageCurl.Core/PageAspectLayout.cs`
- Create: `tests/PageCurl.Core.Tests/PageAnchorTests.cs`
- Create: `tests/PageCurl.Core.Tests/PageAspectLayoutTests.cs`
- Modify: `tests/PageCurl.Core.Tests/Program.cs`

**Interfaces:**
- Produces: `PageAnchor`, `PageAnchorExtensions`,
  `PageChangeThresholdCrossedEventArgs`, and `PageAspectLayout.Fit`.

- [ ] Add failing tests that require four corner anchors, edge conversion,
  opposite-edge conversion, invalid threshold rejection, and aspect-fit
  rectangles for explicit, automatic, and fallback ratios.
- [ ] Run the core runner and confirm compilation fails because the types are
  absent.
- [ ] Add:

```csharp
public enum PageAnchor
{
    Auto, Top, Right, Bottom, Left,
    TopLeft, TopRight, BottomRight, BottomLeft,
}

public sealed class PageChangeThresholdCrossedEventArgs : EventArgs
{
    public PageChangeThresholdCrossedEventArgs(
        PageTurnDirection direction,
        bool isCommitCandidate,
        float progress) { /* assign validated values */ }

    public PageTurnDirection Direction { get; }
    public bool IsCommitCandidate { get; }
    public float Progress { get; }
}
```

- [ ] Give `PageCurlOptions` one backing field shared by
  `PageChangeThreshold` and compatibility property `SnapThreshold`. Add
  `FlickEnabled` and `FlickVelocityThreshold`, defaulting to `true` and `1.5f`.
  Validate both thresholds as finite, with page threshold in `[0,1]` and flick
  threshold positive.
- [ ] Implement `PageAnchorExtensions.FromEdge`, `TryGetEdge`, `IsCorner`, and
  `GetPosition(Vector2 pageSize)` without allocations.
- [ ] Implement:

```csharp
public static PageViewportRect Fit(
    Vector2 viewport,
    float pageAspectRatio,
    Vector2 intrinsicSize,
    float fallbackAspectRatio = 0.75f)
```

  A zero page ratio selects the finite positive intrinsic ratio, then the
  fallback. Return `PageViewportRect.AspectFit` using a synthetic content size
  `(ratio, 1)`.
- [ ] Register both test groups, run the full core runner, and commit with
  `feat: define page anchors and layout options`.

### Task 2: Make controller intent configurable and observable

**Files:**
- Modify: `src/PageCurl.Core/PageTurnController.cs`
- Modify: `src/PageCurl.Core/PageTurnTypes.cs`
- Modify: `tests/PageCurl.Core.Tests/PageTurnControllerTests.cs`

**Interfaces:**
- Consumes: `PageAnchor`, `PageCurlOptions.PageChangeThreshold`,
  `FlickEnabled`, and `FlickVelocityThreshold`.
- Produces: `BeginDrag(Vector2, Vector2, PageAnchor)`, `LockedAnchor` on
  snapshots/poses, and `PageChangeThresholdCrossed`.

- [ ] Add failing tests for each pinned-corner rejection, valid starts away
  from the pin, custom thresholds at `0`, `0.35`, and `1`, flick-disabled
  release, custom flick velocity, one threshold-entry event, one threshold-exit
  event, and no repeated event on the same side.
- [ ] Run the core runner and confirm the new assertions fail against the fixed
  threshold/edge-only controller.
- [ ] Add `PageChangeThresholdCrossed` and track the last commit-candidate
  state. Reset it on every begin/programmatic/cancel path. Raise only when
  `progress >= PageChangeThreshold` changes during `UpdateDrag`.
- [ ] Replace the release decision with:

```csharp
var flickTarget = _options.FlickEnabled
    ? GetFlickTarget(_smoothedProgressVelocity, _options.FlickVelocityThreshold)
    : null;
_targetProgress = flickTarget ??
    (_progress >= _options.PageChangeThreshold ? 1f : 0f);
```

- [ ] Add the anchor overload. Keep the edge overload as a conversion facade.
  Reject the exact pinned corner with the same epsilon used for edges. For a
  corner anchor, keep the nearest valid free edge for progress calculation and
  store the corner separately in the snapshot and pose.
- [ ] Run all core tests and commit with
  `feat: configure page intent and threshold events`.

### Task 3: Add the developable pinned-corner cone

**Files:**
- Modify: `src/PageCurl.Core/PageCurlGeometry.cs`
- Modify: `src/PageCurl.Core/PageTurnTypes.cs`
- Modify: `tests/PageCurl.Core.Tests/PageCurlGeometryTests.cs`
- Modify: `tests/PageCurl.Core.Tests/PageTurnPerformanceTests.cs`

**Interfaces:**
- Consumes: `CurlPose.LockedAnchor` and existing grid buffers.
- Produces: analytic cone vertices for corner anchors and the unchanged cylinder
  path for edge anchors.

- [ ] Add failing tests for all four pinned corners. Each test asserts the pin
  is stationary, the grabbed material point is within `1.5f` of the pointer,
  structural and diagonal 3-D lengths stay within the production tessellation
  tolerance, vertices remain finite/bounded, and repeated builds are
  deterministic.
- [ ] Add a warmed allocation case for every corner anchor and verify it fails
  only because cone behavior is absent.
- [ ] Split `Build` into `BuildCylinderGrid` and `BuildCornerConeGrid`, both
  writing the same reusable `GridVertex` array before shared clipping/sorting.
- [ ] For a pinned corner `apex`, grabbed source vector `g`, and pointer vector
  `p`, compute `sinBeta = clamp(|p| / |g|, 0.08, 1)`. For each source vector
  `s`, use slant distance `rho = |s|`, signed angle
  `phi = atan2(cross(g,s), dot(g,s))`, cone azimuth
  `alpha = phi / sinBeta`, and screen direction obtained by rotating
  `normalize(p)` by `alpha`. Write:

```csharp
position = apex + (rho * sinBeta * screenDirection);
depth = rho * MathF.Sqrt(MathF.Max(0, 1 - (sinBeta * sinBeta)));
```

  Apply the existing endpoint relaxation so progress zero is flat. Use the
  pose direction to keep orientation continuous when `p` is nearly zero.
- [ ] Classify cone triangles from their 3-D normal and retain the exact
  cylinder tangent clipping only on the cylinder path. Sort both paths with the
  existing fixed `Orders` array.
- [ ] Run the full core suite and warmed allocation guard. Commit with
  `feat: curl pages around pinned corners`.

### Task 4: Update current core documentation

**Files:**
- Modify: `README.md`
- Modify: `docs/interaction-and-rendering.md`
- Modify: `docs/testing.md`
- Modify: `docs/requirements-status.md`

- [ ] Document `PageAnchor`, the cone/cylinder split, threshold units, flick
  settings, and threshold events with one short C# example.
- [ ] Mark core anchor, ratio, threshold, and flick requirements implemented.
- [ ] Run the core runner, Markdown link check, and `git diff --check`.
- [ ] Commit with `docs: document anchor and intent controls`.
