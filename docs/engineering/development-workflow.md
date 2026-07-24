# Development Workflow

Authority: ADR-0007. The workflow is the autonomous risk-based loop driven by the PROJECT_DIRECTOR. It replaces the per-action contract-and-verdict gate of the bootstrap model.

## Per-phase loop

1. LOAD_CONTEXT — read canonical documents, active phase contract, project-state, applicable ADRs.
2. PLAN — order tasks; choose the recommended option among equivalents; record decisions.
3. EXECUTE — implement within the active phase scope (R0/R1). Amend the phase contract as needed while the objective is unchanged and risk stays below R3.
4. BUILD.
5. TEST.
6. VERIFY_ACCEPTANCE_CRITERIA against the phase contract.
7. FAILURE_CLASSIFICATION:
   - REPAIRABLE → REPAIR → TEST.
   - ALTERNATIVE_AVAILABLE → REPLAN → EXECUTE.
   - NON_BLOCKING → RECORD_DEBT → CONTINUE.
   - HUMAN_ONLY (R3) → ASK_USER (single question).
   - COMPLETE → JUDGE_AUDIT (recommended) → CLOSE_PHASE → START_NEXT_PHASE.
8. Update project-state and evidence; create a clean Git point when relevant; announce closure.

## What does not stop the loop

Fixing a test, rerunning a build, editing an in-scope file, closing a subtask, advancing a step, using an already-configured account or secret, a work-branch push, or a draft PR. None require user approval.

## When the loop stops

Only for a genuine R3 action (see `.claude/judge/escalation-policy.md`) or when no reasonable automatable strategy remains. Anti-loop: keep a failure signature; after three identical failures, change strategy; bound retries; use timeouts.

## Judge audit

Recommended at phase closure, production deployment, destructive migration, significant security/auth change, and publication. The Judge returns `ALLOW`, `ALLOW_WITH_DEBT`, or `BLOCK_CRITICAL`. The Director may proceed on `ALLOW_WITH_DEBT` and may contest a `BLOCK_CRITICAL` once with new evidence.
