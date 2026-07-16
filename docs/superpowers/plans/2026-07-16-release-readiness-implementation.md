# PageCurl Release Readiness Implementation Plan

> For agentic workers: REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Complete the release gates in knowledge/project/release-readiness.md with an iOS-first local validation path, reusable accessibility and reduced-motion APIs, deterministic performance evidence, lifecycle-safe demo code, and a documented distribution contract.

**Architecture:** Keep interaction and geometry in the platform-free PageCurl.Core project. Put reduced-motion, programmatic-turn, rendering diagnostics, and lifecycle behavior at the PageCurl.Maui boundary. Keep demo navigation and generated image ownership in small demo-specific units, and validate mobile artifacts through an iOS simulator smoke script plus the existing Android package path.

**Tech Stack:** .NET 10, .NET MAUI 10, SkiaSharp 4.150.1, C# nullable reference types, dependency-free executable tests, Xcode/CoreSimulator, xcrun simctl, GitHub Actions.

## Global Constraints

- Target .NET 10 and MAUI 10; keep Microsoft.Maui.Controls at 10.0.20 and SkiaSharp.Views.Maui.Controls at 4.150.1.
- Preserve the core boundary: PageCurl.Core must not reference MAUI, SkiaSharp, dispatchers, or device APIs.
- Keep caller-provided SKImage values caller-owned; PageTurnView may dispose only shaders, paints, and other resources it creates.
- Keep the steady-state render path allocation-free; diagnostics are opt-in and disabled by default.
- Use Android API 24 as the minimum Android platform and iOS 15 as the minimum iOS platform.
- Build explicit target frameworks (net10.0, net10.0-ios, net10.0-android) so unrelated native workloads are not evaluated.
- Add regression tests before changing core behavior, and run the dependency-free core test runner after every core change.
- Use MIT licensing, semantic version 0.1.0, and a pre-1.0 compatibility/support policy.
- Never stage or commit the pre-existing local change in src/PageCurl.Demo/MainPage.xaml.cs unless it is explicitly part of the task being committed.
- Do not publish, merge, or push the PR as part of this plan.

---

### Task 0: Align the local iOS-first SDK/toolchain

**Files:**
- Create: global.json
- Modify: none outside the repository

**Interfaces:**
- Produces a repository SDK selection of .NET SDK 10.0.301 with rollForward set to latestPatch.
- Uses the writable user-local SDK root /Users/marius/.dotnet when the system SDK is root-owned.

- [ ] Step 1: Verify the current user-local SDK and workload locations

Run:

~~~
/Users/marius/.dotnet/dotnet --list-sdks
/Users/marius/.dotnet/dotnet workload list
~~~

Expected: the command identifies the writable user-local installation and reports the existing maui-ios workload.

- [ ] Step 2: Install user-local SDK 10.0.301 when it is absent

Run the official installer into the writable user-local root:

~~~
curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --version 10.0.301 --install-dir /Users/marius/.dotnet --no-path
~~~

Expected: /Users/marius/.dotnet/sdk/10.0.301 exists and /Users/marius/.dotnet/dotnet --version reports 10.0.301.

- [ ] Step 3: Install the matching mobile workloads into the user-local SDK

Run:

~~~
/Users/marius/.dotnet/dotnet workload install maui-ios maui-android
~~~

Expected: dotnet workload list reports both workloads for the selected SDK without writing to /usr/local/share/dotnet.

- [ ] Step 4: Write the SDK pin

Create global.json:

~~~json
{
  "sdk": {
    "version": "10.0.301",
    "rollForward": "latestPatch",
    "allowPrerelease": false
  }
}
~~~

- [ ] Step 5: Verify the repository selects the pin

Run:

~~~
PATH="/Users/marius/.dotnet:$PATH" dotnet --version
PATH="/Users/marius/.dotnet:$PATH" dotnet workload list
~~~

Expected: SDK 10.0.301 is selected and both mobile workloads are available.

- [ ] Step 6: Commit only the toolchain pin

~~~
git add global.json
git commit -m "Pin the .NET SDK for mobile builds"
~~~

### Task 1: Add reduced-motion and programmatic turns to the core controller

