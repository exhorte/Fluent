# Capsule Redesign — Manual Smoke Checklist

> ⚠️ Requires human validation on a real Windows desktop.
> Run `dotnet run --project src/Fluent.App` to launch.

## Scenario 1 — Startup

- [ ] Launch Fluent
- [ ] Wait for "Ready" state

**Expected:**
- [ ] Three circles visible (○○○)
- [ ] No Fluent logo visible
- [ ] Capsule centered horizontally
- [ ] Capsule near bottom of screen (~20px above taskbar)
- [ ] Taskbar not obscured
- [ ] Not in Alt+Tab
- [ ] Not in taskbar
- [ ] No focus stolen from any window

## Scenario 2 — Click-through (Idle)

- [ ] Place Notepad behind the three circles
- [ ] Click where the circles appear

**Expected:**
- [ ] Click reaches Notepad behind
- [ ] Fluent does not gain focus
- [ ] Capsule does not move

## Scenario 3 — Recording Start

- [ ] Place cursor in Notepad
- [ ] Press and hold Ctrl+Win
- [ ] Wait for the recording state

**Expected:**
- [ ] Three circles disappear
- [ ] Compact pill appears (Cancel + waveform + Paste)
- [ ] Waveform animates (bars moving at different speeds)
- [ ] Focus remains in Notepad
- [ ] Pill is centered
- [ ] No activation of the capsule window

## Scenario 4 — Recording End

- [ ] Release Ctrl+Win
- [ ] Wait for transcription and insertion

**Expected:**
- [ ] Capsule stays in pill form during processing
- [ ] Animation slows down (breathing pulse)
- [ ] Text appears in Notepad
- [ ] Capsule returns to three circles
- [ ] Position is still correct

## Scenario 5 — 125% DPI Scaling

- [ ] Set Windows display scaling to 125%
- [ ] Repeat scenarios 1–4

**Expected:**
- [ ] Circles are perfectly round (not oval)
- [ ] Borders are crisp
- [ ] Capsule is centered correctly
- [ ] No blur or pixelation

## Scenario 6 — 150% DPI Scaling

- [ ] Set Windows display scaling to 150%
- [ ] Repeat scenarios 1–4

**Expected:**
- [ ] Same quality checks as 125%

## Scenario 7 — Secondary Monitor

- [ ] Move Notepad to secondary monitor
- [ ] Start a dictation

**Expected:**
- [ ] Capsule appears on the same monitor as Notepad
- [ ] Centered correctly on that monitor
- [ ] Respects that monitor's taskbar
- [ ] Works even if monitor has negative coordinates

## Scenario 8 — Taskbar Positions

Test with taskbar at:
- [ ] Bottom (default)
- [ ] Top
- [ ] Left
- [ ] Right

**Expected for all:**
- [ ] Capsule always inside the work area
- [ ] No overlap with taskbar
- [ ] Bottom offset (~20px) maintained

## Scenario 9 — Error Recovery

- [ ] Force an error (e.g., start dictation with no microphone)
- [ ] Or let a normal dictation fail

**Expected:**
- [ ] No infinite animation
- [ ] Capsule returns to three circles
- [ ] New dictation possible after error

## Scenario 10 — Shutdown

- [ ] Close Fluent

**Expected:**
- [ ] Capsule disappears completely
- [ ] No residual process
- [ ] No click-through remnants
- [ ] No animation running
