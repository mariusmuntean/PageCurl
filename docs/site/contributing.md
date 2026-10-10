# Contributing and validation

Keep geometry and interaction in Core. Keep MAUI, Skia, dispatcher and device
work in the adapter. Keep scene and media dependencies in the sample.

## Verify a change

Read the repository requirements before changing behavior. Add a deterministic
regression that fails before the fix, then run the relevant Core or neutral
MAUI suite. Rendering and lifecycle changes also need native evidence.

The Verify workflow runs deterministic tests, a fresh package consumer,
validation-tool tests, native package checks and platform builds. The
Documentation workflow builds this site, checks local links and retains the validated site artifact.

Use the repository validation skills for explicit target commands. Record
completed evidence under `docs/validation/`; do not infer a runtime pass from
an unsigned build or a workflow definition.

## Build these docs

From the repository root:

```bash
bash tools/build-docs.sh
```

The pinned local tool manifest selects DocFX. Output goes to `.artifacts/site`.
Serve that directory to inspect the site before publishing.
Run `bash tools/publish-docs.sh` to rebuild and publish it to the public
`PageCurl-docs` repository. Its Pages workflow deploys those static files.
Publication uses your existing GitHub credentials. The site includes
these curated guides and generated public API signatures. Historical logs,
plans and device identifiers stay in the repository.

## Report an issue

Use the [source repository's issue tracker](https://github.com/mariusmuntean/PageCurl/issues).
Include the package/commit version, target framework, OS and device model,
reproduction steps, and the complete first build or runtime error. For visual
issues, include a small sample and a screenshot of the active turn.