**Files:**
- Modify: src/PageCurl.Core/PageTurnController.cs
- Test: tests/PageCurl.Core.Tests/PageTurnControllerTests.cs
- Modify: tests/PageCurl.Core.Tests/Program.cs only if a new test group is added

**Interfaces:**
- PageTurnController.EndDrag(bool animate = true) keeps the current no-argument behavior and completes synchronously when animate is false.
- PageTurnController.BeginProgrammaticTurn(Vector2 pageSize, PageTurnDirection direction, bool animate = true) returns false for invalid page sizes or a non-idle controller, otherwise starts a committed turn from the direction-specific edge.
- Existing PageTurnCompleted remains the only completion notification and is raised once per turn.

- [ ] Step 1: Write failing controller tests

Add tests with these exact behaviors to PageTurnControllerTests.RunAll():

~~~
EndDrag_without_animation_commits_or_cancels_once_from_current_progress();
BeginProgrammaticTurn_starts_forward_from_right_edge();
BeginProgrammaticTurn_starts_backward_from_left_edge();
BeginProgrammaticTurn_rejects_invalid_size_and_busy_controller();
~~~

The immediate-release test must begin a drag, set progress above and below the snap threshold, call EndDrag(animate: false), assert Snapshot.State == PageTurnState.Idle, and assert exactly one event with the expected Committed value.

The programmatic tests must call BeginProgrammaticTurn(new Vector2(100, 200), direction, animate: false), assert the event direction and committed state, and assert a second call while the controller is settling returns false.

- [ ] Step 2: Run the focused test runner and verify the new tests fail for the missing API

Run:

~~~
/Users/marius/.dotnet/dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj
~~~

Expected: compilation fails because the new controller APIs do not exist yet.

- [ ] Step 3: Implement the minimal controller transitions

Use one private CompleteAtTarget() helper for both immediate and animated transitions. BeginProgrammaticTurn must set _pageSize, _direction, _dragStartPosition to (pageSize.X, pageSize.Y / 2) for forward or (0, pageSize.Y / 2) for backward, set _pointerPosition to that edge, set _progress = 0, _targetProgress = 1, reset velocity/completion state, and either call BeginSettling() or CompleteAtTarget() according to animate.

EndDrag(false) must compute the existing snap target and call CompleteAtTarget() instead of entering Settling. CompleteAtTarget() must set progress to the target, set state to Idle, and invoke the existing event exactly once.

- [ ] Step 4: Run the focused test runner and then the full core suite

Run:

~~~
/Users/marius/.dotnet/dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj
~~~

Expected: all existing groups and all new transition assertions pass.

- [ ] Step 5: Commit the core transition contract

~~~
git add src/PageCurl.Core/PageTurnController.cs tests/PageCurl.Core.Tests/PageTurnControllerTests.cs tests/PageCurl.Core.Tests/Program.cs
git commit -m "Add accessible page turn transitions"
~~~

### Task 2: Add reduced-motion, accessible commands, surface color, and diagnostics to the MAUI control

**Files:**
- Create: src/PageCurl.Maui/PageTurnPerformanceSnapshot.cs
- Modify: src/PageCurl.Maui/PageTurnView.cs

**Interfaces:**
- PageTurnView.ReduceMotion is a bindable bool, default false.
- PageTurnView.SurfaceColor is a bindable Color, default Color.FromArgb("#EBEBEB").
- PageTurnView.TryTurn(PageTurnDirection direction) returns false if the view has no valid size, is disconnected, or the controller is busy; otherwise it starts the shared controller transition and returns true.
- PageTurnView.PerformanceDiagnosticsEnabled is a normal bool, default false.
- PageTurnView.PerformanceSnapshot returns PageTurnPerformanceSnapshot with FrameCount, LastFrameMilliseconds, AverageFrameMilliseconds, LastFrameAllocatedBytes, and MaxFrameAllocatedBytes.
- PageTurnView.ResetPerformanceSnapshot() clears counters without disposing caller-owned images.

- [ ] Step 1: Add a value-type diagnostics snapshot

Create:

~~~csharp
namespace PageCurl.Maui;

public readonly record struct PageTurnPerformanceSnapshot(
    long FrameCount,
    double LastFrameMilliseconds,
    double AverageFrameMilliseconds,
    long LastFrameAllocatedBytes,
    long MaxFrameAllocatedBytes);
