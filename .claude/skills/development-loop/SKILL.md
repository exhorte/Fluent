---
name: development-loop
description: Run the Fluent autonomous risk-based loop from context sync through implementation, verification, audit, repair, and phase closure, driven by the PROJECT_DIRECTOR.
argument-hint: "<phase-id>"
allowed-tools: Read Glob Grep Write Edit Bash PowerShell Agent
---

# Development Loop

Authority: ADR-0007 and `docs/engineering/development-workflow.md`.

## Steps

1. LOAD_CONTEXT — canonical docs, phase contract, project-state, ADRs.
2. PLAN — order tasks; pick the recommended option among equivalents; log decisions.
3. EXECUTE — implement within the active phase scope (R0/R1); amend the contract as allowed.
4. BUILD.
5. TEST.
6. VERIFY_ACCEPTANCE_CRITERIA.
7. FAILURE_CLASSIFICATION: REPAIRABLE → REPAIR; ALTERNATIVE → REPLAN; NON_BLOCKING → RECORD_DEBT → CONTINUE; HUMAN_ONLY → ASK_USER; COMPLETE → JUDGE_AUDIT → CLOSE_PHASE → START_NEXT_PHASE.
8. Update project-state and evidence; clean Git point when relevant.

## Does not stop for

Fixing a test, rerunning a build, editing an in-scope file, closing a subtask, work-branch push, or draft PR.

## Stops for

A genuine R3 action, or when no reasonable automatable strategy remains. Anti-loop: after three identical failures, change strategy.

## Outputs

- Changed files, verification evidence, audit/verdict, updated project-state.
