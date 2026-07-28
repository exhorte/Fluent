# Push-to-Talk Known Limitations

## 1. Modifier-only hotkey limitation

Ctrl+Win is composed entirely of modifier keys. `RegisterHotKey` requires a
non-modifier virtual key code, which is why a `WH_KEYBOARD_LL` hook is
necessary.

## 2. Start Menu interaction

On Windows 11, releasing the Win key after holding Ctrl+Win during a
dictation may briefly show the Start Menu. The hook suppresses Win key
events during an active Push-to-Talk sequence, but edge cases exist:

- If the user releases Ctrl but keeps Win held, the Start Menu may appear
  before the hook processes the Win-up event.
- The hook blocks Win-key-down during active sequence but cannot retroactively
  suppress an already-dispatched Win key.

Mitigation: The `ShouldSuppressWinKey()` method returns true during active
sequences. The hook callback should return a non-zero value to suppress
the key event when this method returns true.

## 3. Hook requires a message pump

`SetWindowsHookEx(WH_KEYBOARD_LL, ...)` requires the installing thread to
have a Windows message pump. In WPF, this is automatically provided by the
application's dispatcher loop. In tests, a message pump is not available,
so install/uninstall tests are skipped.

## 4. Hook callback performance

The hook callback runs on the UI thread's message pump. While kept minimal
(key state update + event fire), any blocking operation in the callback
would freeze the UI. The current implementation only updates boolean flags
and fires events — all heavy processing is deferred.

## 5. Keyboard repeat

Windows generates repeated key-down messages when a key is held. The hook
filters these out using bit 30 of the flags field. Without this filter,
the state machine would receive spurious StartRequested events.

## 6. Clipboard concurrent modification

The clipboard manager uses a SHA-256 fingerprint to detect concurrent
modification. This is a best-effort mechanism — two different texts could
theoretically share the same first 8 hex characters of their SHA-256 hash
(probability: 1 in 2^32). In practice, this is sufficient for detecting
user-initiated clipboard changes.

## 7. Single instance

The current implementation does not prevent two Fluent instances from each
installing a WH_KEYBOARD_LL hook. Multiple low-level keyboard hooks can
coexist, but each adds event processing overhead. A future improvement
would detect an existing instance and prevent a second launch.

## 8. Elevated processes (UAC)

WH_KEYBOARD_LL hooks installed by a non-elevated process cannot monitor
keystrokes directed at elevated (admin) windows. This is a Windows security
boundary and is not a limitation specific to Fluent.
