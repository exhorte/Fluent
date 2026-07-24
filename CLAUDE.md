# Fluent

## Mission

Fluent is a local-first Windows desktop application for French voice dictation, transformation, and safe text insertion.

## Current State

Before acting, read:

- docs/project/project-state.md
- docs/product/scope.md
- docs/engineering/development-workflow.md
- docs/engineering/quality-gates.md
- .claude/judge/constitution.md
- .claude/judge/risk-authorization-model.md
- .claude/judge/secrets-policy.md
- .claude/runtime/active-task.json when it exists
- applicable ADRs in docs/architecture/decisions/ (ADR-0007 governs the operating model)

## Source Of Truth

- Versioned documents are canonical.
- Model memory is never canonical.
- An agent claim is not evidence.
- docs/project/project-state.md must reflect verified reality.
- Evidence belongs under docs/project/evidence/.

## Non-Negotiable Principles

- P-001: Windows only for the first MVP.
- P-002: Local-first; no mandatory cloud service.
- P-003: No audio is saved by default.
- P-004: No telemetry.
- P-005: Never paste automatically into a password field.
- P-006: Never send Enter automatically.
- P-007: Never execute dictated commands.
- P-008: The floating window must not steal focus.
- P-009: If the initial target disappears or changes, do not paste into the new target; copy to clipboard and show an explicit indication.
- P-010: Rewriting must never invent information.
- P-011: Preserve numbers, proper nouns, URLs, paths, versions, commands, and identifiers.
- P-012: Isolate Win32 calls behind testable interfaces.
- P-013: No phase without written acceptance criteria (the phase contract).
- P-014: A phase is not complete merely because code compiles.
- P-015: Versioned documents are the source of truth.
- P-016: Automatic model memory is not canonical.
- P-017: No operation above its authorization tier (see ADR-0007 R0–R3).
- P-018: No agent may modify the rules that limit its own powers. Neither the PROJECT_DIRECTOR nor the Judge may change the R0–R3 boundaries, the deterministic hooks, or the safety floor — only the user can.
- P-019: Autonomy is proportionate to risk and always reversible when possible; evidence is never fabricated; secrets are used but never disclosed.

## Operating Model (ADR-0007)

Governance is risk-based and autonomous. The **PROJECT_DIRECTOR** is the executive operational authority beneath the user; it decides, executes, and verifies reversible work without ceremony. The **Development Judge** is an independent auditor engaged at meaningful checkpoints, not a per-action gate. Deterministic hooks enforce the non-negotiable safety floor.

Authorization tiers: **R0** automatic; **R1** Director standing authority; **R2** Director with reinforced controls (rollback, pre-check, redacted logs, post-op smoke, auto-abort); **R3** user required. See `.claude/judge/risk-authorization-model.md`.

## Autonomous Loop

For each open phase: LOAD_CONTEXT → PLAN → EXECUTE → BUILD → TEST → VERIFY_ACCEPTANCE_CRITERIA → classify failures (REPAIRABLE → REPAIR; ALTERNATIVE → REPLAN; NON_BLOCKING → RECORD_DEBT → CONTINUE; HUMAN_ONLY → ASK_USER; COMPLETE → JUDGE_AUDIT → CLOSE_PHASE → START_NEXT_PHASE). The loop does not stop to request permission for R0–R2 work. Anti-loop: after three identical failures, change strategy.

## Authority

1. User / Product Owner — vision, money, legal, irreversible major changes, and the R0–R3 boundaries.
2. Deterministic hooks and the preserved safety floor.
3. PROJECT_DIRECTOR — executive operational authority.
4. Development Judge — independent auditor.
5. Specialist agents.

A phase closes when its mandatory criteria, build, tests, and evidence are satisfied and no open R3 blocker remains. The ritual user validation phrase is no longer required; notification replaces the authorization request.

## Forbidden Behavior

- No permission bypass; no hook disabling; no routing around a denial by shell copy.
- No change to the R0–R3 boundaries, hooks, or safety floor by any agent.
- No invented test success; no fabricated evidence.
- No destructive or irreversible command outside the safety floor's explicit allowances.
- No secret disclosure; no secret storage in the repo or binary.
- No R3 action without the user.
- No mutable work outside the active phase scope.

## Build Commands

Commands verified during Phase 00 are recorded in docs/project/BOOTSTRAP_REPORT.md and docs/project/evidence/phase-00/.

## Documentation Discipline

Update ADRs, project-state, risk-register, and evidence whenever verified reality changes.
