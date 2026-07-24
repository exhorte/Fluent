# Phase 06A Focused Reviews

Task: FV-P06-T001
Date: 2026-07-18
Repair cycles used: 1 / 2

## Review 1 - Rewrite architecture and safety: PASS

- No blocking or major finding.
- Confirmed: additive backward-compatible `RewriteProfile` extension; safe static-initialization order of the catalog; `ReferenceEquals` routing rejects fabricated value-equal profiles (a look-alike object with a canonical `Id` cannot route); cancellation propagates at every layer without being swallowed into a fallback; the Developer pass-through preserves Unicode, CRLF, code, URLs, paths, versions, numbers, and identifiers by construction; Developer output can never fail `RewriteOutputValidator` (identical strings); unknown routes convert into `RawFallbackRewriterFailed` with the verbatim source, satisfying P-010/P-011.
- MINOR (repaired): `ProfileSelection` thread-safety assumption was implicit; an explicit "Not thread-safe: UI-thread only" doc line was added.
- MINOR (accepted, no change): the default arm of the navigation-badge switch is defensive for a future third available profile; full card-list recreation on selection is trivial for three static profiles.

## Review 2 - WPF truthfulness and profile snapshot: PASS

- No blocking or major finding.
- Confirmed: the snapshot is captured only after the missing/password/unusable-target checks; `CompleteDictationAsync` reads the snapshot into a local once, so a mid-recording selection change provably cannot affect the in-flight dictation; the snapshot is cleared on every exit path (`finally`, `ResetToIdle`, shutdown); event subscriptions are unhooked symmetrically in `OnClosed`; the disabled Simplified French card cannot raise selection and `TryGetAvailable` rejects it anyway; `SESSION · NON ENREGISTRÉ` and `Profil · <name>` labels match the implementation truthfully; no UI-thread blocking or cross-thread WPF access.
- Residual gap (accepted per plan-judge condition 3 and repo precedent): no WPF-level automated test exercises the real `MainWindow` object graph; the mid-recording-switch invariant is covered by the integration-level equivalent-logic test and by the mandatory user Windows smoke step 6.

## Review 3 - Security, privacy, and tests: PASS

- No blocking or major finding; all seven contract checks verified by direct code reading of every changed file plus the unchanged `TextInsertionPolicy` baseline: no persistence, no transcript or profile logging, no network/process/Ollama additions, target-check ordering and insertion-policy integrity, Developer pass-through performs no execution (P-007), canonical-catalog-only routing defeats fabricated profiles, and no secrets or telemetry.
- MINOR (pre-existing, out of slice): `DescribeTarget` surfaces the target window title verbatim; predates Phase 06A, unchanged by this contract, tracked for awareness.
- MINOR (accepted): three suggested extra tests (app-level capture-ordering, explicit no-side-effect assertion for adversarial command-like strings, UI-handler-level rejection of non-canonical Tags) exceed the bounded slice; the equivalent invariants are covered at the unit/integration level and by the Windows smoke.

## Verification after repair

The doc-only repair does not change behavior; the focused profile tests and complete suite results in `dotnet-test.log` (41/41, 4/4, 184/184) and the Release build (0 warnings, 0 errors) remain the evidence of record, re-confirmed by a post-repair build.
