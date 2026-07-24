# ADR-0003: Agent Governance

Status: Superseded (operationally) by ADR-0007 on 2026-07-23

Date: 2026-07-12

> The read-only-Judge-as-gatekeeper and mandatory-per-action-contract model below is superseded by ADR-0007 (risk-based autonomous governance). The deterministic hooks, audit ledger, and safety floor described here are retained. This ADR remains the record of the original bootstrap model.

## Context

The project requires autonomous engineering support without allowing the model to weaken its own constraints.

## Decision

Use a read-only Development Judge, deterministic PowerShell hooks, specialized worker agents, execution contracts, audit evidence, and explicit human escalation boundaries.

## Consequences

Positive: clearer authority, safer automation, reproducible review loop.

Negative: more bootstrap complexity and extra maintenance for hooks, schemas, and evidence.

## Reversibility

Reducing guardrails requires user governance approval and a new ADR.
