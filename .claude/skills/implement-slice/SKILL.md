---
name: implement-slice
description: Implement only one approved vertical slice from an active Fluent execution contract.
argument-hint: "<task-id> <slice>"
allowed-tools: Read Glob Grep Write Edit Bash PowerShell Agent
---

# Implement Slice

Refuse oversized or unapproved work.

## Steps

1. Read the active contract.
2. Confirm allowed paths and command families.
3. Make minimal changes.
4. Avoid unrelated refactors.
5. Record changed files and risks.

## Outputs

- Scoped implementation.
- Change summary.
- Evidence placeholders for verification.

## Failure Conditions

Scope creep, governance modification, or missing acceptance criteria.
