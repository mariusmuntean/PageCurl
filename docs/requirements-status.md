# Requirements status

The acceptance contract is [requirements and acceptance criteria](requirements.md).
Historical plans are retained as context; they do not define unfinished features.

| Scope | Implementation | Evidence and remaining work |
| --- | --- | --- |
| R01–R05: architecture, geometry, input, completion, layout/navigation | Implemented | Core deterministic and allocation tests; native/visual checks complement the math. |
| R06–R08: ownership, refresh and physical spreads | Implemented | Neutral lifecycle/capture tests and native snapshot/mesh/demo assertions. Sustained hardware memory and texture profiling remains open. |
| R09: public setting validation | Implemented | Behavioral API tests verify constructor option preservation, last-valid state, forwarding and reduced-motion cancellation. |
| R10: responsive showcase and commands | Implemented | Native assertions and computer-control journeys. VoiceOver/TalkBack hardware review remains open. |
| R11: bounded buffers and warmed Core allocation behavior | Implemented | Core zero-allocation guards. Physical-device frame pacing and thermal/texture profiling remains open. |
| R12: fresh package consumer | Implemented | Isolated neutral and Windows consumers, complete native package assets and fresh NuGet.org consumption passed for 0.1.0; see the [release record](validation/public-release-2026-10-10/README.md). Future releases need compatibility checks against that published baseline. |

“Implemented” is a source status, not a blanket platform certification.
[Compatibility](compatibility.md) records platform evidence, and
[release readiness](release-readiness.md) records distribution gates.
Current evidence and failures belong in dated [validation records](validation/).
