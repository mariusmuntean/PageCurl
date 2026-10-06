# PageCurl.Maui

PageCurl.Maui adds page turns to .NET MAUI applications. Use it for images,
live views or a two-page reader. Pages curl from an edge or pinned corner;
buttons and gestures share the same completion event.

## Choose a control

| Control | Use it for | Content ownership |
| --- | --- | --- |
| `PageTurnView` | Images you already render | You own the `SKImage` values |
| `PageCurlView` | A live MAUI page | The control owns captured textures |
| `PageSpreadView` | Two-page navigation | The control captures up to six views |

Start with the [quick start](getting-started.md), then read the
[runtime rules](runtime.md) before connecting a page model.

## Current release status

Package metadata currently uses 0.1.0 and targets .NET 10 and MAUI 10.
Public APIs may change before 1.0.
Use the [platform matrix](platforms.md) to distinguish build coverage from
runtime and hardware validation. Packages have a local consumption test;
this guide does not assume that a package has been published on NuGet.org.

The sample includes Single, Spread, Content and Backside scenes, a live counter,
local video, anchor choices, reduced motion and page-turn commands.

The source repository and its issue tracker currently require access. See
[contributing and validation](contributing.md) if you have repository access.
[MIT license](https://github.com/mariusmuntean/PageCurl-docs/blob/main/LICENSE)
