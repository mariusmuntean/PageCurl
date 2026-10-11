# PageCurl 0.1.1 publication — 11 October 2026

[PageCurl 0.1.1](https://github.com/mariusmuntean/PageCurl/releases/tag/v0.1.1)
publishes the constructor-setting, spread-readiness and responsive-layout fixes
from the [holistic review](../holistic-review-2026-10-10/README.md).
The release tag and packages identify source commit
[`598aa10`](https://github.com/mariusmuntean/PageCurl/commit/598aa10b0275c0adb3b02ed26fb8226a3929c92d).
Core/MAUI packages and symbols are attached to the GitHub release.
Both [Core 0.1.1](https://www.nuget.org/packages/PageCurl.Core/0.1.1) and
[MAUI 0.1.1](https://www.nuget.org/packages/PageCurl.Maui/0.1.1) are published on
NuGet.org, together with their symbols. The updated documentation and preview
are live.

## Completed evidence

- [Verify 38127006005](https://github.com/mariusmuntean/PageCurl/actions/runs/38127006005)
  passed all four jobs: Core/tooling/examples, mobile, Windows and distribution.
- Hosted native fixture: 122 PASS outcomes and `PASS all native suites`.
- Android, iOS simulator, Mac Catalyst and Windows Release demo builds passed.
- Windows fresh-cache package consumer passed, including transitive Core,
  SkiaSharp registration and nonempty WinUI PRI output.
- Complete Core/MAUI packages and symbols assembled and passed package checks
  and an isolated external consumer. MAUI contains neutral, Android, iOS,
  Mac Catalyst and Windows assets.
- Microsoft.DotNet.ApiCompat.Tool 10.0.401 compared both complete 0.1.1 packages
  against fresh downloads of their published 0.1.0 baselines. Both passed
  backward API compatibility, including parameter-name checks. The additional
  strict equality check reported only the expected assembly-version increase
  from 0.1.0.0 to 0.1.1.0; no API suppressions were applied.
- Fresh local Core and neutral MAUI runs passed 12 and 5 test groups.
- Documentation built without warnings/errors. [Pages deployment](https://github.com/mariusmuntean/PageCurl-docs/actions/runs/38127663612)
  completed; live HTML confirms current release status and preview references.

An external project outside the repository restored using an empty cache,
no project references and NuGet.org as its only configured source, then passed
its Release command/mesh checks. Restored metadata confirms the remote source
for Core and MAUI. All four downloaded package/symbol ZIP payloads match the
reviewed distribution, excluding NuGet's added repository signature.
Physical-device performance, full VoiceOver/TalkBack journeys, Windows runtime
and iOS automated HID coverage remain open. This release does not certify
signed demo apps or store packaging.
