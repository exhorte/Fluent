# ADR-0007: Risk-Based Autonomous Project Governance

Status: Accepted

Date: 2026-07-23

Supersedes operational parts of ADR-0003 (agent governance). ADR-0003 remains the record of the original bootstrap model.

> Accepted on 2026-07-23 after verification: rules installed under `.claude/**`; verdict schema validated (test G-003); governance tests pass 45/45 (evidence `docs/project/evidence/governance-migration-2026-07-23/hook-tests.json`); PROJECT_DIRECTOR agent installed (G-002); Judge converted to auditor (G-004); R0–R3 deterministic rules applied and tested (non-force push R1, deploy contract-gated R1/R2, registry/admin ASK, floor intact). "Accepted" means the decision is adopted and in force; the first end-to-end phase run under the model is its runtime demonstration.

## Context

The original governance model (ADR-0003) placed a read-only Development Judge and a mandatory execution contract in front of nearly every mutable action. It was correct for Phase 00 bootstrap, but in day-to-day phase work it required a contract, a Judge verdict, or an explicit user validation for operations that an agent can reasonably decide, execute, and verify alone: local fixes, builds, tests, documentation, reversible migrations, work-branch pushes, and phase closure whose criteria were already met.

The result was frequent stalls of the form "the phase stays open because the next step needs your explicit authorization" even when the step was reversible and already covered by prior decisions. The user has explicitly authorized replacing this model with a risk-proportionate autonomous one on 2026-07-23.

## Decision

Adopt a four-tier, risk-based authorization model driven by an executive **PROJECT_DIRECTOR** agent, with the Development Judge repositioned as an **independent auditor** rather than a per-action gate. Deterministic hooks remain the non-negotiable safety floor.

### Hierarchy

1. USER / PRODUCT OWNER — vision, money, legal, irreversible major changes.
2. PROJECT_DIRECTOR — principal operational authority; decides, orders, executes, and closes reversible work.
3. DEVELOPMENT_JUDGE — independent auditor; verifies criteria, tests, security, regressions, scope, evidence quality.
4. SPECIALIST AGENTS — scoped implementation.
5. TOOLING AND AUTOMATION WORKERS.

### Why the Project Director

A single accountable executive role removes the "who may decide this?" ambiguity that produced escalations. The Director makes the reversible technical and product decisions needed to advance a phase, documents them, and keeps them traceable. It is the authority directly beneath the user.

### Why the Judge becomes an auditor

Independent verification is valuable; a mandatory pre-action gate on reversible work is not. The Judge now audits at meaningful checkpoints (phase closure, production deploy, destructive migration, auth/security change, public publication) and emits `ALLOW`, `ALLOW_WITH_DEBT`, or `BLOCK_CRITICAL`. It blocks only genuine hazards. Minor observations become recorded debt, not stop conditions.

## Risk Tiers

| Tier | Who authorizes | Requirement | Examples |
| --- | --- | --- | --- |
| **R0 Automatic** | nobody | none | read, search, build, test, lint, format, edit within active scope, docs, local commit/branch, dev SQLite migration, evidence update, subtask closure |
| **R1 Director standing authority** | PROJECT_DIRECTOR | decision documented | work-branch `git push`, draft PR, dev/staging deploy, use of an already-configured account, browser/cowork use, opening apps, env var configuration, automatable smoke tests, phase closure when criteria met, starting the next phase |
| **R2 Director with reinforced controls** | PROJECT_DIRECTOR | rollback + pre-check + redacted logs + post-op smoke + auto-abort | pre-authorized production deploy, non-destructive migration, Cloud config change, existing-secret change, controlled key rotation with all consumers updated, reversible access-policy change, private prerelease |
| **R3 User required** | USER | ASK_USER (single question) | payment or new financial commitment, card entry, legal/contractual acceptance, CAPTCHA, human MFA, identity verification, irreversible production-data deletion, permanent account closure, unpre-authorized public brand publication, fundamental product decision outside existing vision, genuine technical impossibility after reasonable strategies are exhausted |

R3 is an exception, not a default. Agent low confidence is not a sufficient reason for R3. Before escalating, the Director must verify: the action is truly impossible with available tools; no prior user decision already covers it; no safe reversible default exists; no offline simulation is possible; no alternative strategy remains; the action cannot be deferred without blocking the phase.

