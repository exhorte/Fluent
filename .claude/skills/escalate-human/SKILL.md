---
name: escalate-human
description: Produce the single-question Fluent human escalation for a genuine R3 action, after the seven-step R3 check has failed to resolve it automatically.
argument-hint: "<task-id> <category>"
allowed-tools: Read Glob Grep Write
disable-model-invocation: true
---

# Escalate Human

Authority: ADR-0007 and `.claude/judge/escalation-policy.md`. Use only for a genuine R3 action. First run the seven-step R3 check in `.claude/judge/risk-authorization-model.md`; escalate only if it cannot be resolved automatically. Agent low confidence is not a reason.

Produce exactly:

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

Ask one precise question only. Accept a minimal answer (PASS/FAIL, yes/no, or a simple selection); do not require a ritual phrase.
