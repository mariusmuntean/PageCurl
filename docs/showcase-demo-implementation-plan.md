# Responsive showcase demo implementation plan

**Goal:** Replace the single demo surface with the approved Showcase shell and
five complete feature scenes for desktop and mobile.

**Architecture:** One page owns scene selection and shared navigation state.
Desktop and mobile compositions reuse the same controls and handlers; width
changes move them between a scene rail, bottom strip, side inspector, and
bottom-sheet options.

**Tech stack:** .NET 10 MAUI XAML/C#, SkiaSharp-generated page art, MAUI
haptics, optional sample-only video control with a poster fallback.

## Global constraints

- Warm canvas, ink text, one cobalt accent, no gradients or decorative badges.
- Every visible button must perform a real action and expose correct enabled
  state.
- Keep diagnostics opt-in and off the primary surface.
- Use safe areas and minimum 44-DIP touch targets.
- Haptics run only on supported mobile targets and only on semantic transitions.

---

### Task 1: Create scene state and responsive shell

**Files:**
- Create: `src/PageCurl.Demo/ShowcaseScene.cs`
- Modify: `src/PageCurl.Demo/App.xaml`
- Rewrite: `src/PageCurl.Demo/MainPage.xaml`
- Modify: `src/PageCurl.Demo/MainPage.xaml.cs`

**Interfaces:**
- Produces five scenes: `Single`, `Spread`, `Content`, `Backside`, `CornerPin`.

- [ ] Add a neutral scene-state test if logic exceeds simple enum selection.
- [ ] Replace the root with header, desktop rail, star-sized stage, footer,
  mobile scene strip, side inspector, and mobile bottom sheet. Use width
  `720` DIPs as the composition breakpoint.
- [ ] Keep one selected-scene state. Move existing controls between desktop and
  mobile containers without duplicating handlers or settings.
- [ ] Add scene buttons with selected semantics and real stage switching. Add
  Previous/Next commands bound to the active scene.
- [ ] Build Mac Catalyst and inspect at 430, 720, 1024, and 1440 DIP widths.
  Commit with `feat: add responsive showcase shell`.

### Task 2: Replace generated art and implement single/backside/corner scenes

**Files:**
- Rewrite: `src/PageCurl.Demo/DemoPageSet.cs`
- Modify: `src/PageCurl.Demo/MainPage.xaml`
- Modify: `src/PageCurl.Demo/MainPage.xaml.cs`

- [ ] Generate warm-paper pages with ink typography, cobalt registration
  marks, thin rules, varied aspect ratios, and concise capability copy. Keep a
  high-resolution page for texture validation.
- [ ] Single scene: anchor picker with all edges/corners, threshold slider shown
  as a percentage, flick toggle, ratio presets, and forward/backward paging.
- [ ] Backside scene: segmented choices for mirrored/dim, solid color, and
  custom content. Every selection updates the active control immediately.
- [ ] Corner scene: four corner-pin buttons, clear pinned-corner marker, and a
  short drag instruction. Disable the selected pin as a start point.
- [ ] Run core and Mac Catalyst builds, inspect every button, and commit with
  `feat: demonstrate page configuration`.

### Task 3: Implement spread and custom-content scenes

**Files:**
- Modify: `src/PageCurl.Demo/MainPage.xaml`
- Modify: `src/PageCurl.Demo/MainPage.xaml.cs`
- Create: `src/PageCurl.Demo/Views/InteractivePageView.cs`
- Create: `src/PageCurl.Demo/Views/VideoPosterPageView.cs`

- [ ] Spread scene: load two adjacent pages, soft gutter shadow, correct
  right-forward/left-backward commands, and disabled boundary buttons.
- [ ] Content scene: cycle among generated image, interactive `ContentView`
  with a working counter/button, and a video/poster page. The poster view uses
  an idle animation or platform video control when available and always exposes
  a capturable fallback.
- [ ] Add a refresh-snapshot action that works only in the content scene and
  reports capture errors without breaking navigation.
- [ ] Build Mac Catalyst, Android, and iOS explicit targets when available.
  Commit with `feat: demonstrate spreads and custom content`.

### Task 4: Add semantic mobile haptics

**Files:**
- Create: `src/PageCurl.Demo/PageTurnHaptics.cs`
- Modify: `src/PageCurl.Demo/MainPage.xaml.cs`
- Modify: `src/PageCurl.Demo/MainPage.xaml`

**Interfaces:**
- Consumes `PageChangeThresholdCrossed` and `PageTurnCompleted`.
- Produces distinct threshold and committed-completion feedback.

- [ ] Extract a testable event-to-pattern mapper with patterns `None`, `Click`,
  and `Strong`. Add core-neutral tests for threshold entry, exit, committed
  completion, cancelled completion, disabled state, and duplicate suppression.
- [ ] Map `Click` to `HapticFeedbackType.Click` and `Strong` to
  `HapticFeedbackType.LongPress`. Guard unsupported desktop targets and catch
  platform unavailability without surfacing an error.
- [ ] Add a Haptics switch in options. Default it on for Android/iOS and off on
  desktop. Do not vibrate for programmatic diagnostics setup.
- [ ] Build explicit mobile targets and commit with
  `feat: add page-turn haptic cues`.

### Task 5: Accessibility, lifecycle, and visual validation

**Files:**
- Modify: `tools/ios-smoke.sh`
- Modify: `docs/validation/ios-release-readiness.md`
- Modify: `docs/requirements-status.md`
- Modify: `docs/testing.md`

- [ ] Extend smoke assertions for scene navigation, spread controls, threshold
  percentage, anchor selection, content/backside scenes, options, and haptics
  setting. Keep decorative page marks outside the accessibility tree.
- [ ] Verify light/dark, portrait/landscape, compact/wide desktop, every scene,
  every button, edge/corner anchors, threshold reversal, fast flick, reduced
  motion, snapshot refresh/failure, repeated spread turns, and teardown.
- [ ] Run core tests, neutral MAUI build, Mac Catalyst build, Android build, iOS
  smoke when available, workflow-project check, Markdown links, and diff check.
- [ ] Record only completed evidence and keep physical-device profiling as an
  open release gate. Commit with `test: validate responsive feature showcase`.
