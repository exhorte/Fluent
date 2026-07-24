# Risk-Based Authorization Model (R0–R3)

Authority: ADR-0007. This document is the canonical operational authorization model for Fluent phase work. It is a preserved safety asset: only the user may change the tier boundaries.

## R0 — Automatic (no approval)

The PROJECT_DIRECTOR and specialist agents act without any approval:

- Repository read and code search.
- Internal architecture consistent with ADRs.
- Edit within the active scope.
- Documentation.
- Build, tests, lint, format, auto-fix, local refactor.
- Development SQLite migrations.
- `git status`, `git diff`, `git add`, `git commit`, local branch creation.
- Opening Fluent, opening a terminal, automated UI/browser tests.
- Redacted log collection, evidence update, subtask closure.

## R1 — PROJECT_DIRECTOR standing authority (no user request)

The Director decides and executes; the decision is documented:

- `git push` to a work branch (non-force).
- Create or update a pull request (draft by default).
- Deployment to development or staging.
- Use of an already-configured external account or connected session.
- Browser and cowork use, opening applications.
- Environment-variable configuration.
- Use of existing keys; creation of free or already-planned resources.
- Cloud smoke tests; service restart; deployment rollback.
- Phase closure when criteria are met; starting the next phase.

## R2 — PROJECT_DIRECTOR with reinforced controls

Permitted without user validation only when a plan, evidence, and rollback exist. Mandatory: backup or rollback, pre-check, redacted logs, post-operation smoke test, automatic abort on a critical anomaly. A Judge audit is recommended.

- Deployment to an already-authorized production environment.
- Non-destructive migration.
- Cloud configuration change.
- Change to an existing secret.
- Controlled key rotation when all consumers are updated.
- Reversible access-policy change.
- Private prerelease.
- External operation with limited, reversible user impact.

## R3 — User intervention required (ASK_USER)

Reserved for genuinely human or irreversible actions:

- Payment or new financial commitment; card entry.
- Legal or contractual acceptance; identity verification; legal consent.
- CAPTCHA; MFA requiring the user.
- Irreversible deletion of production data.
- Permanent account closure.
- Public brand publication without prior authorization.
- Fundamental product decision not covered by existing vision.
- Genuine technical impossibility after reasonable strategies are exhausted.

Agent low confidence alone is not a reason for R3.

## Before any R3 escalation

The Director verifies, in order:

1. Is the action truly impossible with available tools?
2. Does a prior user decision already cover it?
3. Can a safe, reversible option be chosen automatically?
4. Does a reasonable default exist?
5. Is an offline simulation or test possible?
6. Can another strategy be tried?
7. Can the action be deferred without blocking the phase?

If escalation is unavoidable, ask a single question stating: what is blocked, why the agent cannot act, the impact, the recommended option, and the minimal expected answer (PASS/FAIL, yes/no, or a simple selection).

## Relationship to the deterministic floor

The deterministic hooks enforce a conservative floor beneath this model. Regardless of tier they DENY: secret and certificate reads, `git push --force`, `git reset --hard`, destructive `git clean`, recursive delete, disk format, out-of-repository writes, and permission bypass. They still ASK once for Windows registry modification and machine-level installation, because those affect the user's machine rather than the project's reversible cloud state. External deployment tools (terraform/kubectl/vercel/netlify/az) are allowed only when the active phase contract records the target in `allowedCommandFamilies` — this makes R1/R2 authorization non-fabricable without a live user prompt. This model may read as more permissive than the floor; the floor always wins.
