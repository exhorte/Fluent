# Phase 05B Windows Restart Smoke

Status: PASS - USER ACCEPTED
Date prepared: 2026-07-16
Date completed: 2026-07-17

## Normal Persistence Smoke

1. Launch Fluent and open `Dictionnaire`.
2. Confirm the badge becomes `LOCAL · ENREGISTRÉ`.
3. Add a distinctive correction, for example `flux nyx` to `Fluent`.
4. Dictate the spoken form in a normal text field and confirm the replacement is inserted.
5. Close Fluent completely.
6. Restart Fluent.
7. Confirm the badge again becomes `LOCAL · ENREGISTRÉ` and the correction is still listed.
8. Dictate the spoken form again and confirm the persisted correction is still applied.
9. Update the replacement, restart once more, and confirm the updated value is loaded and applied.
10. Delete the correction, restart once more, and confirm it no longer appears or applies.

## Fallback Disclosure

Corrupt and locked database fallback is covered by isolated automated tests that never touch the user's real database. If storage is unavailable in real use, the page must display `SESSION · NON ENREGISTRÉ` with an error explanation while dictation remains usable.

## Acceptance Record

- Persistence after restart: PASS
- Update after restart: PASS
- Delete after restart: PASS
- Dictation correction unchanged: PASS
- User acceptance: PASS - the user replied `c'est bon phase suivante` on 2026-07-17.
