# Phase 06A Windows Profile-Selection Smoke

Status: PENDING USER EXECUTION
Date prepared: 2026-07-18

Binary: `src\Fluent.App\bin\Release\net10.0-windows\Fluent.App.exe` (close any older instance first).

## Steps

1. Launch the application. Verify the header shows `Profil · Français professionnel` and the sidebar `Profils` badge shows `PRO`.
2. Open the `Profils` page. Verify three cards: `Français professionnel` (`ACTIF · SESSION`), `Développeur` (`DISPONIBLE`), `Français simplifié` (`INDISPONIBLE`, button disabled, reason shown), and the `SESSION · NON ENREGISTRÉ` badge.
3. In Notepad, dictate a normal French sentence with `Ctrl+Espace` … `Ctrl+Espace`. Verify punctuation normalization still works and the result text mentions the Professional profile.
4. Select `Utiliser ce profil` on `Développeur`. Verify the header, the sidebar badge (`DÉV`), and the Overview chip update.
5. Dictate a technical phrase (for example a version number, a path, or a command). Verify the inserted text is the exact transcription (after dictionary), with no added final period and no punctuation changes.
6. Start a recording, switch the profile on the Profils page during the recording, then stop. Verify the in-flight dictation still used the profile active when recording started, and only the next dictation uses the new profile.
7. Restart the application. Verify the selection is back to `Français professionnel` (session-only, nothing persisted).

## Result

- [x] PASS — the user confirmed the complete profile-selection smoke on 2026-07-19 with `c'est bon`, including the mid-recording profile switch (step 6) and the session-only reset after restart (step 7).
- [ ] FAIL — describe the defect observed.
