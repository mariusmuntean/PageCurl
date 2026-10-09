# Showcase polish — 2026-10-05

The sample now uses short instructions in place of slogans. Generated image
pages and live page views share one copy catalog. Option descriptions explain
what each setting does. Dark-mode option headings use a lighter accent.

The repository [showcase checks skill](../../../.agents/skills/pagecurl-showcase-checks/SKILL.md)
records the repeatable native and computer-control workflow.

## Regressions and corrections

- The navigation replay previously accepted an unchanged counter even when a
  boundary button was incorrectly enabled. A failing replay test reproduced
  that false positive. The replay now checks both button states explicitly.
- The native theme assertion measured dark option-heading contrast at 2.34:1.
  The corrected accent measures 6.53:1; light appearance measures 5.72:1.
- Independent review caught a higher-resolution claim shared with live-view
  scenes. The text now specifies that the larger image is used in Single.
- A new compact test initially timed out because setting Catalyst Window.Width
  after launch did not resize the native window. It now arranges the actual
  page at a 600×750 constraint. A wrong Grid/Border lookup was also corrected
  before retaining passing assertion evidence.

## Completed checks

Local SDK: .NET 10.0.401, Xcode 27, Mac Catalyst arm64, version checks enabled.
Both native builds used explicit net10.0-maccatalyst targets and serial MSBuild.

- [Native fixture](native.log): exit 0, no failed assertions, final `PASS all native suites`.
  [Fixture build](native-build.log) and [demo build](demo-build.log): zero errors;
  one existing Xamarin settings deprecation warning each.
- Core: all 12 existing test groups passed.
- Neutral MAUI: all 5 existing test groups passed.
- UI-replay unit tests: 5 passed, including both boundary failures, motion
  restoration from either initial value, accessible roles and failure cleanup.
- Python validation-driver tests: 10 passed. Workflow project paths passed.
- Showcase skill structure validated. Whitespace checks passed.

## Real-input observations

The rebuilt Debug app ran through computer control with fresh accessibility
observations after actions. Each of Single, Spread, Content and Backside passed
seven wide-layout navigation assertions: first boundary, forward, backward,
last boundary, blocked extra forward, return to first and blocked extra backward
(28 total). Each endpoint also required the correct enabled/disabled controls.
Reduced motion was restored after each journey.

The live Content counter changed 0→1 and reported “Live content updated”. Explicit
refresh reported “Snapshot refreshed”. Seven navigation/option controls exposed
the expected accessible names and roles. A screenshot-selected edge drag on
live Content committed 01→02 with “Turn complete”.

Native window tiling exposed the smaller desktop layout, where the revised
page instructions, live counter and button remained visible. Freehand resize
attempts did not reach the compact breakpoint through this interface; no new
compact computer-control pass is claimed. Compact Options routing and live-page
layout are covered by the native assertion host instead.

[Desktop layout screenshot](sample.jpg) was captured from the final rebuilt app.
It records the wide layout, scene selector and scrollable Options panel at rest;
use the [motion preview](../../site/index.md) to inspect a turning sheet.

VoiceOver/TalkBack, hardware input, video playback and physical-device release
gates were not certified by this pass. The [testing strategy](../../testing.md)
and [platform notes](../../platform-notes.md) retain those distinctions.
