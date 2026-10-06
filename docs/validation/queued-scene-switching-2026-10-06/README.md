# Queued scene switching — 6 October 2026

The earlier [scene-switch review](../scene-switch-performance-2026-10-06/README.md)
started its stopwatch on the UI thread. It measured the handler and next
dispatch, but missed input waiting behind synchronous native capture. It also
did not require a newly visible idle GL view to paint.

Implementation: [demo refresh scheduling](../../../src/PageCurl.Demo/MainPage.xaml.cs),
[visible rendering](../../../src/PageCurl.Maui/PageTurnView.cs) and
[pending spread capture](../../../src/PageCurl.Maui/PageSpreadView.cs).
Regression coverage: [native timing](../../../tests/PageCurl.Apple.Tests/SceneSwitchTimingTests.cs),
[live refresh](../../../tests/PageCurl.Apple.Tests/InteractiveRefreshTests.cs) and
[neutral spread lifecycle](../../../tests/PageCurl.Maui.Tests/PageSpreadViewTests.cs).

## Reproductions and fixes

- Five rapid real Add one inputs issued five native captures. A scene request
  queued from a worker during the first capture waited **363.4 ms** before its
  handler began. The [baseline](native-before.log) failed the capture-count and
  queue-response checks. The demo now batches queued counter inputs into one
  capture of their latest value. It clears the queued state before capture so
  new input during an asynchronous provider still schedules another refresh.
- A stronger native check found **753.2 ms** from a queued Spread-to-Single
  request to Single's first visible idle paint, despite a 13.0 ms handler. The
  [paint regression failed](native-paint-red.log). Showing `PageTurnView` now
  requests a fresh paint even with unchanged images; it remains an on-demand
  renderer. The [first green run](native-paint-green.log) measured 36.0 ms for
  that case and passed the complete fixture.
- Cancelling a spread could synchronously drain dirty content before the
  caller hid it. The [deterministic regression failed](neutral-red.log).
  Pending automatic capture now yields to that caller and rejects a changed
  lifecycle, including immediate hide/re-entry. Normal completion, reentrant
  turns, explicit refresh waits and dirty-slot serialization remain covered.
- A queued counter refresh could capture the incoming Image/Video page after
  changing content kind. The [new native regression failed](native-content-kind-red.log).
  Content-kind changes now invalidate the demo presentation revision, and the
  drain requires the live page to remain selected. Re-entry with the same live
  view, scene re-entry and window stop also discard obsolete requests.
- The new invalidation exposed a pending explicit refresh leaving the next
  content kind displaying `Capturing`. The [status regression failed](native-status-red.log).
  Changing kind now resets the incoming presentation to `Ready`; selecting
  the same kind preserves its current status. The gated old capture must not
  overwrite that status, recapture the replacement or retain its image.

## Measurements

Native Mac Catalyst Debug, arm64, 1728 by 1000 window. No compilation ran
during the timing samples. Each request originates on a worker; latency starts
before dispatch to the UI thread. The live-burst test verifies that the request
timestamp falls within the actual native capture interval.

| Case | Before | Final run |
| --- | ---: | ---: |
| Five-input live burst: native captures | 5 | 1 |
| Live burst: request to handler | 363.4 ms | 68.0 ms |
| Live burst: request to next dispatch | 394.7 ms | 100.6 ms |
| Spread-to-Single, 0 ms: request to first paint | 753.2 ms | 36.1 ms |

The [repeat](native-repeat.log) also passed all suites with 106 `PASS` lines:
the live burst queued for 67.7 ms, dispatched at 100.3 ms and painted at
99.0 ms, with one capture. The six spread switches painted at 36.8–68.1 ms;
all eighteen rendered-drag switches again met the unchanged response limits.

The [final run](native-final.log) passed all suites with 106 `PASS` lines,
including the final completion marker:

- Six pending-spread switches at nominal 0, 8, 16, 24, 40 and 80 ms: queue
  wait 0.0–27.7 ms, handler 13.0–15.5 ms, next dispatch 14.7–63.2 ms,
  and first visible idle Single paint 36.1–61.5 ms. None captured outgoing
  content in the handler or after hiding.
- Eighteen switches from Single to Spread, Content and Backside after three
  actual GL drag frames and release, with the same delay sweep: queue wait
  0.0–31.2 ms, handler 11.7–29.2 ms and next dispatch 20.7–106.2 ms.
  Actual release-to-request delay is logged alongside the nominal delay.
