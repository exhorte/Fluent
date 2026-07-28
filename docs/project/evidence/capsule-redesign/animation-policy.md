# Capsule Redesign — Animation Policy

## Recording Waveform

The recording waveform uses 13 vertical bars animated with staggered WPF `DoubleAnimation` on `ScaleTransform.ScaleY`:

### Parameters
- **Target**: Each bar's ScaleY (via named ScaleTransform)
- **From values**: 0.20–0.55 (randomized per bar)
- **To value**: 1.0 (full height)
- **Duration**: 260–440ms per bar (staggered)
- **AutoReverse**: True (bars bounce up and down)
- **RepeatBehavior**: Forever
- **BeginTime offset**: 0–170ms (creates natural wave pattern)

### Visual Feel
- Natural speech-like movement, not a metronomic equalizer
- Bars have rounded ends (RadiusX/Y = 1)
- White bars (#F5F5F6) on near-black background

## Processing Pulse

The processing state reuses the same bars but with a slower, calmer animation:

### Parameters
- **Target**: Every other bar (odd-numbered bars 1, 3, 5, 7, 9, 11, 13)
- **From values**: 0.35–0.45
- **To values**: 0.65–0.75
- **Duration**: 800–950ms
- **AutoReverse**: True
- **BeginTime offset**: 100–350ms

### Visual Feel
- Slow breathing, indicates background work
- Non-distracting, keeps the user informed without anxiety

## Animation Lifecycle

- Storyboards are created once as window resources
- `Begin()` with `HandoffBehavior.SnapshotAndReplace` on each state transition
- `Remove()` is called on the window before starting a new animation
- All Storyboards are stopped on window close
- No animation runs in Idle state

## Audio Level Source

Currently using synthetic visual animation (Storyboard-based). Future integration with real audio levels:
- Planned abstraction: `IRecordingLevelSource`
- Would drive ScaleY from real RMS/peak levels
- Max update rate: ~25 fps
- Levels are not stored, logged, or transmitted

## Windows Animation Preferences

Respecting `SystemParameters.ClientAreaAnimation` is planned for a future iteration. Currently, if the user has disabled animations in Windows, the waveform will still animate — this is a known limitation.
