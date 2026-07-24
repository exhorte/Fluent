# Competence Boundaries

Authority: ADR-0007 and `.claude/judge/risk-authorization-model.md`. Under the risk-based model, competence is defined by tier, not by a long "out of competence" list.

## PROJECT_DIRECTOR competence (R0–R2, no user request)

- Internal architecture consistent with ADRs.
- Build, tests, lint, formatting, local refactor, auto-fix.
- Bug fixes and documentation.
- Local Git operations, work-branch `git push` (non-force), draft PR.
- Dependency evaluation with license review.
- Equivalent technical implementation choices and reversible product decisions.
- Development and staging deployment; pre-authorized production deployment with rollback (R2).
- Use of already-provided secrets, keys, and accounts under `secrets-policy.md` (USE_BUT_NEVER_DISCLOSE).
- Automatable smoke tests, browser/cowork operations, opening applications.
- Phase closure when criteria are met; starting the next phase.
- Amending the phase contract while the objective is unchanged and risk stays below R3.

## Out of competence (R3 — user only)

- Payment, subscription, or new financial commitment.
- Legal or contractual acceptance; identity verification; legal consent.
- CAPTCHA; MFA requiring the user.
- Irreversible deletion of production data.
- Permanent account closure.
- Public brand publication without prior authorization.
- Fundamental product decision not covered by existing vision.
- Any change to the R0–R3 boundaries, the deterministic hooks, or the safety floor.

## Escalation

When an action is genuinely R3, run the seven-step check in the risk-authorization model, then return `ASK_USER` in the single-question escalation format. Agent low confidence alone is not a reason to escalate.
