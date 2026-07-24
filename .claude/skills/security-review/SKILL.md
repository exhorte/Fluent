---
name: security-review
description: Run a read-only Fluent security and privacy review focused on secrets, password fields, clipboard, audio, command safety, logs, and exfiltration.
argument-hint: "<task-id>"
allowed-tools: Read Glob Grep Agent Write
---

# Security Review

## Steps

1. Read threat model and changed files.
2. Invoke `security-reviewer`.
3. Check product principles P-003 through P-012.
4. Record findings with severity and evidence.

## Outputs

- Security review report.
- Required fixes.

## Failure Conditions

No file references, missing privacy checks, or success declared without evidence.
