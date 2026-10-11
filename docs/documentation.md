# Documentation publishing

The public site is <https://mariusmuntean.github.io/PageCurl-docs/>.
Its static files live in the public `mariusmuntean/PageCurl-docs` repository.

Edit guides in `docs/site/`. Core and MAUI XML documentation comes from the
built assemblies; DocFX generates the API signatures. The pinned tool manifest
keeps local and hosted builds on the same version.

Guides and preview media follow the current source. When source fixes await a
package release, state that beside the installation instructions and current
release status so users can choose the published package or a source checkout.

## Branding assets

`docs/site/images/logo.svg` is the apricot paper-monogram source. The README,
documentation header, homepage and favicon use this vector mark. The demo also
links it as its app icon and splash screen, with an apricot (`#F8CB98`) background.
Apple targets select the generated icon set with the
[AppIcon build property](https://learn.microsoft.com/en-us/dotnet/ios/building-apps/build-properties#appicon);
Android's manifest references both the regular and round launcher icons.
`docs/site/images/pagecurl-icon.png` is its 512 by 512 export for both NuGet
packages and the demo header. The NuGet README uses Markdown only; the listing
header already displays the package icon. HTML image tags are escaped as text
by NuGet.org and are rejected by the package checker.

After editing the vector, regenerate the PNG with
`rsvg-convert -w 512 -h 512 -o docs/site/images/pagecurl-icon.png docs/site/images/logo.svg`.
Keep the logo embedded in `social-preview.svg` synchronized too; export it with
`rsvg-convert -o docs/site/images/social-preview.png docs/site/images/social-preview.svg`.
The 1280 by 640 social preview is uploaded in the source repository's
**Settings → General → Social preview**. GitHub stores that selection separately
from the files in Git. It does not change the owner's personal avatar.

Published NuGet versions retain the icon embedded in their original packages;
the new icon is included when a new package version is published.

## Publishing

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

The source repository's Documentation workflow validates each change and
retains the site artifact. It cannot push to the separate documentation
repository with its repository-scoped token. Cross-repository automation would
require an explicitly configured credential; none is stored by these scripts.

Keep historical plans, validation logs, local paths and device identifiers out
of the public site. Stable architecture and internal validation notes remain
under `docs/`, outside the curated site content list.
