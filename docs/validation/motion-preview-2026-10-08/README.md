# Motion preview — 8 October 2026

Added a 20-second, silent preview and active-curl poster to the README and public
documentation source. The clip records the real linked demo in the Release native
host at normal speed: committed drag, snap-back, spread navigation, live counter
update, and scene switching during a turn. State assertions protect that sequence.
No production test hooks or library API changes were added.

The macOS recorder selects only its own process's window. Handshakes bound the
sequence and keep the window alive until recording finishes. Cleanup closes the
task-started app and removes temporary files. Duration, decoded frame diversity,
H.264 video and absence of audio are checked before replacing the web assets.
These checks reject incomplete/frozen capture; visual review remains separate.

## Completed evidence

- Release capture completed after unlock; both native and encoded media checks
  passed. The final web clip is 1280 × 740, 30 fps, 20.03 seconds, approximately
  405 KB, with no audio. The poster is approximately 57 KB.
- Inspected active curl frames and the full timeline's scene states. Adjusted
  pacing to reduce idle time at the end. Pages, undersides, stationary content,
  counter updates and scene transitions remain readable and consistent.
- The ordinary Release native fixture completed with 110 PASS outcomes, zero
  FAIL and its final suite marker. Recording mode does not replace those checks.
- Swift typechecking with warnings as errors and shell syntax checks passed.
- All 59 Python tooling tests passed, including encoded moving/frozen/partial/
  audio/corrupt media fixtures and missing poster/video link regressions.
- DocFX built with zero warnings/errors; local links, anchors and publication
  boundaries passed with the real movie and poster included.

Logs and inspection frames are under `.artifacts/preview-*` and
`.artifacts/motion-preview/`. Earlier locked-screen captures were rejected;
only the verified unlocked recording is included in documentation.

This presentation asset does not certify real pointer routing, physical-device
performance or screen-reader behavior. Those remain separate release gates.
