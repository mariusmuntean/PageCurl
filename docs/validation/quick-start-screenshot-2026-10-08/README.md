# Quick-start screenshot — 8 October 2026

Added the initial Mac Catalyst rendering beside the quick-start introduction.
The XAML and code-behind were extracted unchanged from
`docs/site/getting-started.md` using `tools/extract-doc-example.py` and compiled
in a temporary native host with SkiaSharp registration. The host selected the
light system theme and a compact window; the documented page was not restyled.

Completed evidence:

- Release `net10.0-maccatalyst`, `maccatalyst-arm64` native build passed without
  an Xcode override. The host reported its existing Xamarin settings deprecation
  warning; there were no compilation errors.
- Native execution exited 0 after checking that Next was enabled, Previous was
  disabled, layout was nonzero, and the page snapshot could be refreshed.
- UIKit captured the actual native window content at 1200 by 1302 pixels.
  Visual inspection confirmed readable page text and both navigation commands.
  The PNG was copied without image editing to
  `docs/site/images/quick-start-maccatalyst.png`.

- `tools/test-quick-start.sh` passed for both unchanged documented examples,
  with zero warnings and errors.
- `tools/build-docs.sh` passed with zero DocFX warnings and errors, including
  local links, anchors and publication boundaries.

Build and execution logs remain under `.artifacts/quick-start-capture-*.log`.
This is screenshot evidence for the initial documented page, not a new
certification of gestures, iOS, screen readers or hardware performance.
