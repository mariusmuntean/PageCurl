# Images and live content

## Images

Use `PageTurnView` when your application already supplies `SKImage` values.

```csharp
TurnView.FrontImage = currentPage;
TurnView.UnderPageImage = nextPage;
TurnView.PreviousPageImage = previousPage;
TurnView.BacksideMode = PageBacksideMode.CustomContent;
TurnView.BackImage = reverseSide;
```

Keep assigned images alive. Before disposing or recycling an image set, guard
callbacks that can restart a turn, cancel interaction and clear the image
properties. The control never disposes your images. Use
`ContentScale="AspectFit"` to preserve their proportions.

If `PreviousPageImage` is absent, backward turns fall back to `UnderPageImage`.
Supply both when the neighboring pages differ.

## Live MAUI views

Set `PageCurlView.FrontContent` to a MAUI view. For a separate reverse side,
set `BackContent` and choose `BacksideMode="CustomContent"`. Mirrored and
solid-color modes need no second view.

The control owns its captured textures. Your application owns supplied
`UnderPageImage` and `PreviousPageImage` values, and any media or other resources
used by the hosted views. Use the same borrowed-image cleanup rules above for
destination images.

After changing content in place, request a fresh capture:

```csharp
await Reader.RefreshSnapshotAsync(cancellationToken);
```

The control retains the last successful image when capture fails. Handle
`SnapshotFailed` to explain that state to the user. A completed refresh task
is not proof of a new frame when the platform adapter reports failure through
that event.

## Video

A native video layer may not appear in a page snapshot. Keep a poster view
beneath the player, or supply a poster as the curl content. The sample uses a
bundled clip and still image; the library itself has no media-player dependency.

## Interaction settings

- `LockedEdge` or `LockedAnchor` chooses the stationary edge or pinned corner.
- `PageChangeThreshold` controls the travel required to commit on release.
- `FlickEnabled` allows a quick gesture to finish sooner.
- `ReduceMotion` completes releases and commands without settling animation.

Use labeled navigation buttons as an alternative to gestures. Update the model
from `PageTurnCompleted` for both paths. See the [API reference](api-index.md) for the
properties exposed by each control.
