# Runtime and lifecycle

## Use the UI thread

Assign content and image properties, issue turn commands and refresh snapshots
on the MAUI UI thread. Core geometry and interaction have no UI dependencies;
the MAUI adapter and its dispatcher do.

Turn commands return `false` while a turn is active or the control is
disconnected. A rejected command preserves the current curl. New pointer
presses during settling leave the pending completion intact.

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

Also cancel active turns when your window stops. The demo subscribes to window
stop/resume while its page is visible: it rolls back the turn, pauses video,
and resumes video only if that scene remains selected. Removing the page
detaches those handlers before releasing its content.

A spread refresh also waits for replacement content to attach and finish
layout. Cancellation can stop the caller's wait; hiding, replacing content or
disconnecting the handler releases obsolete layout waits. Keep refresh calls
asynchronous so the UI thread can finish arranging the views.

The controls reject delayed results from hidden, replaced or disconnected
content. A live-page refresh waiting for a turn also becomes obsolete when its
content is hidden or replaced, so it cannot recapture the outgoing scene.
If you supply a custom snapshotter, honor cancellation, use the UI
thread and transfer ownership of the returned image to the control.
Automatic load and layout refreshes coalesce changes over one frame before capturing, allowing
the live page and input to present first. Explicit `RefreshSnapshotAsync()`
retains its direct capture path and still waits for active interaction.
Pending automatic spread capture yields after turn completion, allowing an
immediate scene switch to discard outgoing work before rasterization.
The demo also batches rapid live-counter inputs into one capture of their
latest value, discarding queued work when its presentation changes.

An idle `PageTurnView` requests a fresh paint when it becomes visible, including
when the assigned images are unchanged. Rendering remains on demand.

## Commit once

`PageTurnCompleted` reports direction and whether the turn committed. Advance
the model only when `Committed` is true. Threshold-crossing events provide
feedback; they do not change the current page.

A completion subscriber may start another turn on a connected control,
including after cancellation. Disconnecting a wrapper rejects such restart
requests before releasing its snapshots.

Settling follows elapsed time between dispatcher callbacks, with the Core
controller limiting each sample to 100 ms. Busy UI callbacks therefore do not
slow the spring by advancing only one assumed frame.

The sample routes gestures, buttons, reduced motion and haptics through this
same completion path. Handle failed snapshots separately from successful
navigation so the interface does not report a refresh that never happened.
