# Quick-start screenshot — 8 October 2026

The quick start now shows an actual curl instead of an almost empty resting
page. The first published image did not demonstrate the library and was replaced.
Documentation publishing guidance now requires a visible curl from the actual
sample and inspection at the displayed size.

The XAML and code-behind were extracted unchanged from
`docs/site/getting-started.md` using `tools/extract-doc-example.py` and compiled
in a temporary native host with SkiaSharp registration. The host selected the
light system theme and a compact window; the documented page was not restyled.

Completed native evidence:

- Release `net10.0-maccatalyst`, `maccatalyst-arm64` build passed without an
  Xcode override. The host reported its existing Xamarin settings deprecation
  warning; there were no compilation errors.
- Execution checked initial navigation, refreshed the native page snapshot and
  invoked the normal activation-zone touch handler for a held lower-right drag.
  The renderer had to be visible and its controller had to be in Dragging state.
- Captured the completed native GL paint surface at 1136 by 1046 pixels. The
  Mac was locked, preventing an OS window screenshot; the image therefore shows
  the actual page-turn surface rather than window chrome or navigation controls.
  The host exited 0 and was removed after capture.
- Visual inspection confirmed a curved sheet, visible shaded backside and
  readable page text. The unedited PNG is
  `docs/site/images/quick-start-curl-maccatalyst.png` (52,346 bytes); a white
  display background preserves the light app background around its alpha edges.

- Both unchanged documented examples compiled successfully, with zero warnings
  and errors, using `tools/test-quick-start.sh`.
- `tools/build-docs.sh` passed with zero DocFX warnings and errors and valid
  local links, anchors and publication boundaries.
- A background browser preview confirmed that the image loaded at 480 by
  approximately 442 CSS pixels. Inspection in the rendered guide confirmed the
  visible curl and readable label at that size. The temporary tab and server
  were closed afterward.

Native logs remain under `.artifacts/quick-start-curl-*.log`.
This is rendering evidence, not certification of OS pointer routing, iOS,
screen readers or hardware performance.
