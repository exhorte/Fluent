---
name: judge-plan
description: Request an independent read-only Development Judge review of a plan without modifying it.
argument-hint: "<task-id>"
allowed-tools: Read Glob Grep Agent
---

# Judge Plan

## Inputs

- Plan.
- Contract.
- Acceptance criteria.
- Risk assessment.

## Steps

1. Ensure the plan is written and scoped.
2. Invoke `development-judge`.
3. Require `APPROVED`, `REJECTED`, or `NEEDS_REVISION`.
4. Store the verdict as evidence.

## Outputs

- Judge result.
- Required plan revisions if any.

## Failure Conditions

No contract, no acceptance criteria, or mutable changes attempted during review.
