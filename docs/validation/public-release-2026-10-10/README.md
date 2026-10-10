# Public release validation — 10 October 2026

The hosted [Verify run 38048455583](https://github.com/mariusmuntean/PageCurl/actions/runs/38048455583)
passed all four jobs for
[`9e759e0a9587dff327c214f0d6d122fcff40ab53`](https://github.com/mariusmuntean/PageCurl/commit/9e759e0a9587dff327c214f0d6d122fcff40ab53).
This replaces the hosted-build and distribution blocker recorded on 7 October.

| Completed check | Result |
| --- | --- |
| Core and neutral MAUI | Release tests, warmed allocation guards, API/lifecycle tests and neutral build pass |
| Documentation and validation tools | Workflow checks, tooling/replay tests and unchanged quick-start/reader example compilation pass |
| Apple native fixture | 119 PASS outcomes, including the final suite marker; no failures. Covers snapshots, interaction, every scene in both navigation layouts, lifecycle and reader accessibility |
| Platform builds | Android, iOS simulator, Mac Catalyst and Windows Release demo builds pass |
| External Windows package consumer | Fresh-cache restore and build pass with transitive Core, SkiaSharp registration and nonempty WinUI PRI output |
| Complete distribution | Core/MAUI packages and symbols assemble from macOS/Windows inputs; package inspection and an isolated consumer of the assembled feed pass |

The MAUI distribution contains all five frameworks: `net10.0`,
`net10.0-android36.0`, `net10.0-ios26.0`, `net10.0-maccatalyst26.0` and
`net10.0-windows10.0.19041`. Core contains `net10.0`. Verify retains
`native-assertions`, `packages-mobile`, `packages-windows` and
`package-distribution` artifacts, including both `.nupkg` and `.snupkg` outputs.
Package checks require framework assembly/XML assets and the transitive Core
dependency; the distribution assembler checks matching package/symbol inputs.

The [source and issue tracker](https://github.com/mariusmuntean/PageCurl) are
public, and [private vulnerability reporting](../../../SECURITY.md) is available.
Both [Core 0.1.0](https://www.nuget.org/packages/PageCurl.Core/0.1.0) and
[MAUI 0.1.0](https://www.nuget.org/packages/PageCurl.Maui/0.1.0) are published.
Both symbol archives are downloadable. An external PackageReference consumer
restored with an empty package cache, no project references, and NuGet.org as
its only configured source, then passed its Release command/mesh checks.
Restored package metadata confirms the remote source. All package and symbol
ZIP payloads match the reviewed distribution, excluding NuGet's added repository
signature, and retain the source commit above. The earlier isolated hosted
consumers verified the assembled feeds separately.

Physical Android/iOS performance, minimum-OS coverage, long-run texture/memory
behavior, VoiceOver/TalkBack journeys and Windows runtime validation remain
open. The iOS simulator build does not establish HID gesture interaction.
The native accessibility checks do not certify a complete screen-reader journey.
Signed demo apps, archives and store packages need their own release validation;
the completed unsigned builds and library distribution do not establish it.

See [compatibility](../../compatibility.md) and
[release readiness](../../release-readiness.md) for the remaining gates.
