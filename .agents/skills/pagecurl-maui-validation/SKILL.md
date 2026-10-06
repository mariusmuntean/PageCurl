---
name: pagecurl-maui-validation
description: Use when changing PageCurl.Core, PageCurl.Maui, PageCurl.Demo, tests, MAUI workloads, CI verification, or rendering/device evidence. Run the smallest relevant validation path, diagnose workload failures before source edits, and report what passed, failed, or was not run.
---

# PageCurl validation

Read only the relevant project references before choosing commands:

- `README.md`
- `global.json`
- `docs/testing.md`
- `docs/platform-notes.md`
- `docs/validation/ios-release-readiness.md` for simulator work

The command matrix below is the authoritative routing for this repository.
Project documentation explains why a check matters; it should not duplicate these
commands.

## Route changes to checks

- Core geometry, interaction, navigation, or tests:
  `dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj -c Release`
- MAUI adapter or package metadata:
  `dotnet build src/PageCurl.Maui/PageCurl.Maui.csproj -c Release -f net10.0 -p:TargetFrameworks=net10.0`
- Android demo:
  `dotnet build src/PageCurl.Demo/PageCurl.Demo.csproj -c Release -f net10.0-android`
- Public API, bindable validation, snapshot lifecycle, or neutral tests:
  `dotnet run --project tests/PageCurl.Maui.Tests/PageCurl.Maui.Tests.csproj -c Release -f net10.0 -p:TargetFrameworks=net10.0`
- Package consumption: `bash tools/test-package-consumer.sh`. Set
  `PAGECURL_DOTNET` to an explicit workload-enabled CLI if needed. This packs
  neutral packages and tests a temporary external consumer with an isolated cache.
- Documented quick start: `bash tools/test-quick-start.sh`. It compiles the
  guide's unchanged XAML and code-behind in a temporary neutral MAUI project.
- Validation-driver changes:
  `python3 -m unittest discover -s tools/tests -p 'test_*.py'`.
- iOS simulator evidence: `./tools/ios-smoke.sh`
- Native Apple snapshot and linked demo regression fixture:
  `dotnet build tests/PageCurl.Apple.Tests/PageCurl.Apple.Tests.csproj -c Debug -f net10.0-maccatalyst`,
  then execute the built app binary with `PAGECURL_TEST_OUTPUT` set to an absolute log path.
  The process exits nonzero on any assertion failure. Record any local Xcode override separately.
  Prefer `python3 tools/run-native-fixture.py <absolute-built-app-binary>` to
  bound execution and reject stale or incomplete assertion output.
- Mac Catalyst layout:
  `dotnet build src/PageCurl.Demo/PageCurl.Demo.csproj -c Debug -f net10.0-maccatalyst`
- CI project references: run `bash tools/check-workflow-projects.sh` and
  `dotnet sln PageCurl.sln list`. The script scans all workflow YAML files by
  default; pass one workflow path to narrow the check.

Use explicit target frameworks. Do not evaluate iOS, Mac Catalyst, or Windows targets on Linux.
The Linux neutral lane disables workload resolution with
`MSBuildEnableWorkloadResolver=false`; native builds keep workload resolution enabled.
On this Mac, use `-m:1 -nr:false` for serial builds if MSBuild worker nodes exit
prematurely, then execute the built test DLL. Classify this as a toolchain failure,
not a source compilation failure.

## Handle failures

1. Capture completed command output.
2. Inspect the first actual error.
3. Classify the failure as workload, SDK, package, project reference, compilation, runtime, or visual.
4. Do not edit source to address a workload-resolution error.
5. Report unavailable platform checks as not run or not verified.

For CI checks, `bash tools/check-workflow-projects.sh` verifies every `.csproj`
path referenced by `.github/workflows/verify.yml` exists. Use
`dotnet sln PageCurl.sln list` as an informational check of the current
solution; the workflow project set does not need to equal the solution project
set.

## Report

End the run with the commands executed, the status of each command, evidence paths, environment blockers, and any project documentation or validation record that needs updating.
