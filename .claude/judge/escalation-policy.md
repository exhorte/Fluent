# Human Escalation Policy

Authority: ADR-0007. Under the risk-based model, human escalation (ASK_USER) is an **exception** reserved for genuine R3 actions. Everything at R0–R2 is decided and executed by the PROJECT_DIRECTOR without a user request.

## Escalate only for (R3)

- E-001: Fundamental product decision not covered by the existing vision.
- E-002: Payment, subscription, or new financial commitment; card entry.
- E-003: Legal or contractual acceptance; identity verification; legal consent.
- E-004: CAPTCHA.
- E-005: MFA requiring the user; account choice when no session exists.
- E-006: Irreversible deletion of production data.
- E-007: Permanent account closure.
- E-008: Public brand publication without prior authorization.
- E-009: Change to the R0–R3 boundaries, deterministic hooks, or safety floor.
- E-010: Windows system authorization that cannot be automated (registry, admin install).
- E-011: Important subjective visual check or hardware test (e.g., microphone) that only the user can judge.
- E-012: Genuine technical impossibility after reasonable strategies are exhausted.

## Do NOT escalate for

Ordinary class/method/organization, bug fix, local refactor, test, build, lint, format, local Git, local commit, work-branch push, draft PR, technical-equivalent choice, compile error, test failure, ordinary package evaluation, automatically repairable debt, dev/staging deploy, pre-authorized production deploy with rollback, using an already-configured account or secret, reversible migration, phase closure whose criteria are met, or starting the next phase. Agent low confidence is not a reason.

## Before escalating

Run the seven-step check in `.claude/judge/risk-authorization-model.md`. Escalate only if it truly cannot be resolved automatically.

## Required Format

```text
ESCALADE HUMAINE

Task ID :
Opération :
Catégorie d’escalade :
Pourquoi l’action ne peut pas être décidée automatiquement :
Impact :
Options :
Option recommandée :
Réponse minimale attendue :
```

Ask exactly one precise question. Accept a minimal answer (PASS/FAIL, yes/no, or a simple selection); do not require a ritual phrase.
