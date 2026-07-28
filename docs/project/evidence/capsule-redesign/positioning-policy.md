# Capsule Redesign — Positioning Policy

## Default Position

- **Horizontal**: Centered on the target monitor's work area
- **Vertical**: Near the bottom, above the taskbar
- **Bottom offset**: 20 DIPs from the work area bottom edge

## Monitor Selection Priority

1. Monitor containing the foreground window
2. Monitor containing the cursor
3. Primary monitor (fallback)

## DPI Handling

- `GetDpiForMonitor` (shcore.dll) retrieves the DPI of the target monitor
- Physical pixel coordinates from `GetMonitorInfo` are divided by the DPI scale to get DIPs
- Fallback to 1.0 (96 DPI) if `GetDpiForMonitor` is unavailable

## Repositioning Triggers

- Window `Loaded` event
- Window `SizeChanged` event
- State transitions that change the window dimensions

## Clamping

Position is clamped to the work area bounds to ensure the capsule never:
- Overlaps the taskbar
- Extends beyond screen edges

## Multi-Monitor Support

- Negative monitor coordinates (left/above primary) are handled correctly
- Each monitor's independent taskbar position is respected via `rcWork`
- The capsule follows the target application across monitors

## Implementation

`CapsulePositionService` in `src/Fluent.App/Services/CapsulePositionService.cs`. Implements `ICapsulePositionService`.

Win32 APIs used:
- `GetForegroundWindow`
- `MonitorFromWindow` / `MonitorFromPoint`
- `GetMonitorInfo`
- `GetCursorPos`
- `GetDpiForMonitor`
