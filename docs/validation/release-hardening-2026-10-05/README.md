# Release hardening — 2026-10-05

Completed local checks:

- Core: all 12 deterministic test groups passed.
- Neutral MAUI: all 5 test groups passed.
- Validation tooling: 20 Python and 5 JavaScript tests passed.
- Isolated package consumer: Core and MAUI restored from a fresh local feed,
  compiled representative APIs and executed the consumer assertions.
- Native package inspection: neutral .NET, Android, iOS and Mac Catalyst
  assemblies, XML documentation and transitive Core dependencies passed.
- DocFX: zero warnings/errors; local link, anchor and publication checks passed.
- Obsolete-output probe: a removed HTML guide and API YAML file did not survive
  the next documentation build.
- Browser: public navigation/search opened API results; desktop (1280×900) and
  phone (390×844) layouts were inspected. Phone content width matched viewport
  width. The logo and navigation labels were corrected after visual review.
- Shell syntax, workflow project references and whitespace checks passed.

The [hosted documentation build](https://github.com/mariusmuntean/PageCurl/actions/runs/37336569201)
and [Pages deployment](https://github.com/mariusmuntean/PageCurl-docs/actions/runs/37336928689)
completed successfully. The site is available at
<https://mariusmuntean.github.io/PageCurl-docs/>.

The first documentation CI run failed because the generated-input directory's
parent did not exist in a clean checkout; creating parent directories fixed it.
The private source repository cannot host Pages on the current account plan.
The public documentation repository contains curated static docs and API
reference, with publication through the documented script.

These checks do not certify hardware performance, Windows runtime rendering,
VoiceOver/TalkBack, signing, store packages or published NuGet consumption.
See [release gates](../../release-readiness.md).

## Framework override regression

The first Windows build completed with `NETSDK1005`: a global `TargetFrameworks`
override restored Core for Windows while Core still builds `net10.0`. The same
failure was reproduced locally using an Android-only library restore. Restoring
Core separately for `net10.0`, then building and packing Android with
`--no-restore`, passed with zero warnings/errors and valid Android package assets.
The Windows workflow uses this same sequence, with matching restore/build
packaging properties. Only a completed hosted Windows run establishes its pass.

Workflow syntax validation reproduced the invalid job-level `runner.temp`
expression and passed after the SDK installation path moved to the setup step.
Pinned `actionlint` now runs in Verify, with the custom Xcode runner label
configured explicitly. The Windows job uses a fresh SDK directory and a
30-minute limit to avoid upgrading unrelated preinstalled workloads.

The next Windows build passed framework restore and failed with `NETSDK1112`
because its implicit `win-x64` runtime pack was not restored. The workflow now
uses explicit `win-x64` for demo restore/build and the separate Core restore.
The isolated SDK installed the pinned Windows workload in 25 seconds, compared
with roughly seven minutes when upgrading workloads inherited from the image.

Specifying only the runtime did not close `NETSDK1112`. A small SDK-only probe
reproduced the same error when restore was framework-dependent and build was
self-contained. Restoring and building with matching self-contained settings
passed with zero warnings/errors. Windows restore and build now explicitly use
Release, `win-x64` and `SelfContained=true` for the unpackaged demo.

The subsequent Windows run compiled both projects, then failed with `PRI252`:
WinUI searched the library's runtime-specific directory while the SDK built the
library without a runtime suffix. The Windows library now declares
`IsRidAgnostic=false` so its consuming application's runtime identifier flows
through project references. A neutral Release library build still passed with
zero warnings/errors; native resource indexing requires the hosted Windows check.
