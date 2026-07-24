# Protected Assets

Authority: ADR-0007. These paths hold the governance and safety floor. They may be changed only by the user (E-009), never by the PROJECT_DIRECTOR, the Judge, or a specialist agent expanding its own authority. This preserves P-018.

## Governance-protected paths

- `CLAUDE.md`
- `.claude/settings.json`
- `.claude/judge/**` (constitution, risk-authorization-model, secrets-policy, escalation-policy, risk-classification, competence-boundaries, protected-assets, command-policy)
- `.claude/hooks/**`
- `.claude/schemas/**`
- `.claude/agents/development-judge.md`
- `.claude/agents/project-director.md`
- `docs/engineering/quality-gates.md`
- `docs/engineering/definition-of-done.md`
- `.git/**`

## Secret-protected paths (read denied)

- `.env`, `.env.*`
- `secrets/**`, `credentials/**`
- `*.key`, `*.pem`, `*.pfx`, `*.p12`, `*.cer`, `*.crt`

Governance-protected paths cannot be modified by worker agents. Changes require human governance authority (the `FV_HUMAN_APPROVED_GOVERNANCE_CHANGE` flag and an explicit user decision). The PROJECT_DIRECTOR operates freely within R0–R2 everywhere else, but the tier boundaries themselves live here and are outside its authority.
