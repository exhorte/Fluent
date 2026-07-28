# Push-to-Talk Architecture Summary

## Layers

```
┌─────────────────────────────────────────────┐
│  Fluent.App (WPF)                           │
│  MainWindow.xaml.cs                         │
│  - PushToTalk event handlers                │
│  - Arming timer (200ms min hold)            │
│  - Max duration timer (5min)                │
│  - Clipboard save/restore                   │
│  - Capsule visual state updates              │
└──────────────┬──────────────────────────────┘
               │ depends on
┌──────────────▼──────────────────────────────┐
│  Fluent.Windows (Win32 layer)               │
│  GlobalPushToTalkHook (WH_KEYBOARD_LL)       │
│  - SetWindowsHookEx                         │
│  - CallNextHookEx                           │
│  - UnhookWindowsHookEx                      │
│  - VK_LCTRL / VK_RCTRL / VK_LWIN / VK_RWIN │
│  WindowsClipboardManager                    │
│  - System.Windows.Clipboard wrappers         │
└──────────────┬──────────────────────────────┘
               │ depends on
┌──────────────▼──────────────────────────────┐
│  Fluent.Core (pure logic)                   │
│  PushToTalkKeyStateMachine                  │
│  - Thread-safe deterministic FSM            │
│  - Zero Win32 dependency                    │
│  PushToTalkConfiguration                    │
│  - All tunable constants in one place       │
└─────────────────────────────────────────────┘
```

## Key Design Decisions

1. **Why WH_KEYBOARD_LL instead of RegisterHotKey?**
   RegisterHotKey requires a non-modifier virtual key code (e.g., VK_SPACE).
   It cannot detect a modifier-only combination like Ctrl+Win because there
   is no non-modifier key to register. WH_KEYBOARD_LL receives every keystroke
   and lets us track the state of individual modifier keys.

2. **Why a state machine in Fluent.Core?**
   The state machine has zero Win32 dependencies, making it fully testable
   with deterministic unit tests. The hook callback is kept minimal: it only
   tracks key state and fires events. All state transitions happen in Core,
   not in the hook callback.

3. **Why both minimum hold AND maximum duration?**
   Minimum hold (200ms) prevents accidental activations from brief key
   presses. Maximum duration (5min) prevents infinite recording if the user
   forgets to release the keys.

4. **Why save/restore clipboard?**
   The previous implementation overwrote the clipboard without saving the
   user's content. The new clipboard manager saves the previous content,
   fingerprints it, and restores it after the paste operation.
