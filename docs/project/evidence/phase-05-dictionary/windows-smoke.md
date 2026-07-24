# Phase 05A Windows Smoke

Status: PASS - USER CONFIRMED

## Preconditions

- Start `Fluent.App` normally.
- Keep the Dictionary page visible long enough to confirm the `SESSION · NON ENREGISTRÉ` disclosure.

## Steps

1. Open **Dictionnaire** from the sidebar and confirm the empty state and a count of zero.
2. Add `nix voice` as the recognized form and `Fluent` as the replacement.
3. Search for `nix` and confirm that only the real session entry remains visible.
4. Return to **Vue d'ensemble**, place the cursor in a normal text field, and dictate a sentence containing “nix voice”.
5. Stop dictation and confirm that `Fluent` is delivered while the original target and no-Enter protections remain unchanged.
6. Return to **Dictionnaire**, delete the entry, and confirm the empty state and zero count.
7. Dictate the same phrase again and confirm that the deleted correction is no longer applied.
8. Close and relaunch Fluent; confirm the page is empty because Phase 05A intentionally stores entries for the current session only.

## Result

On 2026-07-16, the user explicitly confirmed the requested Phase 05A smoke with `c’est bon`.

Result: PASS. Navigation, session-only disclosure, dictionary correction, deletion, and non-persistence behavior are accepted by the user.
