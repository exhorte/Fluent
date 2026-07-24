---
name: verify-slice
description: Verify an approved Fluent slice with restore, build, tests, static checks, format checks, and applicable manual harness evidence.
argument-hint: "<task-id>"
allowed-tools: Read Glob Grep Bash PowerShell Write
---

# Verify Slice

## Steps

1. Read required tests from the contract.
2. Run restore/build/tests only as needed.
3. Capture command, timestamps, exit code, and summary.
4. Store structured evidence.
5. Report failures without hiding them.

## Outputs

- Evidence JSON or Markdown.
- Command logs.
- Verification summary.

## Failure Conditions

Missing command output, non-zero exit hidden, or evidence not written.
