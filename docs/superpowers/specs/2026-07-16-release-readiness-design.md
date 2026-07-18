# PageCurl Release Readiness Design

**Date:** 2026-07-16  
**Status:** Historical design record; superseded by the current release gates
and validation records.
**Scope:** Complete the release gates in `knowledge/project/release-readiness.md`, prioritizing iOS simulator validation on the local Mac while preserving Android packaging validation.

## Goal

Make PageCurl a small, modular, testable .NET MAUI control that has evidence for its core behavior, mobile builds, accessibility alternatives, reduced-motion behavior, image/resource lifetime, performance characteristics, visual scenarios, and distribution contract.

The target is a strong release candidate baseline. A simulator cannot replace representative physical-device profiling, so hardware-dependent gaps will be recorded explicitly rather than presented as complete.

## Requirements mapped to deliverables

1. Clean mobile builds and packaging: pin the SDK/workload/package line, add CI coverage for clean iOS and Android builds, and produce local iOS-first artifacts.
2. Automated and smoke validation: retain the dependency-free core suite, add regression coverage for new control behavior, and add a repeatable iOS simulator build/install/launch/screenshot smoke script.
3. Performance evidence: preserve the allocation-free core/rendering design, add deterministic core allocation checks and opt-in render metrics that add no per-frame allocations when disabled, and record simulator profiling results with hardware limitations called out.
4. Visual scenarios: make theme, surface color, page size, rotation, safe-area, and reduced-motion scenarios explicit in the demo and validation checklist.
5. Accessibility: provide labeled Previous/Next commands that do not require a gesture, expose reduced-motion behavior through the control API, and document how a consuming application maps platform accessibility preferences.
6. Ownership and lifecycle: isolate demo image creation/ownership, release images only after the view stops using them, and document threading, navigation, and recycling rules.
7. Distribution contract: add an MIT license, semantic versioning policy, compatibility matrix, support policy, and package metadata suitable for a pre-1.0 NuGet release.

## Architecture

### Core interaction contract

`PageCurl.Core` remains independent of MAUI, SkiaSharp, dispatchers, and device APIs.

`PageTurnController` gains two small capabilities:

- an optional non-animated completion path for reduced-motion releases;
- a programmatic turn entry point that accepts a validated page size and direction, starts from the right edge for a forward turn or the left edge for a backward turn, and raises the same completion event as a gesture.

Existing gesture behavior remains the default. Programmatic turns and immediate completion share the existing state machine, target progress, and completion event rather than introducing a second navigation model.

The new paths will be covered by deterministic tests for invalid page sizes, busy-state rejection, forward/backward direction, immediate completion, animated completion, and exactly-once events.

### MAUI control contract

`PageTurnView` remains the renderer and lifecycle boundary. It will expose:

- a bindable reduced-motion property that skips settle animation while preserving direct manipulation;
- a `TryTurn(PageTurnDirection)` method for accessible/non-gesture commands;
- configurable surface appearance needed for light/dark visual review;
- opt-in performance diagnostics represented by immutable value snapshots.

When diagnostics are disabled, the paint path performs only the existing work plus a single disabled check. When enabled, it measures paint duration, frame count, and managed bytes allocated on the paint thread. Metrics are diagnostic evidence, not a production performance guarantee.

Handler disconnect/reconnect, shader replacement, image-property changes, and animation cancellation continue to be owned by `PageTurnView`. Caller-owned `SKImage` values are never disposed by the control.

### Demo presentation boundary

The demo will be split into small responsibilities:

- a disposable page-image set that owns generated `SKImage` values;
- a page navigation model that clamps indices and selects direction-aware under-pages;
- a MAUI page that renders the control and exposes labeled Previous/Next commands plus a reduced-motion switch.

The commands will call `PageTurnView.TryTurn` when a turn is possible. At the first/last page they will remain disabled or no-op without starting a curl. The page-completion event remains the single place that commits the new index.

The demo will use theme-aware MAUI resources and safe-area-aware layout. The generated pages will include a large-page scenario and remain deterministic so screenshot comparisons are meaningful.

## Validation and tooling

### Automated validation

- Extend `PageCurl.Core.Tests` for the new controller contract and repeated geometry allocation behavior.
- Build the neutral core/MAUI targets with warnings treated as errors where supported.
- Build and package `PageCurl.Maui` and `PageCurl.Demo` for `net10.0-ios` on this Mac.
- Preserve the already validated explicit Android package build.

### iOS simulator smoke path

Add a repository script that:

1. selects a known available iPhone simulator and iOS runtime;
2. boots and waits for it;
3. builds the demo for `net10.0-ios`;
4. installs and launches the app;
5. captures deterministic screenshots in light and dark appearance and portrait/landscape orientations;
6. records the installed app bundle and simulator identity.

Interactive gestures, accessibility activation, and reduced-motion behavior will be exercised through the simulator UI and recorded in the validation notes. The script will not claim physical-device frame-rate or GPU results.

### Performance evidence

Add a focused benchmark-style core test that warms the geometry path, runs repeated drag/settle frames against fixed buffers, and fails if managed allocations occur in the steady-state core path. Add a documented simulator procedure for enabling render diagnostics and recording frame duration/allocation snapshots during forward, backward, cancel, repeated, rotated, and large-page scenarios.

The evidence will distinguish:

- deterministic core allocation results;
- simulator render metrics;
- physical-device profiling still required for release.

## Distribution and documentation

- Add `LICENSE` with MIT text.
- Add `SUPPORT.md` defining supported .NET/MAUI/SkiaSharp lines, issue-reporting expectations, and the pre-1.0 compatibility stance.
- Add a compatibility matrix covering .NET 10, MAUI 10, SkiaSharp 4.150.1, Android API 24+, iOS 15+, and the validation status of Android, iOS, Mac Catalyst, and Windows.
- Add semantic version metadata and package metadata to the packable project; use `0.1.0` as the initial public contract because the repository currently has no version policy.
- Update README and OKF testing/release documents with exact local commands, image ownership, threading, navigation/recycling, accessibility, reduced-motion, and profiling guidance.
- Add clean CI workflow definitions that pin the SDK major/minor line, install only the workloads required by each target, and build the explicit target framework instead of evaluating unrelated native targets.

## Non-goals

- No native Metal renderer rewrite.
- No document pagination engine or image decoding/cache framework.
- No claim that simulator profiling substitutes for low-end Android or current physical iOS hardware.
- No automatic publishing, merging, or pushing of the PR without a separate request.

## Acceptance evidence

The implementation is ready for review when the repository contains the documented release artifacts and the validation record shows:

- all core tests passing;
- clean neutral, iOS, and Android builds/packages on the available local toolchains;
- iOS simulator smoke screenshots for required visual modes;
- accessible command and reduced-motion behavior verified;
- deterministic core allocation results and opt-in render metrics recorded;
- image/lifecycle/distribution documentation present;
- remaining physical-device and Apple packaging limitations explicitly listed.
