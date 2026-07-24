---
name: close-phase
description: Close a Fluent phase autonomously when its mandatory criteria, build, tests, and evidence are satisfied. No ritual user validation phrase; notify instead.
argument-hint: "<phase-id>"
allowed-tools: Read Glob Grep Write Edit Bash PowerShell Agent
---

# Close Phase

Authority: ADR-0007 and `docs/engineering/quality-gates.md` (CG-001..CG-008). The PROJECT_DIRECTOR closes a phase; the user is not asked for a ritual phrase.

## Steps

1. Gather all phase evidence under `docs/project/evidence/`.
2. Verify closure gates CG-001..CG-008 (criteria, build, tests, evidence, risks, no open R3 blocker, deterministic completion gate, Judge audit).
3. Run a Judge audit for closure (recommended). Proceed on `ALLOW` or `ALLOW_WITH_DEBT`; a `BLOCK_CRITICAL` stops closure (may be contested once with new evidence).
4. Fold any previously recorded manual verification (e.g., a user-reported smoke test) into evidence without a second confirmation.
5. Update project-state and roadmap; create a clean Git point when relevant.
6. Announce closure and start the next planned unblocked phase.

## Stop only if

A mandatory criterion, build, or test is unmet; evidence is missing; a critical risk is unhandled; or an open R3 blocker remains.

## Outputs

- Closure record (criteria, tests, residual risks/debt).
- Judge verdict.
- Updated project-state; next phase started or reason it is blocked.
