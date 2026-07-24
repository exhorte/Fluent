# Governance Migration Evidence — 2026-07-23

Migration from ADR-0003 (Judge-gatekeeper + mandatory per-action contract) to ADR-0007 (risk-based autonomous governance). Authorized explicitly by the user.

## Verification

- `hook-tests.json` — full deterministic suite, **45/45 PASS** (exit 0), including:
  - Updated: non-force `git push` classified/gated as **ALLOW** (R1); `T-022` hard gate allows non-force work-branch push.
  - New classifier tests: force push DENY (`--force`/`-f`/`--force-with-lease`); deployment ALLOW + RequiresContract; registry/admin ASK_USER.
  - New hard-gate tests: `T-030` deploy denied without recorded contract family; `T-031` deploy allowed when family recorded.
  - New governance tests `G-001..G-007`: policy files installed; PROJECT_DIRECTOR agent + ADR-0007 present; verdict enum is ALLOW/ALLOW_WITH_DEBT/BLOCK_CRITICAL and no longer DENY; Judge agent documents the auditor role; settings authorize non-force push while force stays denied; command-policy no longer human-gates push and protects the Director agent; constitution reserves boundary changes to the user and keeps the Judge read-only.
  - Preserved floor (unchanged, still PASS): `.env`/certificate read DENY, reset --hard / clean -fdx / recursive delete / chained-destructive DENY, malformed input fails closed, audit ledger redacts secrets.

## Preserved safety floor (not modifiable by any agent)

Secret reads, force-push, history rewrite, recursive/disk deletion, out-of-repo writes, permission bypass → DENY at every tier. R0–R3 boundaries, hooks, and floor changeable only by the user (E-009). No fabricated evidence; secrets used but never disclosed.

## Not machine-verified (honest limits)

- Behavioral tiers (Director running a full autonomous loop end-to-end; R3 escalation flows; anti-loop strategy change after 3 identical failures) are governed by agent behavior and reviewed, not unit-tested.
- The harness auto-mode classifier independently guards `.claude/**`; these edits required interactive user approval and the `FV_HUMAN_APPROVED_GOVERNANCE_CHANGE` flag.
