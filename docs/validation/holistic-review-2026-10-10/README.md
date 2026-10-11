# Library, demo and documentation review — 10–11 October 2026

This local review follows behavior through the controls, native demo, examples
and presentation assets. It preserves the public API and the existing branding
work. Completed release evidence remains historical evidence for the published
0.1.0 packages, rather than certification of these local changes.

## Defects and regressions

- The `PageTurnView` constructor could overwrite supplied flick settings while
  initializing the threshold. It now preserves all three initial interaction
  settings while retaining the shared options instance. A neutral regression
  checks nondefault settings and later property updates.
- A hidden spread could accept a turn after explicit snapshot refresh, including
  from a visibility callback. Hiding now directly rejects commands; showing
  recomputes readiness even when snapshots are already current. Both directions,
  the visibility callback and reveal without another capture have regressions.
  Layout, size and load invalidation now also recompute availability immediately;
  command preparation checks current readiness rather than cached bindable flags.
  Regressions cover both directions and commands attempted from readiness callbacks.
- The demo retained explicit wrapper sizes from an earlier stage layout. A live
  page could then overflow stage padding. Grid layout now determines wrapper
  bounds. Separately, page-frame requests changed during parent arrangement could
  fail to update native content. The wrapper fits and remeasures its page during
  arrangement. Native shrink/expand checks protect padding, aspect ratio,
  managed/native dimensions, counter/action bounds and captured action pixels.
- Visual inspection of a newly recorded preview exposed the same lost-layout
  problem in the stationary spread. Both sheets now fit and remeasure during
  arrangement. Native regressions protect the combined aspect ratio, padding,
  sheet dimensions and UIKit bounds through wide/compact/wide constraints.
  The preview sequence also requires fitted spread geometry before and after
  its turn, preventing a snapshot-ready but malformed spread from publication.
- Both wrappers fit within inherited Grid padding. Asymmetric padding regressions
  reproduced an oversized native page before fitting was corrected to use the
  available interior bounds.

Each behavior fix reproduced a failure before the implementation changed.
The runtime contracts, requirements coverage and reader command guidance now
describe the same behavior as the controls. Package status points to the completed
public-release record and retains compatibility checks for later releases.
The README and public homepage distinguish these source fixes from the published
0.1.0 packages; the publishing guide carries that version-boundary rule forward.

## Completed local checks

| Check | Result |
| --- | --- |
| Core Release runner | All 12 groups, including allocation guards |
| Neutral MAUI Release runner | All 5 groups; zero build warnings/errors |
| Native Mac Catalyst Release assertion host | Two final runs each passed all 122 outcomes, exited 0 and included the final suite marker |
| Demo Mac Catalyst Debug | Final controls built and launched; real forward/backward spread drags and live fullscreen/input checked |
| Demo Android Release | Final controls built with zero warnings/errors |
| Demo iOS simulator Release | Final controls built for `iossimulator-arm64`; supported Xcode version check enabled |
| Fresh local packages and isolated consumer | Neutral packages/symbols checked; transitive Core, commands and mesh passed |
| Documented examples | Unchanged quick-start and reader XAML/code-behind compile |
| Python tooling and computer-control driver tests | 63 Python tests and 5 Node tests passed |
| Native motion preview | Recording and media checks passed; active-curl poster and wide spread/live/counter/end frames inspected |
| Documentation site | DocFX zero warnings/errors; local links, anchors and publication boundaries passed |

The refreshed movie and poster use the current demo copy and apricot monogram.
The silent H.264 movie is 1280×740 at 30 fps, 20.07 seconds, about 437 KB.
Both stationary spread sheets now visibly retain their selected proportions;
the live counter, completed turn and interrupted scene transition agree with
the recorded state assertions. This presentation asset does not certify pointer
routing or physical-device performance.

## Environment and evidence limits

Validation uses the pinned .NET SDK 10.0.401 and supported Xcode 27.0, with its
version check enabled. Premature MSBuild child-node exits were classified as
toolchain failures; serial native builds completed. Apple builds report the
local deprecated Xamarin preferences-file warning.

Two earlier native runs reached the accessibility traversal depth bound at 32
entries, rather than timing out. Partial-trail and retained-element identity
diagnostics were added without relaxing any bounds or page-label assertions.
Both final runs traversed the reader tree successfully, reaching depth four
with no repeated identities. The earlier failures remain unexplained; they are
not evidence of a proven native cycle or a cycle fix.

Desktop computer control exercised all four scenes, navigation boundaries,
live-counter updates and accessible control names. After rebuilding, a live page
returned from fullscreen with correct padding and remained interactive. Native
compact layout checks use constrained root sizes, not synthesized OS window
resizing. The final rebuilt demo also completed real forward and backward spread
drags across the spine, restored navigation readiness and retained the selected
3:4 sheet proportions.

The iOS smoke build and light/dark startup captures completed. Its interaction
phase failed because `idb` requires a SimulatorKit framework absent from this
Xcode installation. Three additional dark/light/dark launches captured complete
page content at both three and ten seconds; an apparently blank tool preview was
checked against the actual PNG pixels. No renderer change was justified by that
preview. iOS gestures, physical-device behavior, Windows runtime and screen-reader
usability remain unverified here. No hosted CI, package publication or website
deployment was performed for these changes.

Local raw evidence is retained under `.artifacts/review-*`,
`.artifacts/resize-*` and `.artifacts/renderer-idle-2026-10-10/`.