- The live-burst destination painted at 99.3 ms. Queue and handler must stay
  below 150 ms; next dispatch and visible Single paint below 250 ms. Setup,
  capture signals and dispatch completion have bounded waits.

Navigation readiness is measured separately. Spread still needs native
destination capture: 308.7–342.5 ms in this run. A quick dispatch does not mean
those captures have finished. The paint event proves native drawing occurred;
it does not measure display presentation or certify physical-device frame rate.

An [additional capture audit](native-capture-audit.log) passed all suites with
107 `PASS` lines. Input queued during the first incoming spread capture waited
65.8 ms, dispatched at 97.5 ms and painted at 96.0 ms. Only one outgoing slot
was captured before the scene changed. The existing yield between slots already
services this input; no additional capture-loop change was needed. A neutral
regression now also protects that behavior and retained image ownership.

The [complete run after the status fix](native-complete.log) passed all suites
with 108 `PASS` lines, including the new explicit-capture status/ownership
regression. Its live burst queued for 69.2 ms, dispatched at 99.9 ms and painted
at 98.6 ms, with one capture. All prior response and quality checks still passed.

## Quality and test integrity

The automatic five-input result must match every pixel and the dimensions of
an explicit native capture of the latest live counter. All five inputs remain
in the counter. The Apple snapshot path, recent-screen-change inclusion, native
pixel density, sRGB/alpha conversion, mesh settings and allocation guards are
unchanged. Explicit Refresh retains its immediate, awaitable path.

Injected-provider tests isolate the demo coalescer from legitimate automatic
replacement-content capture. They require no obsolete capture/status on Image,
Video or live-view re-entry, and preserve the latest counter during an unfinished
capture. Each returned image has independent ownership.

The first neutral integration run exposed an existing assertion that expected
all retained turn images to be disposed after the original capture task, before
the new deferred drain completed. The [failure is retained](neutral-integration-failure.log).
The strengthened test gates resumed capture, requires all three textures to
remain alive while suspended, then awaits the actual refresh chain and requires
all three disposed and both directions ready. Both turn directions remain tested.
No previous latency limit, pixel, ownership or allocation assertion was relaxed.

## Completed validation

Environment: SDK 10.0.401, workload set 10.0.401.1, MAUI 10.0.110,
SkiaSharp 4.150.1, Xcode 27.0; Apple version checks enabled. Commands follow the
[repository validation skill](../../../.agents/skills/pagecurl-maui-validation/SKILL.md).

- [Core Release: all 12 groups passed](core-tests.log), including warmed
  zero-managed-allocation checks.
- [Neutral MAUI Release: all 5 groups passed](neutral-green.log), including
  the stronger deferred capture ownership tests.
- [Native Debug build passed](native-build.log); one existing deprecated
  Xamarin Settings.plist warning, no errors. The full native run includes
  color/alpha, mesh, window/video lifecycle, both navigation layouts, failure,
  cancellation and the new queued timing/content tests.
- [Android Release demo build passed](android-build.log), zero warnings/errors.
- [Mac Catalyst arm64 Release demo build passed](maccatalyst-build.log), no
  errors and the same deprecated Settings.plist warning.
- [iOS arm64 simulator Release demo build passed](ios-build.log), no errors
  and the same deprecated Settings.plist warning; this is build evidence only.
- [Documentation build passed](docs-build.log), zero warnings/errors; link,
  anchor and publication-boundary checks also passed.

The completed checks used `/Users/marius/.dotnet/dotnet` from the repository
root. Core ran with `run --project tests/PageCurl.Core.Tests -c Release`;
the neutral suite used `run --project tests/PageCurl.Maui.Tests -c Release
-f net10.0 -p:TargetFrameworks=net10.0`. Native builds used
`build tests/PageCurl.Apple.Tests -c Debug -f net10.0-maccatalyst
-p:RuntimeIdentifier=maccatalyst-arm64 -m:1 -nr:false`, followed by
`python3 tools/run-native-fixture.py` with that built app binary and a distinct
`--output` log for each run. Demo Release builds used the explicit Android,
Mac Catalyst arm64 and iOS simulator arm64 targets above, with `-m:1 -nr:false`.
Documentation used `PAGECURL_DOTNET=/Users/marius/.dotnet/dotnet bash tools/build-docs.sh`.
The final whitespace and index checks passed; no changes were staged or committed.

No computer control was needed: the assertion host runs the actual linked demo
page, native GL rendering, native captures and real button/touch event handlers
with controlled timing. OS pointer delivery, hardware performance, Windows
runtime and hosted CI for these uncommitted changes remain unverified.
