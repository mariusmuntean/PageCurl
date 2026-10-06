# Contributing to PageCurl.Maui

PageCurl.Maui is a small, performance-sensitive control. Contributions should
keep the interaction model deterministic, the rendering path allocation-light,
and the public API understandable to a MAUI application developer.

## Development loop

1. Read the [requirements](docs/requirements.md), relevant usage guide and
   [runtime rules](docs/runtime-contracts.md). For a larger API or behavior
   change, discuss the use case in an issue before implementation.
2. Create a focused branch from the repository's default branch (for example,
   `feature/geometry-shadow`).
3. Make the smallest change that solves the problem. Add a deterministic
   regression for changed behavior; keep core tests platform-independent.
4. Run the checks relevant to your change and record their completed results.
5. Update the relevant page under [`docs/`](docs/index.md) when behavior, a
   design decision, platform limitation, or release assumption changes.
6. Open a pull request with the problem, resulting behavior and validation.
   Name any affected checks you could not run. Keep it draft while required
   rendering or runtime review remains outstanding.

## Required checks

Use [`pagecurl-maui-validation`](.agents/skills/pagecurl-maui-validation/SKILL.md)
for command and target selection. Choose the smallest relevant path: core
behavior needs the dependency-free runner; adapter and public API changes need
neutral MAUI checks; platform changes need that platform's explicit target.
Rendering and snapshot changes also need runtime or visual evidence. For
documentation changes, build the site and check its links.

The repository uses the SDK and workload set selected by `global.json`. On
macOS, the validation skill routes iOS simulator work through
`tools/ios-smoke.sh`. Separate completed tests and builds from device,
screen-reader and store checks that remain unverified.

## Design boundaries

- Keep input state, geometry, and rendering separate. The core project must not
  depend on MAUI, SkiaSharp, a dispatcher, or a device API.
- Treat `SKImage` instances supplied to the MAUI control as caller-owned.
  Keep them alive until the view no longer uses them, clear image properties
  before disposal, and document any new ownership rule before changing it.
- Keep page-model commits in `PageTurnCompleted`; accessibility commands and
  gestures must share the same completion path.
- Avoid per-frame allocations and unbounded mesh growth. If a visual improvement
  changes cost, measure it before adding configuration.
- Keep durable documentation current and use relative Markdown links inside
  the repository.

## Review checklist

Reviewers should look for behavior at the edges of a page, cancellation and
re-entry during settling, multi-touch ownership, image disposal, allocations in
the paint loop, accessibility alternatives, reduced-motion behavior, theme and
safe-area layout, and platform-specific build or workload assumptions. See
[`docs/testing.md`](docs/testing.md),
[`docs/compatibility.md`](docs/compatibility.md), and
[`docs/release-readiness.md`](docs/release-readiness.md)
for the current validation and release gates.
