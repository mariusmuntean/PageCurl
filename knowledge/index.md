# PageCurl.Maui knowledge bundle

This directory records the reasoning behind the library as small, linked
Markdown concepts. It is intentionally project-specific: it describes the
implementation in this repository and does not copy the example knowledge
catalog that inspired the format.

## Concepts

- [Project overview](project/overview.md) — the user problem, scope, and
  non-goals.
- [Architecture](project/architecture.md) — the boundaries between the core
  state machine, page geometry, MAUI adapter, and demo.
- [Approaches and decision](project/approaches.md) — the alternatives assessed
  and why the current implementation uses a C# core with a Skia renderer.
- [Workarounds and solutions](project/workarounds.md) — host, workload, and
  packaging issues that need explicit handling.
- [Runtime contracts](project/runtime-contracts.md) — image ownership,
  cancellation, recycling, and diagnostic lifecycle rules.
- [Testing and validation](project/testing.md) — what is tested, why it matters,
  and what still requires a device.
- [Release readiness](project/release-readiness.md) — risks and release gates
  for a production-quality control.
- [Roadmap](project/roadmap.md) — sensible follow-up work after this baseline.

## Bundle conventions

Every concept document has YAML frontmatter with a required `type` and
descriptive metadata. `index.md` files are directory listings, not concepts.
Links are ordinary relative Markdown links so the bundle remains readable in a
checkout, a code browser, or an automated knowledge consumer. Unknown metadata
keys should be preserved when concepts are transformed.

Current validation evidence lives under `docs/validation/`, executable command
routing lives in `.agents/skills/`, and historical design/implementation
records live under `docs/superpowers/`. Those locations are intentionally not
part of the current concept index.

## Citations

- [Open Knowledge Format specification](https://github.com/GoogleCloudPlatform/knowledge-catalog/blob/main/okf/SPEC.md)
- [Google Cloud: How the Open Knowledge Format can improve data sharing](https://cloud.google.com/blog/products/data-analytics/how-the-open-knowledge-format-can-improve-data-sharing)
