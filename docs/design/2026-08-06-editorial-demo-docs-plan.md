# Editorial Demo and Documentation Consolidation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the busy sample interface with the approved editorial canvas and expose one concise, current `docs/` tree that explains the stiff-paper solver and easy public API.

**Architecture:** Keep demo state in the existing page code-behind and reusable core navigation state. Recompose XAML into header, reading stage, footer, and an overlay settings tray without adding dependencies. Merge durable repository guidance into canonical Markdown pages and remove historical duplicates.

**Tech Stack:** .NET 10 MAUI XAML/C#, SkiaSharp-generated sample images, Markdown.

## Global Constraints

- Preserve `PageCurl.Core`, `PageCurl.Maui`, and `PageCurl.Demo` boundaries.
- Use a warm editorial canvas, ink typography, and one cobalt accent; use no gradients or decorative branding tile.
- Keep the page curl dominant and secondary controls in a compact settings tray.
- Preserve light/dark themes, reduced motion, accessibility commands, diagnostics opt-in, safe areas, and responsive layout.
- Add no UI toolkit, icon package, image dependency, view-model framework, or settings persistence.
- Keep one canonical `docs/` tree and remove superseded history after merging durable content.
- Explain the non-stretching, stiff-paper contract and keep the basic library example short.
- Do not duplicate validation commands outside `.agents/skills/pagecurl-maui-validation/SKILL.md`.

---

### Task 1: Establish editorial resources and sample pages

**Files:**
- Modify: `src/PageCurl.Demo/App.xaml`
- Modify: `src/PageCurl.Demo/DemoPageSet.cs`

**Interfaces:**
- Consumes: `DemoPageSet.Create()` and caller-owned `SKImage` lifetime.
- Produces: the unchanged four-page collection, including the large fourth page, with a restrained editorial appearance.

- [ ] **Step 1: Replace the resource palette**

Define solid light/dark canvas, surface, border, text, muted, and cobalt colors.
Keep the default `Label` style. Replace the eyebrow and gradient button styles
with `CaptionLabel`, `ReaderButton`, `PrimaryReaderButton`, and
`IconReaderButton`. Remove all gradient brushes.

- [ ] **Step 2: Simplify generated-page scaling**

In `CreatePage`, compute one scale from the 900-pixel baseline:

```csharp
var scale = definition.Width / 900f;
float S(float value) => value * scale;
```

Use it for insets, font sizes, line spacing, marks, and page-number placement.
Delete repeated `definition.Width >= 1600` branches. Keep all Skia objects
scoped with `using` and the returned `SKImage` caller-owned.

- [ ] **Step 3: Apply the editorial page design**

Use warm paper variants, near-black ink, and cobalt details for all four pages.
Keep distinct titles and the 1,800-by-2,400 fourth page. Add thin horizontal
rules and small registration marks so stretch, shear, or a sharp fold remains
visible during solver validation.

- [ ] **Step 4: Build Mac Catalyst**

```bash
env DOTNET_ROOT=/Users/marius/.dotnet PATH=/Users/marius/.dotnet:$PATH /Users/marius/.dotnet/dotnet build src/PageCurl.Demo/PageCurl.Demo.csproj -c Debug -f net10.0-maccatalyst
```

Expected: 0 warnings and 0 errors.

- [ ] **Step 5: Commit**

```bash
git add src/PageCurl.Demo/App.xaml src/PageCurl.Demo/DemoPageSet.cs
git commit -m "style: establish editorial demo foundation"
```

---

### Task 2: Recompose the sample around the page surface

**Files:**
- Modify: `src/PageCurl.Demo/MainPage.xaml`
- Modify: `src/PageCurl.Demo/MainPage.xaml.cs`

**Interfaces:**
- Consumes: existing `PageNavigationState`, `PageTurnView`, completion event, hinge selection, reduced motion, and testing diagnostics.
- Produces: the same behavior through a smaller hierarchy plus settings and hint-toggle handlers.

- [ ] **Step 1: Replace the root XAML hierarchy**

Use four rows:

