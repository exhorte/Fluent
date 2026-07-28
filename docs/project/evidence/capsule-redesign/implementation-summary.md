# Capsule Redesign — Implementation Summary

Status: IMPLEMENTED

Date: 2026-07-28

## What Changed

The old recording capsule (dark branded pill with Fluent logo, text labels, and a drag-enabled surface) has been replaced with a minimal, vector-based two-state design built entirely in WPF XAML.

### Old Design → New Design

| Aspect | Old | New |
|--------|-----|-----|
| Idle | 26×26 Fluent icon on dark semi-transparent background | Three discrete circles (○○○), #B4B4B4 border, #B8000000 fill |
| Recording | 7 animated bars, dark background | Compact pill: Cancel button + 13-bar waveform + Paste button |
| Processing | Fluent icon + "Traitement…" text + 3 dots | Compact pill with slow breathing waveform animation |
| Window size | 104×38 DIP (variable) | 126×34 DIP (active), auto-sized (idle) |
| Drag | WM_NCHITTEST→HTCAPTION (entire surface) | Removed (idle is click-through) |

### New Visual States

Four explicit `CapsuleVisualState` values:
- **Idle** — three circles, click-through, no interaction
- **Recording** — compact pill with active waveform animation
- **Processing** — compact pill with slow pulse animation, buttons disabled
- **Error** — transitions immediately to Idle

### State Mapping

```
PushToTalkState          → CapsuleVisualState
Idle, Cancelled          → Idle
Arming, Recording        → Recording
Stopping, Transcribing,
Rewriting, Inserting     → Processing
Failed                   → Error
```

## Files Modified

1. `src/Fluent.App/Phase01/RecordingCapsuleWindow.xaml` — Complete redesign
2. `src/Fluent.App/Phase01/RecordingCapsuleWindow.xaml.cs` — New state management, click-through, RelayCommand
3. `src/Fluent.App/MainWindow.xaml.cs` — CapsulePositionService integration, DPI-aware positioning
4. `src/Fluent.App/Localization/LocalizedStrings.cs` — Accessibility strings

## Files Created

5. `src/Fluent.App/Models/CapsuleVisualState.cs` — Public enum
6. `src/Fluent.App/Models/CapsuleLayoutMetrics.cs` — Centralized design tokens
7. `src/Fluent.App/Models/CapsuleStateMapper.cs` — Pure function mapping
8. `src/Fluent.App/Services/ICapsulePositionService.cs` — Position service interface
9. `src/Fluent.App/Services/CapsulePositionService.cs` — Multi-monitor DPI-aware positioning
10. `tests/Fluent.IntegrationTests/CapsuleStateMapperTests.cs` — 10 tests
11. `tests/Fluent.IntegrationTests/CapsuleLayoutMetricsTests.cs` — 18 tests
12. `tests/Fluent.IntegrationTests/CapsulePositionCalculationTests.cs` — 10 tests

## What Was NOT Changed

- Whisper engine
- Transcription pipeline
- Rewrite profiles
- Cloud authentication
- Dictionary
- History
- Hotkey mechanism
- Text insertion logic
- Password field detection
- Clipboard management
