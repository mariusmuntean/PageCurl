---
name: pagecurl-showcase-checks
description: Use when changing the PageCurl demo presentation, page copy, accessibility labels, or computer-control journeys, or collecting visible showcase evidence before release.
---

# Showcase checks

Read `README.md`, `docs/testing.md` and `docs/requirements.md`. Use the
`pagecurl-maui-validation` skill for the build and native assertion commands.
Preserve the four scenes and the common completion path.

## Before changes

Inspect the current app at the actual target size. Keep page text short,
specific and useful: explain what a person can try. Avoid slogans and internal
rendering terminology. Generated images and live page views share
`src/PageCurl.Demo/DemoPageCopy.cs`; edit the copy there.

Behavior fixes need a failing regression first. Do not test exact wording with
source-text searches. Use native layout/contrast assertions for presentation
failures, and UI-replay tests for missed or incorrectly reported interactions.

## Real input checks

1. Inventory running apps before launch. Use the explicit path of the freshly
   built app to avoid mixing Debug and Release. Preserve pre-existing sessions.
2. Use `mcp__cua_repl` for all UI interaction. Read its documentation first.
   Read `tests/computer-control/pagecurl-checks.js` and paste the relevant
   function declarations directly into the tool code. Do not use `eval` or
   dynamic code generation; the runtime disallows it. The replay uses only
   documented `cua` APIs.
3. Run `runPageCurlChecks(cua, absoluteAppPath, [[sceneLabel, isSpread]])` one
   scene per bounded call. Check all four scenes: Single, Spread, Content,
   Backside. It checks both endpoints and directions and restores reduced motion.
4. Run `runPageCurlLiveContentCheck(cua, absoluteAppPath)` and
   `runPageCurlAccessibilityCheck(cua, absoluteAppPath)`. Inspect actual roles
   and labels, rather than inferring accessibility from visible text.
5. Capture a fresh screenshot before choosing drag coordinates. Run
   `runPageCurlGestureCheck(cua, absoluteAppPath, from, to, counter, status)`
   with a result different from the initial state. Inspect an active curl when
   evaluating geometry or backside appearance.
6. Resize using the native window controls. Repeat relevant journeys in a
   compact layout; inspect Options, Close, page text, counter and live button.
   Verify no overlap or clipped instructions at the smallest tested size.
7. Close only apps started for these checks and remove temporary test files
   after retaining necessary evidence.

Run `node --test tests/computer-control/pagecurl-checks.test.js` whenever the
replay changes. Run the native fixture for demo layout, theme or event-handler
changes. Native fixture success and real-input success are separate evidence.

## Evidence

Record completed commands, assertion counts, platform/build configuration,
layout sizes and observations in a dated `docs/validation/` record. State what
was not exercised. AX names do not prove screen-reader usability; a native
build does not prove runtime behavior; a Mac pass does not certify iOS or
Android. Do not claim hosted CI success until the matching commit has passed.
