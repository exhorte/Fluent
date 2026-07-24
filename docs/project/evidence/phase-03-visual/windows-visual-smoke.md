# Phase 03 Windows Visual Smoke

Status: PASS_USER_CONFIRMED

Date: 2026-07-15

## Automated Attempt

The required Windows-control workflow was checked through the session-provided `computer-use` capability. Its client module exists at:

`C:\Users\exhorte\.codex\plugins\cache\openai-bundled\computer-use\26.707.72221\scripts\computer-use-client.mjs`

However, the JavaScript control tool required to initialize that client is not exposed in this session. The workflow therefore cannot control or inspect the running Windows application.

The workflow explicitly forbids fallback foreground keyboard or mouse automation, so no unsafe substitute was used.

## Completed Evidence

- WPF XAML compiles in Release with no warning or error.
- Complete automated suite passes 35 / 35.
- Focused review confirms the native drag message handling, hook cleanup, in-session position retention, bounded Storyboard lifecycle, and preserved no-activate styles.
- The final source uses the user-accepted 140 by 40 DIP host. Its 118 by 24 DIP inner slot fits idle and processing content; the 26 DIP recording logo has a nominal 1 DIP extension into vertical padding on each side, with no visible clipping reported in the user's smoke.
- Static transition review confirms idle -> recording -> processing -> idle and late-progress guards.

## Required User Smoke

1. Close any older Fluent process and start `src/Fluent.App/bin/Release/net10.0-windows/Fluent.App.exe`.
2. Confirm the idle capsule is visibly smaller and still says `Fluent` without clipped content.
3. Focus a normal text field, drag the capsule to a new position, and confirm the caret and foreground target remain unchanged.
4. Press Ctrl+Space and confirm the compact seven-bar waveform animates without resetting the capsule position or moving focus.
5. Press Ctrl+Space again and confirm the waveform changes to processing only after microphone stop, then returns to `Fluent` at the same position.
6. Repeat once to prove the position is retained and the animation lifecycle does not accumulate.
7. Confirm the dictated text still reaches the original target correctly.

No phase-closure claim is made before this user result.

## User Result

On 2026-07-15, after the rebuilt Release executable and the seven-step compact capsule smoke were provided, the user replied: `c'est bon on peut continuer`.

This explicit confirmation closes the pending visual finding for compact rendering, mouse drag, retained foreground text focus, stable in-session placement across state transitions and repeated recording cycles, animation behavior, and successful text delivery. Technical phase closure remains subject to the final Development Judge verdict.
