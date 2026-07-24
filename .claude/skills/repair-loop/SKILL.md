---
name: repair-loop
description: Classify findings, repair approved issues, retest, and produce a new Fluent evidence report.
argument-hint: "<task-id>"
allowed-tools: Read Glob Grep Write Edit Bash PowerShell Agent
---

# Repair Loop

## Steps

1. Classify findings: critical, major, minor, information.
2. Repair only approved findings.
3. Retest affected scope.
4. Update evidence.
5. Stop after max repair cycles and change strategy.

## Outputs

- Repair summary.
- Updated tests.
- New evidence.

## Failure Conditions

Repeated same failed strategy, unscoped repair, or critical finding left unaddressed without block state.
