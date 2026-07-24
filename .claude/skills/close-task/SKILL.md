---
name: close-task
description: Verify a Fluent task's criteria, evidence, tests, and project state, then close it. Evidence-based, fail-closed; no ritual user phrase.
argument-hint: "<task-id>"
allowed-tools: Read Glob Grep Agent Write Edit
---

# Close Task

Authority: ADR-0007. The PROJECT_DIRECTOR closes a task on evidence.

## Checks

- Phase contract active and the task's criteria satisfied.
- Acceptance criteria proven by evidence.
- Required tests passed (evidence stored).
- Reviews complete where applicable.
- Evidence exists under `docs/project/evidence/`.
- Project state updated.
- No critical finding open.

## Outputs

- Closure report.
- Judge verdict when a checkpoint audit applies (`ALLOW`, `ALLOW_WITH_DEBT`, or `BLOCK_CRITICAL`). `ALLOW_WITH_DEBT` is non-blocking; record the debt.

Closure fails closed when evidence is missing. No user validation phrase is required.