1. Slim header with `PageCurl` and `PageCounterLabel`.
2. Star-sized bordered stage with `PageView` and an end-aligned overlay `SettingsPanel`.
3. `GestureHintLabel` and quiet `StatusLabel`.
4. Footer with Previous, Next, and `SettingsButton`.

Remove the logo tile, gradient status badge, marketing headline, page counter
card, `EdgeHintsOverlay`, eight edge/corner markers, controls stack, and
`HintsButton`.

- [ ] **Step 2: Build the settings tray from current controls**

Inside `SettingsPanel`, retain `LockedEdgePicker`, `ReduceMotionSwitch`,
`TestingModeSwitch`, `TestingPanel`, and a text button that restores the gesture
hint. Keep explicit semantic descriptions. Add a labeled close button and keep
decorative marks outside the accessibility tree.

- [ ] **Step 3: Simplify presentation state in code-behind**

Delete `ApplyLockedEdgeVisuals` and all edge-overlay mutations. Add:

```csharp
private void OnSettingsClicked(object? sender, EventArgs e) =>
    SettingsPanel.IsVisible = !SettingsPanel.IsVisible;

private void OnCloseSettingsClicked(object? sender, EventArgs e) =>
    SettingsPanel.IsVisible = false;

private void OnShowGestureHintClicked(object? sender, EventArgs e) =>
    GestureHintLabel.IsVisible = true;
```

Hide `GestureHintLabel` after the first completed or cancelled gesture. Use
short status copy: `Ready`, `Turning`, `Turn cancelled`, `Complete`, and
`Final page`.

- [ ] **Step 4: Preserve lifecycle and diagnostic behavior**

Keep the timer opt-in, stop it on disappearance, cancel interaction, clear
image properties before disposal, and unsubscribe the completion event. Closing
settings must not alter hinge, reduced motion, or testing values.

- [ ] **Step 5: Build Mac Catalyst**

Run the explicit command from Task 1. Expected: 0 warnings and 0 errors.

- [ ] **Step 6: Launch and inspect wide and compact windows**

Verify the page stays aspect-fit and dominant, the settings tray overlays
instead of resizing the stage, and all footer commands remain reachable.

- [ ] **Step 7: Commit**

```bash
git add src/PageCurl.Demo/MainPage.xaml src/PageCurl.Demo/MainPage.xaml.cs
git commit -m "style: simplify the PageCurl sample"
```

---

### Task 3: Consolidate current documentation under `docs/`

**Files:**
- Create: `docs/index.md`
- Create: `docs/architecture.md`
- Create: `docs/interaction-and-rendering.md`
- Create: `docs/runtime-contracts.md`
- Create: `docs/testing.md`
- Create: `docs/platform-notes.md`
- Create: `docs/release-readiness.md`
- Modify: `docs/compatibility.md`
- Modify: `docs/validation/ios-release-readiness.md`
- Modify: `README.md`
- Modify: `CONTRIBUTING.md`
- Modify: `SUPPORT.md`
- Modify: `AGENTS.md`
- Modify: `.agents/skills/pagecurl-maui-validation/SKILL.md`
- Modify: `.gitignore`
- Delete: `knowledge/`
- Delete: `docs/superpowers/`
- Delete after implementation: `docs/design/`

**Interfaces:**
- Consumes: current source, retained validation evidence, research sources, and durable content from `knowledge/project/`.
- Produces: one current entrypoint at `docs/index.md` with valid relative links.

- [ ] **Step 1: Create canonical pages by merging content**

- `docs/architecture.md`: project boundaries, frame flow, ownership, and fixed-buffer solver state.
- `docs/interaction-and-rendering.md`: gesture contract, arbitrary valid edge/hinge pairs, XPBD constraints, stiff-paper behavior, normals, sorting, and performance choices.
- `docs/runtime-contracts.md`: image ownership, UI threading, lifecycle, cancellation, recycling, and diagnostics.
- `docs/testing.md`: deterministic invariants, allocation guards, visual/device scenarios, and regression policy.
- `docs/platform-notes.md`: SDK/workload setup, explicit targets, host limitations, and package caveats.
- `docs/release-readiness.md`: release gates, known risks, physical-device profiling, and short next steps.

