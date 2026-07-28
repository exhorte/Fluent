# Push-to-Talk Auto-Paste — Implementation Summary

Date: 2026-07-27
Branch: `chore/fluent-v1-consolidation`

## What Changed

Replaced the toggle-based Ctrl+Space global hotkey with a Push-to-Talk
Ctrl+Win low-level keyboard hook that automatically transcribes and pastes
when the user releases either key.

## Architecture

```
User holds Ctrl+Win
    ↓
WH_KEYBOARD_LL hook detects both modifiers
    ↓
PushToTalkKeyStateMachine: Idle → Arming
    ↓
(200 ms minimum hold timer)
    ↓
PushToTalkKeyStateMachine: Arming → Recording
    ↓
Audio capture starts
    ↓
User releases Ctrl or Win
    ↓
PushToTalkKeyStateMachine: Recording → Stopping
    ↓
Audio capture stops → transcription → rewriting → insertion
    ↓
PushToTalkKeyStateMachine: Stopping → Transcribing → Rewriting → Inserting → Idle
```

## Files Created

| File | Purpose |
|---|---|
| `src/Fluent.Core/Interaction/PushToTalkState.cs` | State enum (Idle, Arming, Recording, Stopping, Transcribing, Rewriting, Inserting, Failed, Cancelled) |
| `src/Fluent.Core/Interaction/PushToTalkEvent.cs` | Event enum (StartRequested, StopRequested, MinimumHoldReached, etc.) |
| `src/Fluent.Core/Interaction/PushToTalkKeyStateMachine.cs` | Thread-safe deterministic FSM, zero Win32 dependency |
| `src/Fluent.Core/Interaction/PushToTalkConfiguration.cs` | Centralised constants (200ms min hold, 250ms min recording, 5min max, 50ms processing delay) |
| `src/Fluent.Windows/Hotkeys/IGlobalPushToTalkHook.cs` | Abstraction over WH_KEYBOARD_LL hook |
| `src/Fluent.Windows/Hotkeys/GlobalPushToTalkHook.cs` | WH_KEYBOARD_LL implementation: detects Ctrl+Win combo, fires StartRequested/StopRequested events |
| `src/Fluent.Windows/Clipboard/IClipboardManager.cs` | Clipboard save/restore abstraction |
| `src/Fluent.Windows/Clipboard/ClipboardToken.cs` | Opaque token for clipboard integrity verification |
| `src/Fluent.Windows/Clipboard/WindowsClipboardManager.cs` | Save clipboard before auto-paste, restore after, detect concurrent modification via SHA-256 fingerprint |
| `tests/Fluent.Core.Tests/Interaction/PushToTalkKeyStateMachineTests.cs` | 25 tests covering all state transitions |
| `tests/Fluent.Core.Tests/Interaction/PushToTalkConfigurationTests.cs` | 4 tests for constant values |
| `tests/Fluent.Windows.Tests/PushToTalkHookTests.cs` | 6 automated tests + 4 skipped (require message pump) |

## Files Modified

| File | Change |
|---|---|
| `src/Fluent.App/MainWindow.xaml.cs` | Replaced GlobalHotKey + HandleHotKey toggle with IGlobalPushToTalkHook + PushToTalkKeyStateMachine. Added clipboard save/restore in InsertTranscript. Removed DictationState enum. Added push-to-talk event handlers, arming timer, max duration enforcement. |
| `src/Fluent.App/Localization/LocalizedStrings.cs` | Updated home.shortcut.value, dictation.recording.started, dictation.model.ready, home.lastResult.default. Added 4 new push-to-talk state strings. |

## Capsule Visual States

- **Idle**: Fluent logo (unchanged)
- **Arming**: Shows "Hold…" / "Maintenez…" (idle visual)
- **Recording**: Wave animation + "Recording… Release Ctrl+Win to finish"
- **Processing**: Pulsing dots + status text (unchanged)

## Clipboard Policy

Before auto-paste:
1. Current clipboard content is saved with a SHA-256 fingerprint
2. Dictation result is placed on clipboard
3. Ctrl+V is injected via SendInput
4. Previous clipboard content is restored
5. Concurrent modification is detected via fingerprint comparison

## Test Results

- Build: 0 warnings, 0 errors
- Core Tests: 75/75 (including 25 new state machine + 4 config tests)
- Windows Tests: 10/10 + 4 skipped (hook install requires message pump)
- Speech Tests: 10/10
- Audio Tests: 6/6
- Rewrite Tests: 172/172
- Backend Tests: 54/54
- Persistence Tests: 44/44
- Integration Tests: 88/88
- **Total: 459 passed / 0 failed / 4 skipped**

## Known Limitations

1. Ctrl+Win is a modifier-only combination; RegisterHotKey cannot detect it.
   WH_KEYBOARD_LL is required.
2. The Start Menu suppression is implemented via `ShouldSuppressWinKey()` but
   must be wired into the hook callback's return value for full suppression.
   The current implementation suppresses in the `_activeSequence` context but
   the WndProc integration for suppression is a manual verification step.
3. The hook callback runs on the UI thread; while it is kept minimal (key state
   update + event fire), complex processing is deferred.
4. Hook install/uninstall tests are skipped in automated tests (require a
   Win32 message pump); verified via the manual smoke checklist.
