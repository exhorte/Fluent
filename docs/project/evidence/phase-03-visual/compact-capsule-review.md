# Compact Capsule Focused Review

Status: PASS_STATIC_REVIEW_AWAITING_WINDOWS_SMOKE

Date: 2026-07-15

## Scope

- `src/Fluent.App/Phase01/RecordingCapsuleWindow.xaml`
- `src/Fluent.App/Phase01/RecordingCapsuleWindow.xaml.cs`
- `src/Fluent.App/MainWindow.xaml.cs`

## Findings

- No blocking, high, or medium defect was found.
- `WM_NCHITTEST` returns `HTCAPTION` and `WM_MOUSEACTIVATE` returns `MA_NOACTIVATE`; existing non-activating styles remain applied.
- The `HwndSource` hook is attached once and removed on window close.
- Initial placement now occurs only at capsule construction, so state transitions no longer overwrite the dragged position.
- Storyboard keys, targets, begin calls, and removal behavior are unchanged.
- The final 140 by 40 DIP host provides 118 by 24 DIPs of inner layout space after margin, border, and padding. Idle content fits exactly in height and processing content fits with character ellipsis for long messages. The 26 DIP recording logo has a nominal 2 DIP vertical overhang into the available padding; WPF's default non-clipping layout and the user's real smoke confirm no visible clipping at the validated DPI.
- Width decreased by 24 percent and height by 38 percent; the idle icon decreased by 25 percent and text by 18 percent.

## Residual Verification

The user Windows smoke confirmed native movement, retained foreground text focus, stable placement across two recording cycles, and acceptable rendering at the user's DPI. Future DPI or layout changes must re-open this visual check.