## Secrets Policy: USE_BUT_NEVER_DISCLOSE

Authorized agents may detect `.env` files, read needed variables, use API keys, configure services, modify variables when needed, and create `.env.local` from an example. They must never display a full secret value, copy a secret into a reply, place a secret in evidence or an exception, log `Authorization`, commit a `.env`, or embed a secret in the binary. Reports show only the variable name, its status, and at most the last four characters when strictly necessary. Secret scanning stays mandatory before any push or release. A sensitive file no longer triggers ASK_USER automatically; ASK_USER applies only when the secret does not exist and cannot be created with available tools, or requires human MFA, payment, or a legal commitment.

## Automatic Phase Closure

The PROJECT_DIRECTOR may close a phase when: mandatory criteria are met; the mandatory build passes; mandatory tests pass; evidence exists; critical risks are handled; limitations are documented; and no open R3 blocker remains. A previously performed and recorded manual verification may be folded into evidence without a second confirmation. The ritual sentence "Je valide la clôture de la phase X" is no longer required by default. After closure the Director updates project-state, roadmap, and evidence, creates a clean Git point when relevant, announces closure, and starts the next planned unblocked phase. Notification replaces the authorization request.

## Lightened Contracts

One contract per phase, containing only: objective, scope, exclusions, mandatory criteria, risks, validation strategy, rollback, and already-established product decisions. Bug fixes, extra tests, necessary refactors, documentation updates, equivalent implementation changes, retries, and smoke-test repairs do not require a new contract. The Director may amend the phase contract without the user as long as the objective is unchanged, risk does not rise to R3, no unplanned major feature is added, and no fundamental user decision is reversed.

## Autonomous Loop

Each phase runs a durable loop: LOAD_CONTEXT → PLAN → EXECUTE → BUILD → TEST → VERIFY_ACCEPTANCE_CRITERIA → FAILURE_CLASSIFICATION → {REPAIRABLE → REPAIR; ALTERNATIVE → REPLAN; NON_BLOCKING → RECORD_DEBT → CONTINUE; HUMAN_ONLY → ASK_USER; COMPLETE → JUDGE_AUDIT → CLOSE_PHASE → START_NEXT_PHASE}. The loop continues while mandatory criteria are unmet and an automatic repair, an alternative strategy, an automatable test, or producible evidence remains. It does not stop to request permission to fix a test, rerun a build, edit an in-scope file, close a subtask, or advance a step.

## Anti-Infinite-Loop

Maximum three identical repetitions of one strategy; after three identical failures, change strategy. Keep a failure signature; do not repeat an action without new information; bound network retries; use timeouts; record non-blocking debt; interrupt only when no reasonable strategy remains.

## Rollback and Audit

R2 operations require a rollback or backup, a pre-check, redacted logs, a post-operation smoke test, and automatic abort on a critical anomaly. Every significant decision is documented, traceable, and proportionate to risk. The deterministic audit ledger continues to record every gated tool call.

## Preserved Safety Floor (non-negotiable)

These are enforced by deterministic hooks and are **not** modifiable by the PROJECT_DIRECTOR or the Judge — only by the user:

- Secret and certificate file reads are denied.
- `git push --force`, `git reset --hard`, destructive `git clean`, recursive delete, and disk format are denied.
- Writes outside the repository are denied.
- Permission-bypass and hook-disabling are denied.
- No fabricated evidence; no test success without real output.
- The R0–R3 boundaries, the deterministic hooks, and this safety floor can be changed only by the user. No agent may expand its own authority. This preserves principle P-018.

## Consequences

Positive: phases advance without ceremony; reversible operations are automatic; the user is interrupted only for genuinely human, irreversible, financial, or legal actions; the Judge still provides independent verification at the checkpoints that matter.

Negative: more authority concentrated in the PROJECT_DIRECTOR; the highest residual risk is autonomous R2 production deploy and key rotation, mitigated by mandatory rollback, smoke tests, auto-abort, and a recommended Judge audit.

## Reversibility

Fully reversible. All changes are versioned documents and configuration. Restoring the ADR-0003 model requires reverting these documents. Tightening or loosening a tier requires a user-approved edit to the risk-authorization model and this ADR.
