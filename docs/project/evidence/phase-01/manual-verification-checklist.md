# Phase 01 Manual Verification Checklist

Status: PENDING_MANUAL_EXECUTION

Date: 2026-07-12

## Build Used

Run:

```powershell
dotnet build Fluent.sln --no-restore
```

Executable:

```text
src/Fluent.App/bin/Debug/net10.0-windows/Fluent.App.exe
```

## Expected Fixed Text

```text
Fluent Phase 01 - texte fixe de verification.
```

## Common Procedure

1. Start `Fluent.App.exe`.
2. Put the caret in the target app text field.
3. Press `Ctrl+Space`.
4. Confirm the floating capsule appears and focus remains in the target app.
5. Press `Ctrl+Space` again.
6. Confirm fixed text is inserted.
7. Confirm no Enter key is sent.
8. Repeat with target changed between the two hotkey presses and confirm only clipboard fallback occurs.

## Scenarios

| Scenario | Status | Expected Result |
| --- | --- | --- |
| Notepad text field | PENDING | Fixed text inserted; no Enter sent. |
| Browser normal text field | PENDING | Fixed text inserted; no Enter sent. |
| Browser password field | PENDING | Nothing pasted or copied; blocked state shown. |
| VS Code editor | PENDING | Fixed text inserted; no Enter sent. |
| Windows Terminal prompt | PENDING | Fixed text pasted only; no Enter sent, command not executed. |
| Target changed before second Ctrl+Space | PENDING | Text copied to clipboard; no paste into new target. |

## Notes

These checks are intentionally manual because the quality gate concerns real Windows focus behavior and application-specific UI Automation providers.
