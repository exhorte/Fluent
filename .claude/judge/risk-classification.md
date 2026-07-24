# Risk Classification

Authority: ADR-0007. This file maps operations to the four authorization tiers. The canonical tier definitions live in `.claude/judge/risk-authorization-model.md`; this is the quick lookup.

## R0 — Automatic

Reads, `git status`/`diff`/`log`, deterministic tests, build, lint, format, edit within active scope, documentation, local commit, local branch, dev SQLite migration, evidence update, subtask closure.

## R1 — PROJECT_DIRECTOR standing authority

Work-branch `git push` (non-force), draft PR, dev/staging deploy, use of an already-configured account or session, browser/cowork, opening apps, env-var configuration, existing-key use, Cloud smoke tests, service restart, deployment rollback, phase closure when criteria met, starting the next phase.

## R2 — PROJECT_DIRECTOR with reinforced controls

Pre-authorized production deploy, non-destructive migration, Cloud config change, existing-secret change, controlled key rotation, reversible access-policy change, private prerelease. Mandatory: rollback, pre-check, redacted logs, post-op smoke test, auto-abort. Judge audit recommended.

## R3 — User required (ASK_USER)

Payment, legal acceptance, CAPTCHA, human MFA, irreversible production-data deletion, account closure, unpre-authorized public brand publication, fundamental product decision, governance/floor change, non-automatable Windows authorization, subjective visual/hardware check, genuine technical impossibility.

## Deterministic floor (denied at every tier)

Secret reads, force-push, history rewrite, recursive/disk deletion, out-of-repository writes, permission bypass. Registry and admin install still ASK once. External deployment tools require the target recorded in the active contract.
