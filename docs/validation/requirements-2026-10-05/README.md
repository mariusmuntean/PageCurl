# Requirements and robustness validation — 2026-10-05

This record covers the requirements, API, testing and robustness changes after
`b392c93`. Earlier cleanup records describe earlier source and test counts.
The [requirements contract](../../requirements.md) is the acceptance source.

Environment: macOS arm64, .NET SDK 10.0.401, pinned workloads 10.0.401.1,
Xcode 27 with Apple SDK 27.0.10722. Apple builds used the supported toolchain,
explicit Mac Catalyst frameworks and serialized MSBuild. No Xcode version override.

## Completed checks

| Check | Result | Evidence |
| --- | --- | --- |
| Core Release runner | All 12 groups passed, including 500 reused-controller commit/rollback turns, invalid frame times, reentrant completion and warmed allocation guards | [Core](core-final.log) |
| Neutral MAUI API/lifecycle runner | All 5 groups passed | [MAUI](maui-final.log) |
| Repeated neutral runner | 100/100 complete runs passed, each including all 5 groups | [Repeat](maui-repeat-final.log) |
| Fresh external package consumer | Core and neutral MAUI packed; isolated PackageReference restore, compile and execution passed | [Consumer](package-final.log) |
| Validation-driver/workflow unit tests | 9 tests passed; timeout test includes four invalid values | [Tooling](tooling-final.log) |
| Workflow project references | All 6 referenced project paths resolve | [Workflow](workflow-final.log) |
| Native Mac Catalyst fixture | All suites passed, including capture pixels, mesh shrinking, real native hit testing, four-scene commands, refresh failure/re-entry and compact live layout | [Assertions](native-final.log), [build](native-final-build.log) |
| Mac Catalyst demo Debug | Build passed | [Build](demo-final-build.log) |
| Android demo Release | Build passed, 0 warnings/errors | [Build](android-final.log) |
| Visible Mac application | Four-scene command boundaries, real pointer commit/rollback, compact Options flow and live Content/refresh passed | [Computer-control observations](computer-control.md) |
| Documentation and change hygiene | Local links in all changed documentation pages resolve; JavaScript syntax and git diff whitespace checks pass | Local checks completed |

The native driver additionally repeated the final fixture with its final timeout
validation: [repeat assertions](native-final-repeat.log). Apple builds report the existing deprecated Xamarin settings-file
warning. The native host reports an inherited background-fetch configuration
diagnostic; it exits successfully and emits the required final suite marker.
These are not platform certification claims.

## Findings and fixes

- A completion subscriber could start another animated turn while `Tick` returned
  false. The failing Core regression now passes; animation continues.
- Invalid wrapper enums were stored and diverged from the renderer. Public API
  tests now verify retained valid state, enum rejection, finite dim-factor
  clamping and immediate reduced-motion pointer cancellation.
- Explicit refresh hid capture failure and overwrote inactive scene status.
  Native injected failures/suspended captures reproduced both. A later regression
  reproduced leaving and re-entering Content before capture completion. Refresh
  reporting now requires matching lifecycle and scene generations and no failure.
  See [initial failures](native-red.log) and [re-entry failure](reentry-red.log).
- Screenshot inspection showed the live counter clipped on a narrow page. A
  native 250×333 layout regression reproduced a 3-pixel counter with 52-pixel
  type. Compact spacing/type sizes now keep the counter and button visible.
  See [layout failure](compact-red.log).
- Package packing initially failed because Core declared an unpacked README.
  Core now includes that README; the isolated consumer passes.
- The workflow-path checker ignored tests projects. New unit tests reproduced
  the gap; the scanner now checks both source and test project references.
- The native driver now rejects stale/partial output, failed assertions, missing
  executables, timeouts and non-finite/non-positive timeout values. See
  [invalid-timeout failures](tooling-timeout-red.log).

Independent read-only review was repeated after fixes and found no remaining
material source or harness defect. Hosted CI was configured but not executed in
this local validation. No fresh iOS simulator interaction, Windows, physical
device, screen-reader, signing/publication, sustained GPU/texture or published
binary API-baseline certification was performed. Those remain explicit release
gates. No task-started fixture process is retained; the pre-existing demo session
is left running after the visible checks.
