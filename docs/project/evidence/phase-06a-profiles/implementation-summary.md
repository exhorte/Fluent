# Phase 06A Implementation Summary - Selectable Local Profiles

Task: FV-P06-T001
Date: 2026-07-18
Plan Judge: ALLOW (2026-07-18) with three advisory conditions.

## What was implemented

- `RewriteProfile` now carries truthful availability metadata (`IsAvailable`, `Description`, `UnavailableReason`) through optional parameters, so the accepted read-only test baseline keeps compiling unchanged.
- `RewriteProfiles` is the canonical local catalog: Professional French (default, unchanged deterministic behavior), Developer (available), Simplified French (visibly unavailable with an honest local-semantic-engine reason). `IsCanonicalAvailable` uses reference equality so fabricated value-equal profile objects are never canonical; `TryGetAvailable` matches IDs ordinally and only for available profiles.
- `ProfileSelection` provides validated session-only selection starting at Professional French on every launch; nothing is persisted or logged. `ProfileSelectionResult` reports success and a truthful French message.
- `DeveloperPassThroughRewriter` returns the exact post-dictionary source text.
- `ProfileRoutedRewriter` routes deterministically by reference equality to the Professional rewriter or the Developer pass-through; every other profile object fails the route, which `SafeProfileRewriteService` (unchanged) converts into the accepted exact-source fallback.
- `MainWindow` captures an immutable selected-profile snapshot when recording starts (after target-safety checks), uses it through dictation completion, and clears it in `finally` and `ResetToIdle`. All dictation status texts now name the actual profile. The header discloses `Profil · <name>` separately from `Moteur Base Q8 · CPU`, the sidebar has a real Profils navigation entry with a live PRO/DÉV badge, and the Overview foundations chip shows the actual selected profile.
- `ProfilesPage` lists only the canonical catalog with truthful badges (`ACTIF · SESSION`, `DISPONIBLE`, `INDISPONIBLE`), a disabled Simplified French card with its reason, a `SESSION · NON ENREGISTRÉ` badge, and an explicit note that a change during recording applies to the next dictation.

## Verification

- Focused profile tests: Rewrite 41 / 41, Integration 4 / 4 (both `~Profile` filters matched non-zero counts per the plan-judge condition).
- Release solution build: 0 warnings, 0 errors (`dotnet-build.log`).
- Complete suite: 184 / 184 (`dotnet-test.log`), including the unchanged accepted baselines.
- Two transient SDK out-of-memory failures during builds were environmental and disappeared on retry with identical source.

## Unchanged protections

Target locking, password-target blocking, unverified/changed-target fallback, clipboard-only fallback, no-Enter, shutdown cancellation, in-memory-only audio, and persistent-dictionary behavior are untouched; the profile snapshot is applied strictly between the dictionary step and insertion.

## Open items

- Focused reviews (rewrite architecture/safety, WPF truthfulness/snapshot, security/privacy/tests) recorded in `focused-review.md`.
- Final Development Judge evidence verdict.
- User Windows profile-selection smoke (`windows-profile-smoke.md`) before product-phase closure.
