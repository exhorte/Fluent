---
name: review-slice
description: Trigger code, security, documentation, and test review for an approved Fluent slice, then consolidate findings.
argument-hint: "<task-id>"
allowed-tools: Read Glob Grep Agent Write
---

# Review Slice

## Steps

1. Read changed files and evidence.
2. Invoke `code-reviewer`.
3. Invoke `security-reviewer`.
4. Invoke `test-engineer` when test strategy changed.
5. Consolidate findings without hiding disagreement.

## Outputs

- Review report.
- Findings by severity.
- Recommended repair plan.

## Failure Conditions

Reviewer lacks files, reports success without evidence, or critical findings are ignored.
