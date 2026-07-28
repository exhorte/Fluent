# Capsule Redesign — Known Limitations

## Not Implemented in This Batch

1. **Cancel and Paste buttons are not wired** — They render visually with correct styling but `IsEnabled="False"` and have no business logic connected. Properties `CancelCommand` and `PasteCommand` are exposed on `RecordingCapsuleWindow` for future wiring.

2. **No real audio level driving the waveform** — The waveform uses synthetic Storyboard-based animation. Integration with the audio pipeline (real RMS/peak levels) would require creating an `IRecordingLevelSource` abstraction, which is deferred.

3. **No respect for Windows animation preferences** — `SystemParameters.ClientAreaAnimation` is not checked. The waveform animates regardless of the user's "Show animations in Windows" setting.

4. **Window animation transitions are not yet implemented** — The `TransitionDurationMs` constants exist in `CapsuleLayoutMetrics` but the actual fade/scale transitions between states are deferred. State changes are immediate.

5. **No repositioning on display change or DPI change** — The capsule recalculates position on Loaded and SizeChanged, but does not yet listen for `WM_DPICHANGED` or `WM_DISPLAYCHANGE`. The position is correct on launch and after state transitions.

6. **Error state shows no visual indication** — `ShowErrorState()` transitions immediately to Idle (three circles). A brief error pulse or indicator was deferred.

## Planned for Future Batches

- Wire Cancel and Paste commands
- Hook real audio levels into waveform animation
- Add smooth crossfade/scale transitions between states
- Listen for DPI and display change events
- Add brief error visual indicator
- Respect Windows animation reduction setting
- Re-enable buttons when commands are wired
