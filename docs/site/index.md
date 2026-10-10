# PageCurl.Maui

PageCurl.Maui adds page turns to .NET MAUI applications. Use it for images,
live views or a two-page reader. Pages curl from an edge or pinned corner;
buttons and gestures share the same completion event.

## Motion preview

<video controls preload="none" playsinline width="1280" style="max-width:100%;height:auto" poster="images/pagecurl-preview.jpg" aria-label="PageCurl native demo: drag and snap-back, a spread turn, live content and a scene switch">
  <source src="images/pagecurl-preview.mp4" type="video/mp4">
  <a href="images/pagecurl-preview.mp4">Watch the PageCurl native demo.</a>
</video>

The 20-second Mac Catalyst recording shows a committed drag and snap-back,
a spread turn, a live counter update, and switching scenes during a turn.
Playback runs at normal speed. [Download the preview](images/pagecurl-preview.mp4).

## Choose a control

| Control | Use it for | Content ownership |
| --- | --- | --- |
| `PageTurnView` | Images you already render | You own the `SKImage` values |
| `PageCurlView` | A live MAUI page | The control owns captured textures |
| `PageSpreadView` | Two-page navigation | The control captures up to six views |

Start with the [quick start](getting-started.md), then read the
[runtime rules](runtime.md) before connecting a page model.
The [small reader example](spreads.md#small-reader-example) supplies a complete
five-page recipe with navigation, capture retry and the odd last page.

## Current release status

Version 0.1.0 targets .NET 10 and MAUI 10; NuGet publication is pending.
Public APIs may change before 1.0.
Use the [platform matrix](platforms.md) to distinguish build coverage from
runtime and hardware validation. See the [quick start](getting-started.md)
for package installation and a working example.

The sample includes Single, Spread, Content and Backside scenes, a live counter,
local video, anchor choices, reduced motion and page-turn commands.

Browse the [source](https://github.com/mariusmuntean/PageCurl) or
[report an issue](https://github.com/mariusmuntean/PageCurl/issues).
See [contributing and validation](contributing.md) to build and test locally.
[MIT license](https://github.com/mariusmuntean/PageCurl-docs/blob/main/LICENSE)
