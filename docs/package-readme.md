# PageCurl

![PageCurl icon](https://raw.githubusercontent.com/mariusmuntean/PageCurl/master/docs/site/images/pagecurl-icon.png)

Page turns for images, live .NET MAUI views and two-page spreads.
`PageCurl.Maui` targets .NET 10 / MAUI 10 and references `PageCurl.Core`
transitively. Core contains platform-independent interaction, navigation and
page geometry and can also be used on its own.

Install from your MAUI application's directory:

```bash
dotnet add package PageCurl.Maui --version 0.1.0
```

| Control | Content |
| --- | --- |
| `PageTurnView` | Caller-owned `SKImage` values |
| `PageCurlView` | A live MAUI view, captured during turns |
| `PageSpreadView` | A two-page reader with live neighboring content |

Register SkiaSharp in your MAUI app:

```csharp
using SkiaSharp.Views.Maui.Controls.Hosting;

builder.UseMauiApp<App>().UseSkiaSharp();
```

The [quick start](https://mariusmuntean.github.io/PageCurl-docs/getting-started.html)
shows XAML setup and bounded navigation. Start commands after layout and
snapshot capture; update the page model after committed completion. Supplied
`SKImage` values remain caller-owned. The
[runtime guide](https://mariusmuntean.github.io/PageCurl-docs/runtime.html)
covers refresh, UI-thread and lifecycle requirements, and the
[API reference](https://mariusmuntean.github.io/PageCurl-docs/api-index.html)
lists the public surface.

Minimum targets are Android API 24, iOS 17, Mac Catalyst 17 and Windows
10.0.17763. See the [compatibility matrix](https://github.com/mariusmuntean/PageCurl/blob/master/docs/compatibility.md)
for tested platform coverage. Physical-device performance, screen-reader
certification, Windows runtime validation and store signing remain
[release gates](https://github.com/mariusmuntean/PageCurl/blob/master/docs/release-readiness.md).
The 0.x API may change before 1.0.

[MIT license](https://github.com/mariusmuntean/PageCurl/blob/master/LICENSE) ·
[Support](https://github.com/mariusmuntean/PageCurl/blob/master/SUPPORT.md) ·
[Source and sample](https://github.com/mariusmuntean/PageCurl)
