# PageCurl.Maui repository guidance

## Before editing

- Read `README.md` and the relevant concept under `knowledge/project/`.
- Preserve the boundary between `PageCurl.Core`, `PageCurl.Maui`, and the demo.
- For build failures, obtain completed command output and classify the first actual error before editing source.
- Use explicit target frameworks for platform builds.

## Design boundaries

- Keep geometry and interaction code free of MAUI, SkiaSharp, dispatcher, and device dependencies.
- Avoid per-frame allocations and unbounded mesh growth.
- Treat supplied `SKImage` values as caller-owned.
- Keep gesture and accessibility commands on the same page-completion path.
- Add deterministic regression coverage for behavior changes.

## Validation routing

- Use `$pagecurl-maui-validation` for PageCurl changes, build failures, CI changes, workload issues, rendering checks, and device validation.
- Run the dependency-free core test runner for geometry and interaction changes.
- Run the neutral MAUI build for adapter and packaging changes.
- Use explicit Android, iOS, or Mac Catalyst targets for platform changes.
- Use `tools/ios-smoke.sh` for iOS simulator evidence.

## Project knowledge

- Update the relevant `knowledge/project/` concept when architecture, workarounds, testing strategy, or release gates change.
- Do not duplicate the full validation command list in this file.
- Keep validation records limited to evidence from completed runs.
