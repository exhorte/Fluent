---
name: create-execution-contract
description: Create the lightweight phase contract that scopes an autonomous Fluent phase. One contract per phase, amendable by the PROJECT_DIRECTOR while the objective is unchanged.
argument-hint: "<phase-id>"
allowed-tools: Read Glob Grep Write Edit Bash PowerShell Agent
---

# Create Execution Contract

Authority: ADR-0007. Micro-contracts are gone: one lightweight contract per phase. The PROJECT_DIRECTOR owns it and may amend it while the objective is unchanged, risk stays below R3, no unplanned major feature is added, and no fundamental user decision is reversed.

## Contract contents (only these)

- Objective.
- Scope and exclusions.
- Mandatory acceptance criteria.
- Risks.
- Validation strategy.
- Rollback.
- Already-established product decisions.
- `allowedCommandFamilies` (including any authorized deploy target for R1/R2).

## Steps

1. Read `.claude/templates/active-task.example.json` and project-state.
2. Populate the phase contract fields above.
3. Ensure forbidden paths override allowed paths and that deploy targets, if any, are explicitly recorded (the deterministic gate requires it).
4. Store the contract in `.claude/runtime/active-task.json`.

## Does NOT require a new contract

Bug fix in the phase, extra test, necessary refactor, documentation update, equivalent implementation change, retry, or smoke-test repair.

## Notes

- A Judge plan audit is optional; recommended only for production/security/destructive/publication work.
- Human approval is never fabricated by an agent; genuine R3 items are escalated via `escalate-human`.

## Outputs

- Phase contract JSON path.