~~~

- [ ] Step 2: Add control properties and command behavior

Add bindable properties for ReduceMotion and SurfaceColor, plus the diagnostics properties and reset method. TryTurn must use _canvasSize or a validated Width/Height fallback, call Controller.BeginProgrammaticTurn(size, direction, animate: !ReduceMotion), start the dispatcher loop only while the controller is settling, and invalidate the surface.

Change the release branch in OnTouch to call Controller.EndDrag(animate: !ReduceMotion) and start the animation loop only when Controller.Snapshot.State == PageTurnState.Settling.

- [ ] Step 3: Add opt-in paint metrics without default-path allocations

Wrap the existing paint body with timestamp and allocation reads only when PerformanceDiagnosticsEnabled is true. Use Stopwatch.GetTimestamp() and GC.GetAllocatedBytesForCurrentThread(). Update the five counters with primitive assignments and expose them through the immutable snapshot. Keep DrawMesh, shader caching, fixed arrays, and caller-owned image semantics unchanged.

- [ ] Step 4: Use the configurable surface color

Replace the hard-coded canvas.Clear(new SKColor(235, 235, 235)) with conversion from SurfaceColor.Red, .Green, .Blue, and .Alpha, clamped to byte range. Invalidate when SurfaceColor changes.

- [ ] Step 5: Build the neutral MAUI target and the explicit iOS target

Run:

~~~
PATH="/Users/marius/.dotnet:$PATH" dotnet build src/PageCurl.Maui/PageCurl.Maui.csproj -c Release -f net10.0 -p:TargetFrameworks=net10.0
PATH="/Users/marius/.dotnet:$PATH" dotnet build src/PageCurl.Maui/PageCurl.Maui.csproj -c Release -f net10.0-ios -p:TargetFrameworks=net10.0-ios
~~~

Expected: both builds exit 0 with no warnings.

- [ ] Step 6: Commit the MAUI control contract

~~~
git add src/PageCurl.Maui/PageTurnView.cs src/PageCurl.Maui/PageTurnPerformanceSnapshot.cs
git commit -m "Add accessible control commands and render diagnostics"
~~~

### Task 3: Extract testable demo navigation and make the demo accessible/lifecycle-safe

**Files:**
- Create: src/PageCurl.Demo.Model/PageCurl.Demo.Model.csproj
- Create: src/PageCurl.Demo.Model/DemoNavigationState.cs
- Modify: src/PageCurl.Demo/PageCurl.Demo.csproj
- Create: src/PageCurl.Demo/DemoPageSet.cs
- Modify: src/PageCurl.Demo/MainPage.xaml
- Modify: src/PageCurl.Demo/MainPage.xaml.cs
- Modify: src/PageCurl.Demo/App.xaml
- Modify: PageCurl.sln
- Test: tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj
- Test: tests/PageCurl.Core.Tests/Program.cs
- Create: tests/PageCurl.Core.Tests/DemoNavigationStateTests.cs

**Interfaces:**
- DemoNavigationState(int pageCount) exposes PageIndex, CanTurnForward, CanTurnBackward, TryCommit(PageTurnDirection direction), and GetUnderPageIndex(PageTurnDirection direction).
- DemoPageSet.Create() returns a disposable object with IReadOnlyList<SKImage> Pages and creates deterministic pages for three normal sizes plus one large-page image.
- MainPage owns the DemoPageSet, subscribes to PageTurnCompleted, and clears the control image properties before disposing the page set during disappearance.

- [ ] Step 1: Write failing pure navigation tests

Create DemoNavigationStateTests.cs with tests for initial state, boundary rejection, forward/backward commits, and direction-specific under-page selection. Example:

~~~csharp
var state = new DemoNavigationState(3);
TestAssert.True(state.CanTurnForward);
TestAssert.False(state.CanTurnBackward);
TestAssert.True(state.TryCommit(PageTurnDirection.Forward));
TestAssert.Equal(1, state.PageIndex);
TestAssert.Equal(0, state.GetUnderPageIndex(PageTurnDirection.Backward));
~~~

- [ ] Step 2: Run the test runner and verify the navigation tests fail because the model is absent

