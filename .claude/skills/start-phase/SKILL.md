---
name: start-phase
description: Start a Fluent phase by producing its lightweight phase contract (objective, scope, exclusions, mandatory criteria, risks, validation, rollback) and entering the autonomous loop.
argument-hint: "<phase-id> <objective>"
allowed-tools: Read Glob Grep Write Edit Bash PowerShell Agent
---

# Start Phase

Authority: ADR-0007. The PROJECT_DIRECTOR may start the next planned unblocked phase without a user request.

## Inputs

- Phase id and objective.
- Product constraints and applicable ADRs.

## Steps

1. Read canonical context and project-state.
2. Write the lightweight phase contract: objective, scope, exclusions, mandatory acceptance criteria, risks, validation strategy, rollback, and already-established product decisions.
3. Enter the autonomous loop (see `docs/engineering/development-workflow.md`).

## Notes

- A Judge plan audit is optional at start; it is recommended only for phases touching production, security/auth, destructive migration, or publication.
- Escalate (R3) only if the phase requires a fundamental product decision not covered by the vision, or another genuine R3 action.

## Outputs

- Phase contract in `.claude/runtime/active-task.json` (or the phase document).
- Ordered task list and required evidence list.
