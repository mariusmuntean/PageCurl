# Runtime and lifecycle

## Use the UI thread

Assign content and image properties, issue turn commands and refresh snapshots
on the MAUI UI thread. Core geometry and interaction have no UI dependencies;
the MAUI adapter and its dispatcher do.

## Own images deliberately

`PageTurnView` borrows images. Your application must keep them alive until they
are no longer assigned or used by an active turn.

`PageCurlView` and `PageSpreadView` own their captured textures. They reject and
dispose obsolete captures, retain a last-good frame on failure, and keep active
turn textures stable until the turn ends.

## Handle cancellation and reuse

Call `CancelInteraction()` when removing or recycling a view. It rolls back,
stops settling and releases pointer ownership. Clear borrowed image references
before disposing them. Handler teardown cancels active work and releases
adapter-owned snapshots; a cancelled caller wait does not mean the underlying
serialized capture has finished cleaning up.

The controls reject delayed results from hidden, replaced or disconnected
content. If you supply a custom snapshotter, honor cancellation, use the UI
thread and transfer ownership of the returned image to the control.

## Commit once

`PageTurnCompleted` reports direction and whether the turn committed. Advance
the model only when `Committed` is true. Threshold-crossing events provide
feedback; they do not change the current page.

The sample routes gestures, buttons, reduced motion and haptics through this
same completion path. Handle failed snapshots separately from successful
navigation so the interface does not report a refresh that never happened.