Run:

~~~
/Users/marius/.dotnet/dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj
~~~

Expected: compilation fails because PageCurl.Demo.Model and DemoNavigationState do not exist.

- [ ] Step 3: Implement the model and project references

Use a net10.0 class library for PageCurl.Demo.Model, reference it from the demo and tests, and implement TryCommit so it changes the index only when the requested direction is within [0, pageCount - 1].

- [ ] Step 4: Run the pure navigation tests and the full core suite

Run:

~~~
/Users/marius/.dotnet/dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj
~~~

Expected: all core and navigation tests pass.

- [ ] Step 5: Implement disposable generated pages and lifecycle-safe MainPage

Move page generation from MainPage.CreatePage into DemoPageSet. Keep SKFont/SKTextAlign.Left for SkiaSharp 4. Clear FrontImage, BackImage, UnderPageImage, and PreviousPageImage, cancel interaction, unsubscribe the completion handler, and dispose the page set in OnDisappearing.

- [ ] Step 6: Add accessible controls and visual-state inputs to XAML

Add two labeled buttons with SemanticProperties.Description values Turn to previous page and Turn to next page, plus a labeled Switch with SemanticProperties.Description value Reduce page-turn motion. Bind button enabled state to the navigation model and bind the switch to PageView.ReduceMotion. Apply light/dark resources to page background, labels, border, and PageView.SurfaceColor; keep the page content inside the MAUI safe-area container.

- [ ] Step 7: Build the demo for iOS and run the full core suite

Run:

~~~
PATH="/Users/marius/.dotnet:$PATH" dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj
PATH="/Users/marius/.dotnet:$PATH" dotnet build src/PageCurl.Demo/PageCurl.Demo.csproj -c Release -f net10.0-ios -p:TargetFrameworks=net10.0-ios
~~~

Expected: all tests pass and the iOS demo build exits 0 with no warnings.

- [ ] Step 8: Commit the demo accessibility and lifecycle slice

~~~
git add PageCurl.sln src/PageCurl.Demo.Model src/PageCurl.Demo tests/PageCurl.Core.Tests
git commit -m "Add accessible demo navigation and image ownership"
~~~

### Task 4: Add deterministic core performance checks

**Files:**
- Create: tests/PageCurl.Core.Tests/PageTurnPerformanceTests.cs
- Modify: tests/PageCurl.Core.Tests/Program.cs

**Interfaces:**
- PageTurnPerformanceTests.RunAll() runs a warmed steady-state geometry loop and a warmed controller settle loop.

- [ ] Step 1: Write the failing allocation test

Add a test that constructs PageCurlOptions, PageMeshBuffer, and a valid pose before measuring, calls GC.Collect(), warms the loop, measures GC.GetAllocatedBytesForCurrentThread(), runs 1,000 PageCurlGeometry.Build calls, and asserts the delta is zero. Add a second loop that calls PageTurnController.UpdateDrag and Tick without subscribing an event and asserts zero managed allocations after warm-up.

- [ ] Step 2: Run the performance test and verify its baseline result

Run:

~~~
/Users/marius/.dotnet/dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj
~~~

Expected: the test fails only if the current steady-state code allocates; a failure records the measured byte delta in the assertion message.

- [ ] Step 3: Remove only measured steady-state allocations

Keep all arrays allocated by PageMeshBuffer and the controller outside the measured loops. Change implementation code only where the failing allocation measurement identifies a real loop allocation; do not add pooling or general abstractions without a measured failure.

- [ ] Step 4: Re-run all tests and commit the performance guard

Run:

~~~
/Users/marius/.dotnet/dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj
~~~

Then commit:

~~~
git add tests/PageCurl.Core.Tests/PageTurnPerformanceTests.cs tests/PageCurl.Core.Tests/Program.cs src/PageCurl.Core
git commit -m "Guard the steady-state page-turn allocation budget"
~~~

### Task 5: Add the iOS simulator smoke and profiling workflow

**Files:**
- Create: tools/ios-smoke.sh
- Create: docs/validation/ios-release-readiness.md
- Modify: .gitignore

