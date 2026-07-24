# Windows Text Insertion

Future spike topic: insert validated text into the original target.

Provisional ADR direction:

- Validate target identity with UI Automation.
- Insert via clipboard plus SendInput only when safe.
- Fall back to clipboard if the original target is gone or changed.

Hard constraints:

- Do not paste into password fields.
- Do not send Enter.
- Do not execute dictated commands.
