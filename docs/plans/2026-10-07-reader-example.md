# Small reader example implementation plan

**Goal:** A complete five-page reader recipe that a MAUI developer can copy,
compile and run without reproducing the showcase application.

**Architecture:** Keep the XAML and code-behind in `docs/site/spreads.md` as the
canonical example. Extract those exact snippets for neutral compilation and
the existing native assertion host. Use `PageSpreadNavigationState` and the
public `PageSpreadView` API; add no library API or demo scene.

**Execution:** Implement after the motion preview is verified, committed and
pushed. Commit and push this milestone after its checks pass.

**Constraints:** .NET 10 and MAUI 10; preserve the four demo scenes; no per-frame
reader work, new runtime dependencies, source-text wording tests or generated
application scaffold. Reuse the current snapshot and completion paths.

## Files and responsibilities

- `docs/site/spreads.md`: complete `ReaderApp.ReaderPage` XAML/code-behind and
  explanation of state, sheet mapping, capture readiness and the odd last page.
- `tools/extract-doc-example.py`: extract one complete XAML page and its matching
  code-behind verbatim; reject missing or ambiguous examples. Write only changed
  output so repeated builds do not invalidate compilation.
- `tools/test-quick-start.sh`: compile both documented examples in separate
  temporary neutral projects, with warnings treated as errors.
- `tools/tests/test_doc_examples.py`: real extraction fixtures for complete,
  missing and ambiguous examples, including the smaller spread binding snippet.
- `tests/PageCurl.Apple.Tests/PageCurl.Apple.Tests.csproj`: extract and include
  the same reader snippets before building the native host.
- `tests/PageCurl.Apple.Tests/ReaderExampleTests.cs` and `FixturePage.cs`:
  exercise the documented reader through native layout, buttons and gestures.
- `README.md`, `docs/site/index.md`, `docs/testing.md` and a dated validation
  record: entry points and completed evidence only.

## Behavior and regression checklist

- [x] Write and run failing extraction tests before adding the shared extractor.
- [x] Write the native reader expectations before implementing the recipe.
- [x] Start on pages 1–2; Previous disabled, Next enabled only after capture.
- [x] A settling command disables both navigation buttons and leaves the counter
  unchanged until committed completion; rapid extra clicks cannot skip a spread.
- [x] Forward reads 1–2, 3–4, then 5; backward returns 3–4, then 1–2. Both endpoint
  commands are rejected without changing state.
- [x] Each occupied content slot has its own view instance. Before the last
  forward turn, NextLeft holds page 5 and NextRight is a blank view. Afterward
  Left holds page 5 and Right is null; backward navigation remains available.
- [x] A gesture commits through the same handler as a button. A rollback keeps
  the current spread and all page content unchanged.
- [x] Stopping or leaving cancels an active turn without advancing navigation.
  Resuming restores capture and commands. A hidden page does not reactivate
  when the old window resumes; leaving detaches window subscriptions.
- [x] A failed native capture displays Retry without changing the counter.
  Retry awaits actual capture, prevents overlapping retries and restores current
  command readiness. Delayed failure after hiding cannot affect the active page.
- [x] At phone widths, all text remains reachable at its original font sizes.
  Scrolling refreshes the turn snapshot without timers or concurrent captures.
- [x] Compile both unchanged guide examples; run Core, neutral MAUI, tooling,
  package consumption and the complete native fixture. Build DocFX and check its
  links and publication boundaries. Inspect the reader visibly after unlock.
- [x] Record results, review the final diff for duplication and unnecessary
  abstractions, then commit and push. Check the matching hosted runs separately.

## Review focus

Readiness can lag behind content assignment; button state must follow the
control's readiness properties. Completion can arrive during teardown; mark the
page inactive before cancelling. Optional-back failure may permit a fallback
turn; show failure honestly without overriding the control's readiness policy.
The last spread has one real page, so its blank destination belongs only to the
incoming turn. Generate views only when changing spreads, and let the wrapper
own its snapshots.