**Interfaces:**
- tools/ios-smoke.sh [device-udid] builds the explicit iOS demo, boots/waits for an iPhone simulator, installs the app, launches it, captures light/dark screenshots, and prints the device/app paths.
- docs/validation/ios-release-readiness.md records commands, simulator identity, build artifacts, screenshots, accessibility/reduced-motion observations, and physical-device limitations.

- [ ] Step 1: Create the smoke script with strict shell behavior

The script must use set -euo pipefail, choose the first available iPhone 17 Pro and fall back to the first available iPhone, set DOTNET_ROOT=/Users/marius/.dotnet, run the explicit iOS build, locate src/PageCurl.Demo/bin/Release/net10.0-ios/iossimulator-arm64/PageCurl.Demo.app, boot and wait with xcrun simctl boot and xcrun simctl bootstatus --timeout 120, install with xcrun simctl install, launch com.example.pagecurldemo, and capture screenshots with xcrun simctl io <udid> screenshot.

- [ ] Step 2: Add deterministic light/dark screenshot commands

Use:

~~~
xcrun simctl ui "$DEVICE" appearance light
xcrun simctl io "$DEVICE" screenshot "$OUTPUT/light.png"
xcrun simctl ui "$DEVICE" appearance dark
xcrun simctl io "$DEVICE" screenshot "$OUTPUT/dark.png"
~~~

Record portrait screenshots automatically and document the exact Simulator menu action used to rotate to landscape before capturing the landscape pair.

- [ ] Step 3: Add the validation record template

Include a table for build, install, launch, light/dark appearance, portrait/landscape, previous/next accessibility commands, reduced motion, image replacement, rotation, and large-page scenarios. Include a separate Not proven locally section for physical GPU/frame-rate profiling, low-end Android, VoiceOver/TalkBack on hardware, and App Store signing.

- [ ] Step 4: Run the smoke script and inspect screenshots

Run:

~~~
./tools/ios-smoke.sh
~~~

Expected: the app installs and launches on the chosen simulator, screenshots exist for both appearances, and the validation record contains the simulator/runtime identity.

- [ ] Step 5: Capture optional diagnostics during a simulator run

Launch the demo with diagnostics enabled through the validation-only code path, exercise forward, backward, cancel, repeated, rotated, and large-page turns, and record PageTurnPerformanceSnapshot values. Use xcrun xctrace list templates to select an installed Time Profiler or Allocations template, and record the template name and run duration rather than claiming a physical-device result.

- [ ] Step 6: Commit the smoke tooling and validation record

~~~
git add tools/ios-smoke.sh docs/validation/ios-release-readiness.md .gitignore
git commit -m "Add the iOS release validation workflow"
~~~

### Task 6: Add distribution metadata, compatibility policy, and clean CI

**Files:**
- Create: LICENSE
- Create: SUPPORT.md
- Create: docs/compatibility.md
- Create: .github/workflows/verify.yml
- Modify: Directory.Build.props
- Modify: src/PageCurl.Maui/PageCurl.Maui.csproj
- Modify: README.md
- Modify: CONTRIBUTING.md
- Modify: knowledge/project/release-readiness.md
- Modify: knowledge/project/testing.md
- Modify: knowledge/project/workarounds.md
- Modify: knowledge/project/architecture.md

**Interfaces:**
- Package metadata exposes version 0.1.0, MIT licensing, repository URL, package README, and the supported target framework contract.
- .github/workflows/verify.yml runs core/neutral checks on Ubuntu and explicit iOS/Android builds on macOS using .NET 10 and the required MAUI workloads.
- docs/compatibility.md is the single compatibility matrix referenced by README and support policy.

- [ ] Step 1: Add MIT license and support policy

Write the standard MIT license with copyright holder mariusmuntean. SUPPORT.md must state that 0.x releases may break APIs, supported lines are .NET 10/MAUI 10/SkiaSharp 4.150.1, minimum platforms are Android API 24 and iOS 15, issues must include target/device/build logs, and physical-device profiling remains a consumer release gate.

- [ ] Step 2: Add compatibility and version metadata

Add VersionPrefix, Version, PackageLicenseExpression, PackageReadmeFile, RepositoryUrl, and PackageTags to Directory.Build.props or the packable MAUI project, keeping values centralized. Add a compatibility table for Core, Android, iOS, Mac Catalyst, and Windows with status values Build, Simulator, Physical device, or Not validated.

