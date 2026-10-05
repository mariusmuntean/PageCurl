# Mac Catalyst interaction record

Records completed local runs on macOS 26.6 (ARM64) with the .NET 10.301
user-local SDK and MAUI workloads. The demo window is driven with synthetic
CG events through `tools/maccatalyst-smoke.sh`; screenshots are analyzed
programmatically (pixel diffs and Vision OCR).

## 2026-09-15: interaction and memory validation

Build: `dotnet build src/PageCurl.Demo/PageCurl.Demo.csproj -c Debug -f net10.0-maccatalyst`
(clean, 0 warnings, 0 errors).

Validated on the resizable desktop window:

- Spread turns in both directions via the outer-edge activation zones and
  the footer commands; mid-turn captures show the curl crossing the gutter.
- Mid-page grab: pressing inside the page leaves it flat (only text
  antialiasing differs); the curl grows from the grab point while dragging.
- Book-model back face: a late mid-turn capture of a forward spread turn
  OCRs as the adjacent page the sheet lands on, not a mirrored front.
- Snapshot churn: five alternating turn pairs keep resident memory flat
  (~350-365 MB) where the previous build grew about 85 MB per turn.
- Termination after repeated turns and drags exits cleanly (no crash
  reports after the fixed build).

Environment notes: `screencapture` requires Screen Recording permission
for the driving terminal; a crash during an earlier session corrupted the
saved window frame, which was cleared by deleting the
`de.marius.PageCurlDemo` defaults before relaunching.
