# Production hardening — 6 October 2026

Local source, test, documentation and running-demo review. This record does
not certify production hardware, signing, publication or hosted CI for these
uncommitted changes.

## Regressions and fixes

Seven new neutral regressions cover interruption and callback behavior. The
[red runs](regression-failures.log) show the failures before their fixes:

- A rejected live-page command hid an already active curl. Busy commands now
  return before changing presentation, in either direction.
- Image-control presses could replace a settling turn and lose its pending
  completion. Native touch input now uses the same guarded interaction entry
  point as the wrappers. Duplicate and foreign presses during an owned drag
  remain consumed; the original pointer retains control.
- Cancellation subscribers could restart disconnected live-page and spread
  wrappers before their textures were released. Both wrappers reject that
  restart during teardown.
- On connected wrappers, cancellation could hide or clear a replacement turn
  started synchronously by its subscriber. Cleanup now preserves that new turn,
  its textures and its completion.

The full native navigation journey reproduced a separate refresh race:
[newly assigned spread views were captured with logical dimensions -1 by -1
and zero native bounds](native-layout-failure.log). Serialized refreshes now
wait for layout events, recheck revisions and wake on cancellation, content
replacement, hiding or handler teardown. Two native probes require cancellation
and disconnection to release a wait on unarranged content without a failure
notification or false navigation readiness.

No geometry resolution, material-length tolerance, allocation assertion,
rendering quality, public API or existing acceptance assertion was reduced.

## Automated demo coverage

The native fixture runs all four scenes in wide and 600 by 750 compact page
layouts. It normalizes the starting page, visits every page/spread in both
directions, checks every counter and completion status, asserts both endpoints,
and compares wide/mobile navigation state. Each scene also completes an
animated forward/backward pair in each layout with commands disabled during
settling: sixteen additional animated turns.

The journey waits for the requested direction's button before an animated
command. An initial probe waited for either direction; a disabled Previous
button correctly ignored that attempt. Direction-specific readiness fixed that
fixture assumption while retaining the animation and boundary assertions.
The fixture's layout probes remove native children before disconnecting their
handlers, preserving MAUI's required removal context.

This coverage runs in the existing hosted native-fixture lane. It reduces the
need for desktop button replays while preserving real-input and visual checks
where they provide distinct evidence.

## Completed checks

Environment: .NET SDK 10.0.401, workload set 10.0.401.1, MAUI 10.0.110,
SkiaSharp 4.150.1 and Xcode 27.0. Apple version checking stayed enabled.

| Check | Result |
| --- | --- |
| Core Release runner | [12 groups passed](core-tests.log), including warmed zero-allocation guards |
| Neutral MAUI Release runner | [5 groups passed](maui-tests.log), including seven new regressions |
| Native Mac Catalyst fixture | [All suites passed](native-assertions.log), including full/animated journeys and layout-wait probes |
| Android Release demo | [Build passed](android-build.log), zero warnings/errors |
| Mac Catalyst Release demo | [Final source build passed](maccatalyst-build.log); native assertions run separately |
| NuGet known vulnerabilities | [Demo transitive dependency query reported none](vulnerabilities.json) |
| External package consumer | [Fresh isolated packages restored, compiled and ran](package-consumer.log) |
| Native package assets | [Neutral, Android, iOS and Mac Catalyst assemblies/XML/Core dependencies passed](package-assets.log); Windows assets require the Windows host |
| Validation tooling | [20 tests passed](tooling-tests.log) |
| Computer-control replay checks | [5 tests passed](replay-tests.log) |
| Documentation | [Warning-free build and link/anchor/publication checks passed](documentation-build.log) |
| Workflow project references | All referenced projects exist; solution project list inspected |
| iOS Release simulator demo | [Final source build passed](ios-build.log); existing deprecated Settings.plist warning |
| iOS smoke | [Build, install, launch and light/dark captures completed; HID step failed](ios-smoke.log) because Xcode has no SimulatorKit framework |

Commands follow the [repository validation skill](../../../.agents/skills/pagecurl-maui-validation/SKILL.md).
The native assertion host was run with the bounded driver and explicit output
file. Package consumption used an isolated local feed and empty cache; the
native package was also packed and inspected separately. Builds used explicit
frameworks and the workload-enabled local CLI.

An initial docs invocation selected system SDK 10.0.400; rerunning with
`PAGECURL_DOTNET=/Users/marius/.dotnet/dotnet` succeeded. A concurrent neutral
pack/platform restore initially produced NETSDK1005; serial restores/builds
succeeded. These were SDK/restore-state failures, not source fixes. Apple builds
retain the existing deprecated Xamarin Settings.plist warning.

## Running demo and performance observations

Targeted desktop input on the freshly built Mac Catalyst demo verified:

- a diagonal Single drag advanced 01 to 02 and reported Turn complete;
- a physical spread drag crossed the gutter and advanced 01–02 to 03–04;
- the live Content button advanced its counter from 0 to 1 and reported
  Live content updated;
- wide rendering, native accessibility names and the restored spread landing
  remained readable.

These checks used computer control only for actual pointer/AX/visual behavior.
Full command journeys and compact option/layout checks ran in the native
fixture. No new full desktop replay or hardware screen-reader pass is implied.

The native log retains frame and submission timings. The five measured Single
paint frames are a diagnostic sample of a large desktop viewport; the 1,000
mesh submissions include reflection overhead. Neither is a hardware frame-rate
or long-run GPU-memory certification.

A final standalone Release window check was blocked by the locked Mac. The
earlier real-input checks preceded the final competing-pointer preservation
refactor; the final native fixture exercises the updated touch entry point.

The simulator's compact initial scene was inspected in
[light appearance](ios-light.png) and [dark appearance](ios-dark.png).
The HID error prevented simulator gesture and scene journeys. The smoke
preceded the final pointer-routing refactor; the separate final iOS build
validated that source. The available
paired physical iPhone's CoreDevice connection failed with a connection reset;
no physical-device run is claimed.

Task-started desktop apps exited, the smoke shut down its newly booted simulator,
and existing apps/simulator helpers were preserved. Temporary test outputs were
removed after retaining this evidence.

## Open release gates

The final changes still need their own green hosted Verify run. Physical iOS
and low-end Android frame pacing, texture/memory/thermal soak, VoiceOver and
TalkBack, Windows runtime behavior, distributable signing/archives, complete
cross-platform package publication and a published binary compatibility
baseline remain open. See [release readiness](../../release-readiness.md).
