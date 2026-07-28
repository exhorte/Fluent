# Capsule Redesign — Visual Specification

## Idle State (○○○)

| Property | Value |
|----------|-------|
| Circle diameter | 30 DIPs |
| Circle spacing | 9 DIPs |
| Stroke color | #B4B4B4 |
| Stroke thickness | 1.25 DIPs |
| Fill color | #B8000000 (black, ~72% opacity) |
| Total width | 108 DIPs (3 × 30 + 2 × 9) |
| Total height | 30 DIPs |
| Window background | Transparent |
| Interaction | Click-through (WS_EX_TRANSPARENT) |

## Recording State

### Capsule Pill

| Property | Value |
|----------|-------|
| Width | 126 DIPs |
| Height | 34 DIPs |
| Corner radius | 17 DIPs (pill shape) |
| Fill | #DC050505 (near-black, slight transparency) |
| Border | #454545, 1 DIP |
| Horizontal padding | 5 DIPs |

### Cancel Button (left)

| Property | Value |
|----------|-------|
| Diameter | 22 DIPs |
| Fill | #353535 |
| Border | #555555, 1 DIP |
| Icon | X symbol, #F2F2F2, 1.6 DIP stroke |
| Accessibility | "Cancel dictation" / "Annuler la dictée" |
| Enabled | No (future wiring) |

### Waveform (center)

| Property | Value |
|----------|-------|
| Area width | ~48 DIPs (13 bars × 2 + 12 gaps × 1.5) |
| Bar count | 13 |
| Bar width | 2 DIPs |
| Bar spacing | 1.5 DIPs |
| Bar max height | 14 DIPs |
| Bar min height | 3 DIPs |
| Bar color | #F5F5F6 (white) |
| Bar rounding | 1 DIP radius |
| Animation | Staggered DoubleAnimation on ScaleY, 260–440ms periods |

### Paste Button (right)

| Property | Value |
|----------|-------|
| Diameter | 22 DIPs |
| Fill | #F3F3F3 |
| Border | #FFFFFF, 1 DIP |
| Icon | Clipboard arrow, #111111 |
| Accessibility | "Paste transcription" / "Coller la transcription" |
| Enabled | No (future wiring) |

## Processing State

Same layout as Recording. Waveform animates with slow breathing pulse (800–950ms periods) instead of the speech-like stagger. Both buttons remain disabled.

## Error State

Transitions immediately to Idle (three circles).

## Transitions

| Transition | Duration | Behavior |
|------------|----------|----------|
| Idle → Recording | 150ms | Fade/contract to active pill |
| Recording → Processing | 110ms | Keep layout, swap animation |
| Processing → Idle | 150ms | Fade back to three circles |

## Window Properties (all states)

- WindowStyle="None"
- AllowsTransparency="True"
- Background="Transparent"
- ResizeMode="NoResize"
- ShowInTaskbar="False"
- ShowActivated="False"
- Topmost="True"
- Focusable="False"
- WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW (always)
- WS_EX_TRANSPARENT (idle only)