- [ ] Step 3: Add clean CI workflows

Use pinned action major versions, actions/setup-dotnet with 10.0.301, and explicit commands:

~~~
dotnet workload install maui-ios maui-android
dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj -c Release
dotnet build src/PageCurl.Maui/PageCurl.Maui.csproj -c Release -f net10.0 -p:TargetFrameworks=net10.0
dotnet build src/PageCurl.Demo/PageCurl.Demo.csproj -c Release -f net10.0-ios -p:TargetFrameworks=net10.0-ios
dotnet build src/PageCurl.Demo/PageCurl.Demo.csproj -c Release -f net10.0-android -p:TargetFrameworks=net10.0-android
~~~

Use a macOS job for mobile workloads and an Ubuntu job for the platform-free tests. Do not invoke a bare multi-target build.

- [ ] Step 4: Update repository documentation

Document exact user-local SDK commands, iOS smoke invocation, Android package invocation, image lifetime/threading/recycling rules, accessible command usage, reduced-motion mapping, theme/rotation/safe-area review, and the distinction between simulator and physical-device evidence.

- [ ] Step 5: Validate workflow syntax and documentation links

Run:

~~~
git diff --check
rg -n 'TBD|TODO|FIXME|file://|http://|PageCurl\\.Core|PageCurl\\.Maui|PageCurl\\.Demo' README.md CONTRIBUTING.md SUPPORT.md docs/compatibility.md knowledge .github/workflows/verify.yml
~~~

Expected: no placeholders, no file:// links, and every referenced project/path exists.

- [ ] Step 6: Commit distribution and CI metadata

~~~
git add LICENSE SUPPORT.md docs/compatibility.md .github/workflows/verify.yml Directory.Build.props src/PageCurl.Maui/PageCurl.Maui.csproj README.md CONTRIBUTING.md knowledge
git commit -m "Document the PageCurl distribution contract"
~~~

### Task 7: Final iOS-first validation and evidence handoff

**Files:**
- Modify: docs/validation/ios-release-readiness.md
- No source changes unless a failing validation test identifies one

- [ ] Step 1: Run the complete local validation matrix

Run:

~~~
PATH="/Users/marius/.dotnet:$PATH" dotnet run --project tests/PageCurl.Core.Tests/PageCurl.Core.Tests.csproj -c Release
PATH="/Users/marius/.dotnet:$PATH" dotnet build src/PageCurl.Core/PageCurl.Core.csproj -c Release
PATH="/Users/marius/.dotnet:$PATH" dotnet build src/PageCurl.Maui/PageCurl.Maui.csproj -c Release -f net10.0 -p:TargetFrameworks=net10.0
PATH="/Users/marius/.dotnet:$PATH" dotnet build src/PageCurl.Maui/PageCurl.Maui.csproj -c Release -f net10.0-ios -p:TargetFrameworks=net10.0-ios
PATH="/Users/marius/.dotnet:$PATH" dotnet build src/PageCurl.Demo/PageCurl.Demo.csproj -c Release -f net10.0-ios -p:TargetFrameworks=net10.0-ios
PATH="/Users/marius/.dotnet:$PATH" dotnet build src/PageCurl.Demo/PageCurl.Demo.csproj -c Release -f net10.0-android -p:TargetFrameworks=net10.0-android
./tools/ios-smoke.sh
~~~

- [ ] Step 2: Verify the release checklist against evidence

Mark each release gate only with a command output, screenshot, profiler record, or explicit Not validated locally note. Check that the report names the exact SDK, workload manifests, Xcode version, iOS runtime, simulator model, APK/app bundle paths, and current Git commit.

- [ ] Step 3: Verify the worktree and staged scope

Run:

~~~
git diff --check
git status --short --branch
git log --oneline --decorate -12
~~~

Expected: no whitespace errors, all intended implementation commits visible, and the pre-existing src/PageCurl.Demo/MainPage.xaml.cs compatibility fix either intentionally included in a relevant commit or still clearly listed as the only unrelated local modification.

- [ ] Step 4: Commit only the final evidence update

~~~
git add docs/validation/ios-release-readiness.md
git commit -m "Record local iOS release validation"
~~~

