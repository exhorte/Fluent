# Push-to-Talk Manual Smoke Checklist

This checklist must be run on Windows 11 with Fluent built in Release mode.

Prerequisites: `dotnet run --project src/Fluent.App` from the repo root.

## SCENARIO 1 — Notepad

- [ ] Open Notepad
- [ ] Place caret in the document
- [ ] Hold Ctrl + Win
- [ ] Dictate a phrase in French or English
- [ ] Release one of the two keys

Expected:
- [ ] Recording starts on hold (capsule shows wave animation)
- [ ] Recording stops on release
- [ ] Transcription is automatically pasted into Notepad
- [ ] No Enter key is sent (caret stays on same line)
- [ ] Start Menu does NOT open
- [ ] No extraneous characters appear

## SCENARIO 2 — Browser text field

- [ ] Open a browser (Chrome/Edge/Firefox)
- [ ] Click into any text field (e.g., search bar)
- [ ] Hold Ctrl + Win, dictate, release

Expected:
- [ ] Text is inserted into the field
- [ ] Focus stays in the field
- [ ] No browser shortcuts are triggered

## SCENARIO 3 — Password field

- [ ] Navigate to any login page
- [ ] Click into the password field
- [ ] Hold Ctrl + Win, try to dictate

Expected:
- [ ] Recording is blocked at start OR
- [ ] Recording starts but insertion is blocked
- [ ] No text appears in the password field
- [ ] A clear blocked message is shown

## SCENARIO 4 — VS Code

- [ ] Open VS Code with any file
- [ ] Place caret in the editor
- [ ] Hold Ctrl + Win, dictate a technical phrase, release

Expected:
- [ ] Text is inserted at cursor position
- [ ] Technical terms are preserved
- [ ] No VS Code commands are triggered inadvertently

## SCENARIO 5 — Target change during recording

- [ ] Start in Notepad
- [ ] Hold Ctrl + Win, start dictating
- [ ] Alt+Tab to another application while holding
- [ ] Release keys in the new application

Expected:
- [ ] No paste into the unexpected application
- [ ] Text is preserved on clipboard
- [ ] Message: "Target changed" or similar

## SCENARIO 6 — Brief press (< 200 ms)

- [ ] Very briefly tap Ctrl + Win (under 200ms)
- [ ] Do not dictate

Expected:
- [ ] No recording starts
- [ ] No transcription
- [ ] No paste
- [ ] Fluent returns to idle immediately
- [ ] Start Menu opens normally (brief tap < 200ms = not suppressed)

## SCENARIO 7 — Multiple rapid cycles

- [ ] Perform 3 consecutive dictations: hold → dictate → release → paste
- [ ] Verify each cycle completes independently

Expected:
- [ ] One paste per dictation
- [ ] No double-paste
- [ ] Hook does not get stuck
- [ ] Fluent stays in Idle between cycles

## SCENARIO 8 — App closed

- [ ] Close Fluent completely
- [ ] Test Ctrl + Win

Expected:
- [ ] Fluent no longer intercepts keys
- [ ] Start Menu opens normally (Win key alone)
- [ ] No residual Fluent.exe process
- [ ] System shortcuts are fully functional

## SCENARIO 9 — Left/Right variants

- [ ] Left Ctrl + Left Win → dictation works
- [ ] Left Ctrl + Right Win → dictation works (if hardware supports it)
- [ ] Right Ctrl + Left Win → dictation works
- [ ] Right Ctrl + Right Win → dictation works (if hardware supports it)

## SCENARIO 10 — Max duration (5 min)

- [ ] Hold Ctrl + Win for 5+ minutes without releasing
- [ ] Dictate periodically

Expected:
- [ ] Recording stops automatically after 5 minutes
- [ ] Transcription and paste proceed normally
- [ ] Fluent returns to idle
- [ ] No keys remain logically blocked