Link the original PBD, XPBD, and paper-bending papers from the rendering page.
Use concise prose and link to source files rather than copying implementation.

- [ ] **Step 2: Add `docs/index.md`**

Link each canonical page, `compatibility.md`, and retained validation records.
State that the validation skill owns commands and `docs/validation/` owns dated
evidence.

- [ ] **Step 3: Make the README API path short**

Keep one minimal XAML example, one image-assignment example, and one completion
handler. Explain that the default material behaves like stiff paper and that
images remain caller-owned. Move advanced behavior and solver rationale to
canonical docs.

- [ ] **Step 4: Update repository entrypoints and guidance**

Update `CONTRIBUTING.md`, `SUPPORT.md`, and `AGENTS.md` to use canonical docs.
Replace instructions to read or update `knowledge/project/` with the matching
`docs/` page.

- [ ] **Step 5: Update retained links and the validation skill**

Fix links in `docs/compatibility.md` and
`docs/validation/ios-release-readiness.md`. Point the skill reference list to
`docs/testing.md` and `docs/platform-notes.md`; keep its command matrix intact.

- [ ] **Step 6: Ignore local mockups**

Add `.superpowers/` to `.gitignore` so the visual-companion workspace never
becomes a repository artifact.

- [ ] **Step 7: Remove obsolete documentation exactly**

Delete resolved files under `knowledge/` and `docs/superpowers/`, including
their `.DS_Store` entries. After both work tracks and canonical docs are
complete, delete the temporary `docs/design/` files. Git history remains the
archive.

- [ ] **Step 8: Verify stale paths are gone**

```bash
rg -n "knowledge/|docs/superpowers|docs/design" --glob '*.md' --glob '*.yml' --glob '*.yaml' --glob '*.cs' --glob '*.xaml' .
```

Expected: no retained source or documentation references a removed path.

- [ ] **Step 9: Verify local Markdown links and diff hygiene**

Run a one-off link check that ignores web URLs and anchors and fails for a
missing relative target. Then run:

```bash
git diff --check
```

- [ ] **Step 10: Commit**

```bash
git add .gitignore AGENTS.md README.md CONTRIBUTING.md SUPPORT.md docs knowledge .agents/skills/pagecurl-maui-validation/SKILL.md
git commit -m "docs: consolidate PageCurl guidance"
```

---

### Task 4: Validate the integrated product

**Files:**
- Review: `src/PageCurl.Demo/`
- Review: `docs/`
- Update only from completed runs: `docs/validation/ios-release-readiness.md`

**Interfaces:**
- Consumes: completed paper-solver, demo, and documentation work.
- Produces: fresh build, visual, interaction, accessibility, and documentation evidence.

- [ ] **Step 1: Run platform-independent checks**

```bash
dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj -c Release
dotnet build src/PageCurl.Maui/PageCurl.Maui.csproj -c Release -f net10.0 -p:TargetFrameworks=net10.0
```

- [ ] **Step 2: Build and inspect Mac Catalyst**

Use the user-local SDK. Launch the app and verify light/dark presentation, wide
and compact layouts, settings open/close, each hinge value, reduced motion,
testing diagnostics, Previous/Next, hint restoration, cancellation, and the
large fourth page.

- [ ] **Step 3: Exercise stiff-paper behavior visually**

Drag every valid free edge and corner with Auto and explicit hinges. Check slow
release, reversal, snap-back, flick commit, and backward turns. Confirm page
rules do not lengthen, the surface does not form a one-cell crease, and the page
settles flat. Save screenshots or video outside tracked source.

- [ ] **Step 4: Run iOS smoke when CoreSimulator responds**

```bash
./tools/ios-smoke.sh
```

If `simctl` cannot list devices, capture the completed timeout or error and
record iOS as not refreshed. Do not edit source for a simulator-service failure.

- [ ] **Step 5: Run repository checks**

```bash
bash tools/check-workflow-projects.sh
dotnet sln PageCurl.sln list
git diff --check
git status --short
```

- [ ] **Step 6: Record only completed evidence**

Record date, commands, target, scenarios, and evidence paths. Keep physical
device profiling, store signing, and unavailable simulator work as explicit
release gates.
