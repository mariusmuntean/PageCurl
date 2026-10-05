# Support policy

PageCurl.Maui is maintained as a focused sample-quality control intended for
reading and document interfaces. The 0.x release line may introduce breaking
API changes while the public contract settles.

The supported baseline is .NET 10, .NET MAUI 10, and
SkiaSharp.Views.Maui.Controls 4.150.1. The minimum platform targets are
Android API 24, iOS/Catalyst 17 and Windows 10.0.17763. See [`docs/compatibility.md`](docs/compatibility.md)
for the validation matrix and evidence status.

When reporting an issue, include the package version, target framework,
operating-system version, device or simulator model, reproduction steps, and
the complete build or runtime log. Include a small image or sample project for
visual or image-lifetime problems when possible.

Physical-device profiling remains a consumer release gate. A simulator build
or the deterministic core allocation guard does not establish frame pacing,
GPU behavior, thermal behavior, or texture limits on physical hardware.
