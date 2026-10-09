# Documentation publishing

The public site is <https://mariusmuntean.github.io/PageCurl-docs/>.
Its static files live in the public `mariusmuntean/PageCurl-docs` repository.
The current account plan cannot host Pages from the private source repository.

Edit guides in `docs/site/`. Core and MAUI XML documentation comes from the
built assemblies; DocFX generates the API signatures. The pinned tool manifest
keeps local and hosted builds on the same version.

Screenshots must show the behavior the guide explains, using the documented
sample itself. A page-turn example needs a visible curl; an empty resting page
does not demonstrate it. Inspect the final image at its displayed size before
publishing, including readable text, navigation and the rendered sheet. Verify
the original file in a browser if an inspection thumbnail looks blank or
incomplete. Keep dated validation captures with their original records; label
what they prove instead of replacing old evidence with a newer build.

Run `bash tools/build-docs.sh` from the repository root. The script builds the
neutral assemblies, runs DocFX with warnings as errors, then checks local links,
anchors and publication boundaries. Output goes to `.artifacts/site`.

Run `bash tools/publish-docs.sh` to rebuild and publish using your existing Git
credentials. The script copies only the validated site to the documentation
repository; that repository's Pages workflow handles deployment. It creates no
feature branch or access token. Check the resulting Pages run before claiming
that publication completed.

The private repository's Documentation workflow validates each change and
retains the site artifact. It cannot push to the separate documentation
repository with its repository-scoped token. Cross-repository automation would
require an explicitly configured credential; none is stored by these scripts.

Keep historical plans, validation logs, local paths and device identifiers out
of the public site. Stable architecture and internal validation notes remain
under `docs/`, outside the curated site content list.
