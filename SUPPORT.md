# Support policy

PageCurl.Maui provides controls for image, live-content and two-page spread
turns. The 0.x release line may introduce breaking API changes before 1.0.
Version 0.1.0 is available on NuGet.org. Use the
[quick start](docs/site/getting-started.md) for source setup and package installation.

The MAUI package includes `PageCurl.Core` transitively.

The target baseline is .NET 10, .NET MAUI 10, and
SkiaSharp.Views.Maui.Controls 4.150.1. The minimum platform targets are
Android API 24, iOS/Catalyst 17 and Windows 10.0.17763. See [`docs/compatibility.md`](docs/compatibility.md)
for build and runtime evidence. Minimum OS targets do not establish a
physical-device or screen-reader pass.

## Report a problem

Use [GitHub issues](https://github.com/mariusmuntean/PageCurl/issues) for bugs and
feature requests. Search existing issues and the
[runtime rules](docs/runtime-contracts.md) before opening a report.

When reporting an issue, include the package version, target framework,
operating-system version, device or simulator model, reproduction steps, and
expected versus actual behavior. For build failures, include the first actual
error and the completed command log. For visual or image-lifetime problems,
include a small sample and a screenshot or recording of the active turn.
Remove credentials, personal content and unrelated device identifiers from
attachments. Report suspected vulnerabilities through the
[security reporting policy](SECURITY.md).

Use [Contributing](CONTRIBUTING.md) to prepare a fix or documentation change.

## Before shipping

Physical-device profiling remains a consumer release gate. A simulator build
or the deterministic core allocation guard does not establish frame pacing,
GPU behavior, thermal behavior, or texture limits on physical hardware.
