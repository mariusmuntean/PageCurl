# Small reader example — 8 October 2026

Added a complete five-page reader recipe to the spread guide. It uses the public
navigation and snapshot APIs, committed completion, distinct slot views, capture
readiness, retry and an odd final page. Window/page teardown cancels interaction
and pending waits before hiding the book. No library API, demo scene, runtime
dependency, timer or animation-frame callback was added.

The guide is the source of truth. A shared extractor includes its unchanged
XAML/code-behind in the neutral example compiler and native assertion host.
Extraction rejects missing, malformed, ambiguous or mismatched examples and
preserves unchanged output timestamps. CI compiles both documented examples.

## Completed evidence

- Core: all 12 deterministic groups passed in Release.
- Neutral MAUI: all five groups passed in Release.
- Both unchanged guide examples compiled with zero warnings/errors.
- Isolated package consumption passed with a fresh package cache, transitive
  Core dependency and representative public calls. Neutral package inspection
  and SDK validation passed.
- All 61 Python tooling tests and five computer-control replay tests passed.
  Workflow syntax, project references, shell syntax and whitespace checks passed.
- DocFX built with zero warnings/errors; links, anchors and publication
  boundaries passed. This validates site output locally; it does not deploy the
  separate public documentation repository.
- The Release native fixture completed with 119 PASS outcomes, zero FAIL,
  exit 0 and the final suite marker. It compiles the exact guide with the demo's
  XAML source generation. The only build warning concerns the local SDK's
  deprecated Xamarin settings file.
- Reader coverage includes both boundaries, rapid commands during settling,
  forward/backward journeys, odd-page mapping, distinct views, gesture commit
  and rollback, stop/resume, leaving/re-entry, capture failure and retry.
- Native text-fit checks cover 600, 390 and 320-point root layouts. The original
  fixed rows failed at 390: a body required 114 points but received about 90.
  Scrollable text retains the original fonts and a fixed page number. Inspected
  a native 320-point-layout page capture after scrolling.
- Exact native pixel comparison initially rejected a turn using text from
  before scrolling. Coalesced refreshes now preserve the latest visible pixels.
- A deferred UI continuation holds an old retry until a new activation has a
  pending retry. Removing the activation-token guard fails before recovery can
  hide the result; restoring it passes. Stale provider failure cannot change the
  resumed activation's error or command state.
- Unlocked desktop input exercised a button turn, forward edge gesture to page
  five, backward edge gesture and Previous back to pages 1–2. Endpoint states
  and counters agreed. Native quarter-window resizing preserved readable pages
  and commands. Task-started preview apps were closed.

The initial book description grouped page text into one accessibility element.
It was removed after inspecting the actual AX tree. The Mac locked before the
final tree check, so native assertions verified current-page labels and every
ancestor's accessibility, visibility and opacity flags. Reinstating the old
grouping fails; removing it passes. This automates exposed-text coverage without
claiming screen-reader usability.

## Decisions and limits

- Kept the example in the existing guide and native host, without a new
  application scaffold or library API. This reduces duplication; extraction and
  compilation are required to prevent documentation drift.
- Constrained the native root layout for phone-width checks. Mac Catalyst does
  not support resizing through `Window.Width`/`Height`, as described in
  [Microsoft's window documentation](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/controls/window?view=net-maui-10.0#mac-catalyst).
  A temporary page arrangement was insufficient because native relayout restores
  desktop bounds. Root constraints allow actual child layout and capture to
  settle; desktop resizing remains separate real-input evidence.
- Deferred the real retry continuation on the UI thread to control its ordering.
  Default cancellation completed inline and could not expose the late-completion
  race. This test proves state isolation for that ordering, not scheduler timing.
- Scroll input refreshes only the visible reader's book, with one refresh loop
  and its activation token. This adds capture work when text moves; pixel
  comparison protects quality and no idle or animation-frame work is introduced.
- Native accessibility flags can automate exposed-text and ancestor-grouping
  checks while the desktop is locked. They do not establish VoiceOver navigation
  or physical-device accessibility.

The final review found two important gaps: phone text clipping and a retry
assertion masked by later recovery. Both received stronger regressions. No minor
findings were deferred. Logs and the native page image are under
`.artifacts/reader-*`; the plan is `docs/plans/2026-10-07-reader-example.md`.

Hosted checks for the preview commit did not start because of account billing
or spending restrictions: [Verify](https://github.com/mariusmuntean/PageCurl/actions/runs/37736074488)
and [Documentation](https://github.com/mariusmuntean/PageCurl/actions/runs/37736074489).
Local evidence does not replace hosted success. Platform/runtime, hardware,
screen-reader, signing and publication gates remain in
[release readiness](../../release-readiness.md).
