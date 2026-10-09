# Screenshot review — 2026-10-09

Reviewed all seven tracked screenshots in their documentation context, at native
resolution and in a browser at reduced display sizes. The licensed video still
in the demo is a media asset, not a screenshot, and remains unchanged.

| Original asset | Decision and value |
| --- | --- |
| `docs/site/images/pagecurl-preview.jpg` | Keep: a visible diagonal curl previews the native demonstration video. |
| `docs/site/images/quick-start-curl-maccatalyst.png` | Replace: the isolated GL surface omitted navigation. The new [iPhone capture](../../site/images/quick-start-curl-ios.png) shows the documented sample's corner curl and Previous/Next buttons. |
| `docs/validation/showcase-polish-2026-10-05/sample.jpg` | Keep: dated evidence of the wide scene selector, page layout and scrollable Options panel. Clarified its resting-layout purpose. |
| `docs/validation/rider-and-interaction-2026-10-04/ios-light.png` | Keep: dated evidence of the compact light layout. |
| `docs/validation/rider-and-interaction-2026-10-04/ios-dark.png` | Keep: the matching dark layout and contrast. |
| `docs/validation/production-hardening-2026-10-06/ios-light.png` | Keep: compact layout and updated instructions on that build. |
| `docs/validation/production-hardening-2026-10-06/ios-dark.png` | Keep: the matching dark layout and contrast. |

Added a [reader capture](../../site/images/reader-curl-ios.png) beside the complete
five-page recipe. It shows the stationary left page, turning right sheet,
adjacent-page backside, destination page, counter and navigation. Rejected the
first portrait and nearly edge-on captures because they obscured those details.

## Capture and verification

- Extracted the quick-start and reader XAML/code-behind with
  `tools/extract-doc-example.py`; compared both with the published code blocks
  and confirmed exact source equality.
- Built a temporary native host with SDK 10.0.401, MAUI 10.0.110 and Xcode 27:
  explicit `net10.0-ios`, `iossimulator-arm64`, Release and Debug builds passed.
  Only the existing deprecated Xamarin settings warning occurred; no Xcode
  validation override was used.
- Final captures used the unchanged examples in the Debug host on iPhone 17e
  and iPad Pro 13-inch simulators running iOS 27. The host invoked the normal
  activation handlers and held a corner drag; it asserted `Dragging` before
  `simctl` captured the native compositor. This is rendering evidence, not
  certification of physical touch input.
- The iPhone PNG is unmodified. The iPad PNG was rotated into its displayed
  landscape orientation with `sips`; a pixel comparison confirmed that rotation
  preserved every captured pixel. No artwork, UI or text was composited.
- Historical iOS images initially looked blank in an inspection preview.
  Browser rendering confirmed their artwork; no app fix or new smoke guard was
  justified by that preview. Historical results retain their original dates.
- `PAGECURL_DOTNET=/Users/marius/.dotnet/dotnet bash tools/build-docs.sh`:
  zero warnings/errors; links, anchors and publication boundaries passed.
- Inspected the final rendered guides in the in-app browser: the quick-start
  image displays at 360 pixels wide and the reader at 768, both responsive.
- `python3 -m unittest discover -s tools/tests -p 'test_*.py'`: 61 tests passed.

The separate `tools/ios-smoke.sh` run completed Release build, installation,
launch and light/dark screenshots. Its HID stage failed because idb could not
find SimulatorKit. No simulator gesture pass is claimed.
