# MAUI content and spread implementation plan

**Goal:** Add single-page MAUI content snapshots, configurable backsides, and a
two-page book-spread control while preserving the existing image renderer.

**Architecture:** `PageCurlView` owns live MAUI content and cached `SKImage`
snapshots. `PageSpreadView` composes static page hosts with one full-spread curl
overlay and a gutter shadow. Platform capture code stays behind one interface.

**Tech stack:** .NET 10 MAUI, SkiaSharp, Android/iOS/Mac Catalyst/Windows native
view capture APIs.

## Global constraints

- `PageTurnView` remains usable directly with caller-owned `SKImage` values.
- Snapshot capture never runs in the pointer-down path.
- The wrapper owns and disposes only snapshots it creates.
- The neutral `net10.0` target must continue to compile.
- No video-player dependency enters the library package.

---

### Task 1: Add backside rendering to `PageTurnView`

**Files:**
- Create: `src/PageCurl.Maui/PageBacksideMode.cs`
- Modify: `src/PageCurl.Maui/PageTurnView.cs`
- Create: `tests/PageCurl.Core.Tests/PageBacksideMappingTests.cs`
- Modify: `tests/PageCurl.Core.Tests/Program.cs`

**Interfaces:**
- Produces: `PageBacksideMode`, `BacksideMode`, `BacksideColor`,
  `BacksideDimFactor`, and mirrored-back UV/color mapping.

- [ ] Add a platform-free mapping helper and failing tests for horizontally
  mirrored UVs, dim-factor clamping, solid color, and custom texture mode.
- [ ] Implement `PageBacksideMapping.Map` in Core or a neutral MAUI source file
  without Skia types; register the tests.
- [ ] Add bindable properties with defaults `MirroredFront`, `Colors.White`,
  and `0.82`. In `DrawMesh`, mirror back UV X for `MirroredFront`, multiply
  vertex shade, draw a color shader for `SolidColor`, and use `BackImage` for
  `CustomContent`.
- [ ] Run core tests and the neutral MAUI build. Commit with
  `feat: configure page backsides`.

### Task 2: Add platform content snapshot abstraction

**Files:**
- Create: `src/PageCurl.Maui/IPageContentSnapshotter.cs`
- Create: `src/PageCurl.Maui/PageContentSnapshotter.cs`
- Create: `src/PageCurl.Maui/Platforms/Android/PageContentSnapshotter.Android.cs`
- Create: `src/PageCurl.Maui/Platforms/Apple/PageContentSnapshotter.Apple.cs`
- Create: `src/PageCurl.Maui/Platforms/Windows/PageContentSnapshotter.Windows.cs`
- Modify: `src/PageCurl.Maui/PageCurl.Maui.csproj`

**Interfaces:**
- Produces:

```csharp
public interface IPageContentSnapshotter
{
    Task<SKImage> CaptureAsync(View content, CancellationToken cancellationToken);
}
```

- [ ] Add neutral contract tests through a fake snapshotter in the wrapper task;
  keep the neutral default implementation throwing `PlatformNotSupportedException`.
- [ ] On Android, call `View.ToPlatform`, draw the native view into an ARGB
  bitmap, encode it to PNG in memory, and create `SKImage.FromEncodedData`.
- [ ] On iOS/Mac Catalyst, render the native view hierarchy into a
  `UIGraphicsImageRenderer`, encode PNG data, and create the Skia image.
- [ ] On Windows, render the `UIElement` with `RenderTargetBitmap`, copy BGRA
  pixels into an `SKBitmap`, and create `SKImage` from that bitmap.
- [ ] Keep capture asynchronous and cancellable at lifecycle boundaries. Build
  neutral and every available explicit platform target. Commit with
  `feat: capture MAUI page content`.

### Task 3: Add `PageCurlView`

**Files:**
- Create: `src/PageCurl.Maui/PageCurlView.cs`
- Create: `src/PageCurl.Maui/PageSnapshotFailedEventArgs.cs`
- Create: `tests/PageCurl.Maui.Tests/PageCurlViewTests.cs` only if a MAUI test
  project can run neutrally; otherwise test extracted lifecycle state in Core.

**Interfaces:**
- Produces: `FrontContent`, `BackContent`, `PageAspectRatio`,
  `RefreshSnapshotAsync`, `SnapshotFailed`, and forwarded interaction events.

- [ ] Extract a neutral `PageSnapshotState` that owns replacement/cancellation
  decisions. Add failing tests for last-good-frame retention, content
  replacement cancellation, front/back independence, and disposal order.
- [ ] Build `PageCurlView` as a `Grid` with live content, a hidden
  `PageTurnView`, and edge/corner activation strips. The center remains free for
  child interaction. Once a strip starts a gesture, show the renderer until
  completion/cancellation.
- [ ] Forward anchor, threshold, flick, backside, reduced-motion, diagnostics,
  aspect ratio, and completion properties/events.
- [ ] Capture after `Loaded`, `SizeChanged`, and explicit refresh. Never await a
  capture from pointer-down. On failure keep the previous image and raise
  `SnapshotFailed`.
- [ ] Cancel captures, clear renderer image properties, then dispose owned
  snapshots on handler disconnect. Run neutral and Mac Catalyst builds. Commit
  with `feat: add snapshot-backed page content`.

### Task 4: Add `PageSpreadView`

**Files:**
- Create: `src/PageCurl.Maui/PageSpreadView.cs`
- Create: `src/PageCurl.Core/PageSpreadNavigationState.cs`
- Create: `tests/PageCurl.Core.Tests/PageSpreadNavigationStateTests.cs`
- Modify: `tests/PageCurl.Core.Tests/Program.cs`

**Interfaces:**
- Produces direction-aware spread navigation and MAUI properties for left,
  right, next-right, and previous-left content.

- [ ] Add failing navigation tests for initial spread, forward/right-sheet
  turn, backward/left-sheet turn, boundaries, and exactly-once notifications.
- [ ] Implement `PageSpreadNavigationState` with even/odd indices and one
  completion method shared by gestures and commands.
- [ ] Build `PageSpreadView` with two static `PageCurlView` hosts, one
  full-width `PageTurnView` overlay, and a centered shadow `BoxView`/gradient
  brush. The overlay uses half-page geometry with a left/right origin so a
  moving sheet can cross the gutter.
- [ ] Expose `TryTurnForward`, `TryTurnBackward`, `CanTurnForward`,
  `CanTurnBackward`, and one `PageTurnCompleted` event. Disable re-entry while
  the overlay settles.
- [ ] Run core, neutral MAUI, and Mac Catalyst builds. Commit with
  `feat: add two-page spread turns`.

### Task 5: Document content and spread contracts

**Files:**
- Modify: `README.md`
- Modify: `docs/architecture.md`
- Modify: `docs/runtime-contracts.md`
- Modify: `docs/testing.md`
- Modify: `docs/requirements-status.md`

- [ ] Add minimal XAML for `PageCurlView` and `PageSpreadView`, snapshot/video
  fallback rules, ownership, capture failure, and backside examples.
- [ ] Mark content, backside, and spread requirements implemented while keeping
  live video deformation a non-goal.
- [ ] Run link checks, neutral build, and `git diff --check`. Commit with
  `docs: document content and spread controls`.
