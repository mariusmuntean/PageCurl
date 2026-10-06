# Public launch review — 6 October 2026

Earlier hardening and rapid-switch improvements were committed as `c52e0ac`.
The changes covered here are left unstaged and uncommitted for inspection.
This is local evidence; the revised Verify workflow has not run on a hosted
runner, and no repository, documentation or package publication was performed.

## Improvements and regressions

- Settling now uses monotonic elapsed time between dispatcher callbacks rather
  than an assumed 60 Hz step. A manually advanced clock reproduced a delayed
  callback advancing progress by only 0.037. The fix retains Core's 100 ms
  sample limit, resets the clock for a new loop and rejects obsolete timers.
  Coverage also completes two turns chained through the completion event.
- Stopping the demo window immediately rolls back active turns and pauses
  native video. Resume restores commands and only resumes the selected video
  scene. The first native probe failed because the turn remained settling;
  the corrected demo passes rollback, unchanged navigation and video checks.
  Window handlers detach when the page disappears.
  The fixture delivers MAUI window notifications inside the native host;
  physical-device background behavior still belongs to device validation.
- Spread capture failures identify physical sheet backs correctly:
  previous-right and next-left are backs; the other four slots are not.
  The regression failed with the old always-false event flag.
- The quick start includes complete, bounded navigation code. A new check
  compiles its unchanged XAML and code-behind in a temporary project, and Verify
  runs it. Package READMEs use absolute links and include platform/ownership
  guidance; both packages include the MIT license text and author metadata.
- Verify retains unsigned macOS/Windows packages and symbols, combines them
  into a five-framework distribution after the test jobs pass, and restores an
  isolated consumer from that feed. Assembly rejects mixed versions/revisions,
  missing symbols, conflicting assets/dependencies, signed inputs and malformed
  ZIP/OPC structure. It requires an empty output directory and exposes the
  validated directory together. Independent review reproduced stale-version
  output and permissive standalone ZIP checks; new red tests now pass.
  Shared README/license files are pinned to LF: a regression reproduced Windows
  checkout conversion changing package bytes, then passed with the attributes.
- Support/contribution guidance, issue forms, a PR checklist and private
  security-reporting instructions are ready for adoption. The source repository
  was confirmed private during this review; public access and an available
  private reporting channel remain launch actions.

## Completed checks

Environment: SDK 10.0.401, workload set 10.0.401.1, MAUI 10.0.110,
SkiaSharp 4.150.1, Xcode 27.0 with version checking enabled. Apple builds report
the existing deprecated local Xamarin settings warning; it is not a source
warning and no Xcode validation override was used.

| Check | Completed result | Evidence |
| --- | --- | --- |
| Core Release runner | All 12 groups pass, including warmed allocation guards | [Core](core-tests.log) |
| Neutral MAUI Release runner | All 5 groups pass, including elapsed-time timers and six failure roles | [MAUI](maui-tests.log), [timer red](animation-red.log), [backside red](backside-red.log) |
| Native Mac Catalyst fixture | Two final runs each report 84 PASS lines, including the final suite marker | [native](native-final.log), [repeat](native-repeat.log), [window red](window-red.log), [first window/video iteration](native-window-iteration.log) |
| Validation/package Python suite | 51 tests pass; review regressions reproduced first | [tools](tooling-tests.log), [review red](tooling-red.log), [line-ending red](line-endings-red.log) |
| UI replay runner tests | All 5 pass; no computer-control replay was needed for these changes | [replay](replay-tests.log) |
| Android Release demo | Build succeeds, 0 warnings/errors | [Android](android-build.log) |
| Mac Catalyst Release demo | Build succeeds, 0 errors and the local settings warning above | [Mac Catalyst](maccatalyst-build.log) |
| iOS Release simulator demo | Build succeeds, 0 errors and the same local settings warning | [iOS](ios-build.log) |
| Documented quick start | Unchanged XAML/code-behind compile with CI mode and workload resolution disabled; 0 warnings/errors | [example](quick-start-build.log) |
| Real host packages and symbols | Core plus all four macOS-built MAUI targets pass inspection; missing Windows prevents assembly and leaves no outputs | [pack](host-pack.log), [inspection](package-checks.log) |
| Isolated package consumer | Fresh restore from the real host feed; transitive Core and representative command/mesh calls pass | [consumer](package-consumer.log) |
| Workflow/shell checks | Workflow references, actionlint and both changed shell scripts pass | [references](workflow-references.log), [lint](workflow-lint.log) |
| Documentation site | Build and link/anchor/publication checks pass, 0 warnings/errors | [documentation](documentation-build.log) |
| Repository documentation | 74 local links and issue-form YAML pass validation | [source documentation](source-docs-check.log) |

The final native run exercised every scene in both layouts. Its 16 rapid-switch
probes dispatched in 13.2–74.7 ms (mean 34.8 ms); all dispatched before automatic
capture and cancelled their outgoing turn. These are one-machine measurements,
not a hardware performance guarantee. The existing snapshot resolution,
geometry, shading and ownership contracts remain covered by their tests.
The repeat passed the same suite; its 16 rapid dispatches took
13.0–70.3 ms (mean 30.4 ms).
An [initial native run](native-timing-failure.log) timed out during compact
Content animated navigation. The timer regression then isolated the assumed
frame-time defect; the full native journeys pass with elapsed-time advancement.

## Remaining launch gates

- A fresh hosted Verify pass, including Windows inputs and complete assembled
  distribution. Synthetic assembly tests and local host packages cannot replace
  a Windows build or platform consumer/runtime evidence.
- Representative physical Android/iOS frame pacing, memory, texture lifetime
  and long-run checks, plus VoiceOver/TalkBack and Windows runtime validation.
- iOS HID interaction still needs a working SimulatorKit/device path; an iOS
  simulator build does not prove gesture interaction.
- Review public source/security access, signing and package publication, then
  validate fresh NuGet.org consumption. Local development packages from this
  uncommitted checkout are test inputs, not release artifacts.

See [release readiness](../../release-readiness.md) for the full release gates.
