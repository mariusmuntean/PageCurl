# NuGet README rendering correction — 11 October 2026

The 0.1.1 NuGet listings displayed the package README's HTML `img` tag as text.
NuGet.org supports Markdown and escapes raw HTML; embedded READMEs are immutable.
See [Microsoft's README guidance](https://learn.microsoft.com/en-us/nuget/nuget-org/package-readme-on-nuget-org).

Version 0.1.2 removes the redundant image tag. The package listing header retains
its embedded PNG icon. The package checker now rejects HTML image tags, with a
regression that failed before the guard and passes afterward. No library code,
public API or demo behavior changed from 0.1.1.

[The GitHub release](https://github.com/mariusmuntean/PageCurl/releases/tag/v0.1.2)
contains complete packages and symbols from
[`45d61e8`](https://github.com/mariusmuntean/PageCurl/commit/45d61e87c47b30d3a7e5009cee62dc7cde81edd3).
NuGet preview, upload and fresh remote consumption await renewed account sign-in.

## Completed evidence

- 65 Python tooling tests passed, including HTML rejection and Markdown acceptance.
- Local neutral Core/MAUI packages passed inspection and an isolated consumer.
- [Verify 38177441732](https://github.com/mariusmuntean/PageCurl/actions/runs/38177441732)
  passed Core, mobile, Windows and distribution jobs.
- Hosted native fixture passed 122 outcomes and `PASS all native suites`.
- Android, iOS simulator, Mac Catalyst and Windows Release demo builds passed.
- Complete distribution passed asset checks and isolated consumers, including
  Windows WinUI PRI generation.
- APICompat 10.0.401 found no breaking changes against fresh published 0.1.1
  baselines for Core and all five MAUI frameworks, including parameter names.
- Both actual release package READMEs contain no HTML image tag and reference 0.1.2.

Historical 0.1.0 and 0.1.1 listings retain their embedded READMEs. The correction
applies to 0.1.2 and the latest-version listing after indexing. Existing hardware,
screen-reader, iOS HID and Windows runtime validation limits remain unchanged.
