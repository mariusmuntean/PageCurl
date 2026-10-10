# Apricot branding validation — 2026-10-10

The selected paper monogram uses apricot, ivory and an orange curl. The shared
SVG supplies the README, documentation header/homepage/favicon, and demo app
icon/splash. Its PNG supplies both packages, package readmes and the demo header.
GitHub's repository social preview was uploaded and visually confirmed.

## Completed checks

Using SDK 10.0.401, MAUI 10.0.110 and Xcode 27.0:

- Release demo builds passed for `net10.0-android`, `net10.0-ios` with
  `iossimulator-arm64`, and `net10.0-maccatalyst` with `maccatalyst-arm64`.
  Builds used `-m:1 -nr:false`. Apple builds reported the existing deprecated
  Xamarin settings-file warning; no Xcode-version override was used.
- Compiled Apple manifests select `appicon`; Android selects the normal and
  round launcher icons. Generated icon images were visually inspected.
  See [asset checks](asset-check.log).
- `bash tools/build-docs.sh` passed the neutral build, DocFX with warnings as
  errors, and link/anchor/publication checks. Exported branding files match
  their source assets.
- Neutral Release packs of Core and MAUI passed `tools/check-packages.py`
  for `net10.0`; both contain the identical 512 by 512 PNG and sized README
  image. See [package checks](package-check.log).
- The Release Apple fixture built and `tools/run-native-fixture.py` completed
  with no failed assertions and `PASS all native suites`.
- The freshly built Mac demo was launched, its header logo was visually checked,
  and the task-started app was closed. See [demo header](demo-header.jpg).
- See [GitHub social preview](github-social-preview.jpg) for the saved setting.

## Diagnostic notes and limits

Overlapping neutral/platform restores initially replaced shared NuGet assets;
sequential restores/builds resolved NETSDK1005/NETSDK1047 and neutral packing
metadata mismatches. The existing Apple asset-catalog cache was invalidated
when selecting the newly introduced icon set, then the normal build regenerated
and packaged its metadata. No source workaround was used for restore failures.

Concurrent library and test edits were preserved outside the branding change.
These checks cover branding integration, not new runtime/device certification.
Windows was not built locally. Existing NuGet.org versions retain their original
embedded icons; publishing a new version is a separate release step. Historical
screenshots and motion recordings remain evidence of their original runs.
