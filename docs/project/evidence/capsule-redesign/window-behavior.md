# Capsule Redesign — Window Behavior

## Window Styles (always)

```
WindowStyle="None"
AllowsTransparency="True"
Background="Transparent"
ResizeMode="NoResize"
ShowInTaskbar="False"
ShowActivated="False"
Topmost="True"
Focusable="False"
SizeToContent="WidthAndHeight"
```

## Win32 Extended Styles

### All States
- `WS_EX_NOACTIVATE` — prevents window activation on click
- `WS_EX_TOOLWINDOW` — hides from Alt+Tab and taskbar

### Idle State Only
- `WS_EX_TRANSPARENT` — click-through; mouse events pass to windows below

Styles are toggled dynamically via `SetWindowLongPtr`.

## WndProc

- `WM_MOUSEACTIVATE` → `MA_NOACTIVATE` — never activate
- No `WM_NCHITTEST` → `HTCAPTION` (drag removed; idle is click-through)

## Focus Guarantees

- Capsule never appears in Alt+Tab
- Capsule never appears in the taskbar
- Capsule never receives keyboard focus
- Idle state: all mouse events pass through to windows below
- Active state: mouse events are received for future button clicks, but the capsule still never activates

## Lifecycle

- Created once on first use, reused for all dictation sessions
- Closed on application shutdown
- All event subscriptions and animations are cleaned up on close
