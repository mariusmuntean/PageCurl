# Microsoft guidance readiness — 7 October 2026

Validated the working tree based on `ea0ea61` with SDK 10.0.401, workload set
10.0.401.1, MAUI 10.0.110 and supported Xcode 27.0. No Xcode version override
was used. This is alignment with Microsoft's published guidance, not a
certification or completion of the hardware release gates.

## Changes and evidence

Product builds pin .NET 10 analyzer rules and fail compiler/analyzer warnings.
Core uses `recommended`; MAUI and the demo use the build-oriented `minimum`
mode plus recommended globalization rules. The existing public `pointer`
parameter names retain named-argument compatibility through a localized naming
exception. Framework lifecycle ownership remains unchanged. See Microsoft's
[analysis modes](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview).

SDK [package validation](https://learn.microsoft.com/en-us/dotnet/fundamentals/apicompat/package-validation/overview)
now checks the packaged assemblies, with strict compatible-framework and
parameter-name checks for MAUI. It initially failed because Android generated
a public `PageCurl.Maui.Resource` absent from the other targets. Setting the
documented [resource class modifier](https://learn.microsoft.com/en-us/dotnet/android/building-apps/build-properties#androidresourcedesignerclassmodifier)
to `internal` fixed that accidental public API. The four-target pack then passed
APICompat with zero warnings/errors, and the Android Release demo still built.

Threshold setters retain the last valid setting when rejecting invalid input.
The alias delegates to the same setter, and frame-time validation no longer
rechecks settings that setters already validate. A deliberate mutation that
assigned before throwing failed the retained-value assertion; the restored
implementation passed all Core groups, including warmed allocation guards.

The native fixture now uses Release and the demo's XAML source generation,
following Microsoft's [profiling guidance](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/profiling?view=net-maui-10.0).
All 110 PASS outcomes completed with exit 0 and the final suite marker. Existing
timing limits remain 150 ms for queue/handler and 250 ms for dispatch/first paint.
Maxima in this run were 75.7 ms, 27.4 ms, 95.2 ms and 94.3 ms respectively.
Native pixels/resolution, capture coalescing, window/video lifecycle, and all
four scenes' navigation in both layouts passed.

## Completed checks

| Check | Result |
| --- | --- |
| Core Release runner | All 12 groups |
| Neutral MAUI Release runner | All 5 groups |
| Core and four-target MAUI packages | SDK APICompat, packages and symbols produced; MAUI assets checked |
| Isolated consumer of those packages | Fresh restore, transitive Core, public commands and geometry passed |
| Android Release demo | Zero warnings/errors |
| iOS simulator and Mac Catalyst Release demos | Built; one local SDK settings deprecation warning each |
| Mac Catalyst Release native fixture | 110 PASS, zero FAIL; no timing limits relaxed |
| Python tooling / JavaScript replay tests | 59 / 5 passed in the working tree, including pending preview checks |
| Workflow references and pinned actionlint | Passed |
| Documented quick start | Unchanged XAML/code-behind compile; zero warnings/errors |
| Documentation site | DocFX zero warnings/errors; links, anchors and publication boundaries passed |

Local logs are retained under `.artifacts/microsoft-*`. Findings from the
enforced rules were resolved without renaming public parameters or replacing
interface-based tests. A broader recommended UI audit also flags handler-owned
resources under CA1001; native teardown coverage protects that lifetime without
adding a public disposal API.

## Remaining gates

The [7 October Verify run](https://github.com/mariusmuntean/PageCurl/actions/runs/37581805987)
did not start its jobs because of account billing/spending restrictions;
Documentation was blocked too. Windows and complete hosted distribution have
not been revalidated for these changes. A published API baseline, physical
devices, screen readers, signing and NuGet.org consumption remain separate
[release gates](../../release-readiness.md). Visual capture remains pending while
the Mac is locked. Preview links are withheld until a valid recording exists.
