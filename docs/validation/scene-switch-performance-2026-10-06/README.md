# Rapid scene switching — 6 October 2026

The reported lag reproduces with automated native input timing. The previous
single Single-to-Spread check missed the worst case: switching away from
Backside 80 ms into a turn delayed the next UI dispatch by 319.4 ms, including
four unnecessary captures taking 276.6 ms.

## Changes and regression evidence

- A live-page refresh now records its content revision before waiting for an
  interaction. Hiding, replacing or reconnecting the content invalidates that
  request before it can capture the outgoing page. The
  [neutral regression failed before the fix](neutral-red.log); it now covers
  hiding and immediate hide/re-entry.
- Idle cancellation no longer invalidates the surface. A native test observed
  a redundant paint before the fix and requires zero afterwards.
- Backside reuses its two page views while the navigation index is unchanged,
  including scene re-entry and presentation settings. Navigation and resource
  recreation still replace them. The native reference-identity regression
  [failed before this change](native-before.log).
- Automatic refreshes coalesce invalidations for 16 ms after yielding, allowing
  scene presentation and input before synchronous native rasterization. The
  [ordering regression failed in 12 of 16 cases](scheduling-red.log) before
  this change. Explicit refresh has no added scheduling delay.

The Apple snapshot method still includes recent screen changes, retains native
pixel density and uses the same sRGB/premultiplied-alpha conversion. No mesh
resolution, texture sampling, material constraint, allocation assertion or
existing acceptance threshold changed. Apple documents that disabling
[afterScreenUpdates](https://developer.apple.com/documentation/uikit/uiview/drawhierarchy(in:afterscreenupdates:))
can omit recent changes; that shortcut was not used.

## Native measurement

The fixture switches Single → Spread → Content → Backside → Single with four
timings: in the same event-loop turn as starting an animated command, 16 ms
later, 80 ms later, and immediately after completion. Every outgoing controller
must be idle after selection. Capture count/time and handler/next-dispatch
latency are recorded separately. The existing five-rendered-frame gesture
release/switch check remains, with its original 150/250 ms limits.

| Run | Mean next dispatch | Range across 16 switches | Result |
| --- | ---: | ---: | --- |
| [Before fixes](native-before.log) | 164.5 ms | 37.1–319.4 ms | Targeted regressions fail |
| [Revision, cancellation and view reuse](native-first-iteration.log) | 117.7 ms | 15.4–205.6 ms | All native suites pass |
| [Automatic capture scheduling](native-second-iteration.log) | 34.8 ms | 15.1–83.4 ms | All suites and 16 ordering checks pass |
| [Final repeat](native-final.log) | 32.1 ms | 13.6–89.3 ms | All 79 native assertions pass |

These are local Mac Catalyst Debug samples at a 1728 by 1000 window, not a
hardware certification or a guarantee of frame rate. Scheduling improves the
response before capture; it does not make the underlying native rasterization
free. Cold snapshot readiness still depends on layout and capture.

## Test integrity

The faster sequence exposed a fixture snapshotter returning the same owned
image to multiple concurrent requests. One request could dispose another's
image and crash the native host. The suspended provider now suspends only its
first request and creates independently owned images for subsequent captures.
This was a test-provider defect, not evidence of a production image-lifetime
defect. Stale-result and failure assertions remain intact.

The later reduced-motion journey now waits for the requested Next button after
the benchmark advances navigation, rather than assuming capture completes
within 300 ms. The live-view probe likewise awaits attachment and layout before
explicit capture, as required by the snapshot contract. Full navigation,
endpoints, animations, layout, color/alpha, cancellation and failure checks remain.

## Completed validation

Environment: .NET SDK 10.0.401, workload set 10.0.401.1, MAUI 10.0.110,
SkiaSharp 4.150.1 and Xcode 27.0. Apple version checking stayed enabled.
Commands follow the [repository validation skill](../../../.agents/skills/pagecurl-maui-validation/SKILL.md).

- [Core Release: 12 groups passed](core-tests.log), including warmed allocation guards.
- [Neutral MAUI Release: 5 groups passed](maui-tests.log).
- Native Mac Catalyst: both implementation iterations passed the complete
  bounded fixture, including all scenes and both layouts. The final repeat
  passed all 79 assertions. The [native build passed](native-build.log), with
  the existing deprecated Xamarin Settings.plist warning and no source errors.
- [Android Release demo build passed](android-build.log), zero warnings/errors.
- [Mac Catalyst Release demo build passed](maccatalyst-build.log), no source
  errors and the same deprecated Settings.plist warning.
- [Documentation build and link/anchor/publication checks passed](docs-build.log),
  zero warnings/errors.

No computer control was needed for this investigation. The actual native demo
and linked event handlers ran in the assertion host at controlled timing.
Physical-device performance, Windows runtime and hosted CI for the current
changes remain unverified.
