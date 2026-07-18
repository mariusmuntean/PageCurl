---
name: pagecurl-maui-validation
description: Use when changing PageCurl.Core, PageCurl.Maui, PageCurl.Demo, tests, MAUI workloads, CI verification, or rendering/device evidence. Run the smallest relevant validation path, diagnose workload failures before source edits, and report what passed, failed, or was not run.
---

# PageCurl validation

Read only the relevant project references before choosing commands:

- `README.md`
- `global.json`
- `knowledge/project/testing.md`
- `knowledge/project/workarounds.md`
- `docs/validation/ios-release-readiness.md` for simulator work

## Route changes to checks

- Core geometry, interaction, navigation, or tests:
  `dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj -c Release`
- MAUI adapter or package metadata:
  `dotnet build src/PageCurl.Maui/PageCurl.Maui.csproj -c Release -f net10.0 -p:TargetFrameworks=net10.0`
- Android demo:
  `dotnet build src/PageCurl.Demo/PageCurl.Demo.csproj -c Release -f net10.0-android`
- iOS simulator evidence: `./tools/ios-smoke.sh`
- Mac Catalyst layout:
  `dotnet build src/PageCurl.Demo/PageCurl.Demo.csproj -c Debug -f net10.0-maccatalyst`
- CI project references: run the workflow path check and `dotnet sln PageCurl.sln list`.

Use explicit target frameworks. Do not evaluate iOS, Mac Catalyst, or Windows targets on Linux.

## Handle failures

1. Capture completed command output.
2. Inspect the first actual error.
3. Classify the failure as workload, SDK, package, project reference, compilation, runtime, or visual.
4. Do not edit source to address a workload-resolution error.
5. Report unavailable platform checks as not run or not verified.

For CI checks, verify every `.csproj` path referenced by `.github/workflows/verify.yml` exists. Use `dotnet sln PageCurl.sln list` as an informational check of the current solution; the workflow project set does not need to equal the solution project set.

## Report

End the run with the commands executed, the status of each command, evidence paths, environment blockers, and any project knowledge or validation record that needs updating.
