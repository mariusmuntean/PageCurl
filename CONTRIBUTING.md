# Contributing to PageCurl.Maui

PageCurl.Maui is a small, performance-sensitive control. Contributions should
keep the interaction model deterministic, the rendering path allocation-light,
and the public API understandable to a MAUI application developer.

## Development loop

1. Create a focused branch from `master` (for example,
   `feature/geometry-shadow`).
2. Make the smallest change that solves the problem.
3. Add or update a platform-independent test before changing behavior in the
   core project.
4. Run the core test runner and the builds for the target frameworks you can
   install locally.
5. Update the relevant concept in [`knowledge/`](knowledge/index.md) when a
   design decision, workaround, platform limitation, or release assumption
   changes.
6. Open a pull request. Keep it draft while device testing or visual review is
   still outstanding.

## Required checks

```bash
dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj
dotnet build src/PageCurl.Core/PageCurl.Core.csproj -c Release
dotnet build src/PageCurl.Maui/PageCurl.Maui.csproj -c Release
```

For a machine with the MAUI workloads installed, also build the demo for the
target platform under review. A successful source build is not a substitute for
profiling the control on a representative device.

## Design boundaries

- Keep input state, geometry, and rendering separate. The core project must not
  depend on MAUI, SkiaSharp, a dispatcher, or a device API.
- Treat `SKImage` instances supplied to the MAUI control as caller-owned.
  Document any new ownership rule before changing it.
- Avoid per-frame allocations and unbounded mesh growth. If a visual improvement
  needs more work, measure it and expose a deliberate option.
- Preserve unknown YAML frontmatter fields in OKF documents and use normal
  Markdown links between concepts.

## Review checklist

Reviewers should look for behavior at the edges of a page, cancellation and
re-entry during settling, multi-touch ownership, image disposal, allocations in
the paint loop, accessibility alternatives, and platform-specific build or
workload assumptions. See [`knowledge/project/testing.md`](knowledge/project/testing.md)
and [`knowledge/project/release-readiness.md`](knowledge/project/release-readiness.md)
for the current validation and release gates.
