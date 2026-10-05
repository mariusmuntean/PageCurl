# Release hardening — 2026-10-05

Completed local checks:

- Core: all 12 deterministic test groups passed.
- Neutral MAUI: all 5 test groups passed.
- Validation tooling: 20 Python and 5 JavaScript tests passed.
- Isolated package consumer: Core and MAUI restored from a fresh local feed,
  compiled representative APIs and executed the consumer assertions.
- Native package inspection: neutral .NET, Android, iOS and Mac Catalyst
  assemblies, XML documentation and transitive Core dependencies passed.
- DocFX: zero warnings/errors; local link, anchor and publication checks passed.
- Obsolete-output probe: a removed HTML guide and API YAML file did not survive
  the next documentation build.
- Browser: public navigation/search opened API results; desktop (1280×900) and
  phone (390×844) layouts were inspected. Phone content width matched viewport
  width. The logo and navigation labels were corrected after visual review.
- Shell syntax, workflow project references and whitespace checks passed.

The [hosted documentation build](https://github.com/mariusmuntean/PageCurl/actions/runs/37336569201)
and [Pages deployment](https://github.com/mariusmuntean/PageCurl-docs/actions/runs/37336928689)
completed successfully. The site is available at
<https://mariusmuntean.github.io/PageCurl-docs/>.

The first documentation CI run failed because the generated-input directory's
parent did not exist in a clean checkout; creating parent directories fixed it.
The private source repository cannot host Pages on the current account plan.
The public documentation repository contains curated static docs and API
reference, with publication through the documented script.

These checks do not certify hardware performance, Windows runtime rendering,
VoiceOver/TalkBack, signing, store packages or published NuGet consumption.
See [release gates](../../release-readiness.md).
