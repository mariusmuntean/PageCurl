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

## Sheet mapping

| Direction | Turning front | Revealed underneath | Turning back |
| --- | --- | --- | --- |
| Forward | Right | NextRight | NextLeft |
| Backward | Left | PreviousLeft | PreviousRight |

The back is the adjacent page the sheet lands on. If you omit it, the control
uses the selected mirrored or solid-color fallback.

## Commands and boundaries

Use `TryTurnForward()` and `TryTurnBackward()`. Enable buttons from
`CanTurnForward` and `CanTurnBackward`; these include snapshot readiness.
Use `PageSpreadNavigationState` to decide whether a neighboring spread exists.
Update the six content slots from a committed `PageTurnCompleted` event.

Replacing content schedules capture of dirty slots. Call
`RefreshSnapshotAsync()` for in-place edits. Explicit requests during a turn
wait until it ends and preserve cancellation. See [runtime rules](runtime.md)
for the UI-thread and lifecycle contract.
