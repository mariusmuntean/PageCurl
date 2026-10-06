# Security reporting

Keep suspected vulnerabilities and exploit details out of public bug reports.
Use GitHub's **Security → Report a vulnerability** action on the source
repository when it is available. Repository collaborators can also use a
private contact channel already shared by the maintainer.

If you cannot access a private reporting route, open an issue requesting one.
Include only a request for private contact; omit exploit steps, sensitive
content and attachments until a private channel is available. The source
repository and its tracker currently require access.

In a private report, include:

- The package version or source commit and affected target framework.
- The suspected impact and the conditions needed to reproduce it.
- A small reproduction, with credentials and personal content removed.
- Any workaround you have tested.

The project has a 0.x API and no verified public NuGet release. Check the
[compatibility matrix](docs/compatibility.md) for the target baseline and
recorded evidence.
