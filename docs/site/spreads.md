# Two-page spreads

`PageSpreadView` presents a left and right page. It captures the neighboring
pages before enabling navigation.

```xml
<curl:PageSpreadView
    x:Name="Book"
    PageAspectRatio="0.75"
    LeftContent="{Binding LeftPage}"
    RightContent="{Binding RightPage}"
    PreviousLeftContent="{Binding PreviousLeftPage}"
    PreviousRightContent="{Binding PreviousRightPage}"
    NextLeftContent="{Binding NextLeftPage}"
    NextRightContent="{Binding NextRightPage}" />
```

Give each occupied slot its own MAUI view instance. Multiple views can display
the same page data.

## Sheet mapping

| Direction | Turning front | Revealed underneath | Turning back |
| --- | --- | --- | --- |
| Forward | Right | NextRight | NextLeft |
| Backward | Left | PreviousLeft | PreviousRight |

The back is the adjacent page the sheet lands on. If you omit it or its current
capture is unavailable, the control uses the selected mirrored or solid-color
fallback. Required front and destination captures still determine readiness.

## Commands and boundaries

Use `TryTurnForward()` and `TryTurnBackward()`. Enable buttons from
`CanTurnForward` and `CanTurnBackward`; these include snapshot readiness.
Use `PageSpreadNavigationState` to decide whether a neighboring spread exists.
Update the six content slots from a committed `PageTurnCompleted` event.

Set previous or next slots to `null` when no neighboring spread exists. For an
odd page count, the final spread has only a left page. Prepare that forward turn
with the final page in `NextLeftContent` and a blank view in `NextRightContent`;
the turn needs an underneath destination even when that destination is blank.
After committed completion, assign the final page to `LeftContent` and set
`RightContent` to `null`.

Replacing content schedules capture of dirty slots. Call
`RefreshSnapshotAsync()` for in-place edits. Explicit requests during a turn
wait until it ends and preserve cancellation. See [runtime rules](runtime.md)
for the UI-thread and lifecycle contract.
