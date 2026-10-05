# Visible computer-control observations

These checks used the actual Mac Catalyst demo through native computer control,
with fresh accessibility observations after actions and screenshots to choose
pointer locations and inspect layout. Fixture calls into event handlers are
reported separately. Screenshots were inspected during the session; this text
is the retained observation record, not a screenshot archive.

## Navigation replay

The saved [replay helpers](../../../tests/computer-control/pagecurl-checks.js)
ran command journeys in all four wide-layout scenes. Each scene passed first
boundary, forward, backward, last boundary, rejected extra forward, return to
first, and rejected extra backward: 28 assertions. Single/Content/Backside
counters used 01 → 02 → 01, reached 08, and returned to 01. Spread used
01–02 → 03–04 → 01–02, reached 07–08, and returned to 01–02.

The compact Spread replay passed the same seven assertions using mobile scene
labels and the Options/Close sheet. Reduced motion was temporarily enabled by
the replay and restored afterward.

## Real pointer input

- Wide Backside diagonal drag committed page 01 → 02. With flick disabled, a
  short diagonal drag returned to page 02 with status “Returned”.
- Compact Spread outer-edge drag crossed the spine and committed 01–02 → 03–04.
  A short drag returned to 03–04 with status “Returned”. Initial attempts outside
  the visible paper did not start a turn; points were corrected from the fresh
  screenshot before recording the passing journey.
- The final rebuilt app's compact Single scene passed the saved gesture helper:
  diagonal input reached “02 / 08, Turn complete”; short input reached
  “02 / 08, Returned”. The helper rejects an already-present expected result
  before sending input, so an unchanged page cannot certify a gesture.

## Content and responsive layout

Native window tiling/resizing exposed desktop and compact scene controls.
Compact Options opened, exposed labeled settings, and closed. Wide layout
returned through the native Fill command.

The live Content counter changed 0 → 1 and reported “Live content updated”.
Explicit refresh reported “Snapshot refreshed”. The saved live-content helper
repeated this on the final compact build (0 → 1) and after returning to wide
layout (1 → 2), including opening and closing compact Options.

An early compact screenshot showed a clipped counter despite correct AX text.
After the layout fix, the final compact screenshot showed the entire counter,
button and explanatory text visibly within the page. The native 250×333 layout
assertion also passed. Accessibility-tree presence alone was not used as visual
proof. VoiceOver/TalkBack, video playback and hardware touch were not certified.
