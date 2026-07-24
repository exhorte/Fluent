# Quality Gates

Phase 00 is ready for user review only when:

- QG-001: Required tree exists.
- QG-002: `CLAUDE.md` is concise and coherent.
- QG-003: Product documents exist.
- QG-004: Architecture documents exist.
- QG-005: Initial ADRs exist.
- QG-006: Agents are valid and discoverable.
- QG-007: Development Judge is read-only.
- QG-008: Skills are valid.
- QG-009: Hooks read stdin JSON.
- QG-010: Hooks produce valid JSON.
- QG-011: Destructive commands are blocked.
- QG-012: Protected paths are blocked.
- QG-013: Secrets are inaccessible.
- QG-014: Safe commands can be allowed.
- QG-015: External operations escalate.
- QG-016: Closure without evidence is blocked.
- QG-017: Repair loop is documented.
- QG-018: Audit ledger is valid.
- QG-019: .NET solution compiles if SDK is available.
- QG-020: Initial .NET tests pass.
- QG-021: Harness tests pass.
- QG-022: Bootstrap report exists.
- QG-023: `project-state.md` reflects reality.
- QG-024: No business feature was developed.
- QG-025: No dangerous permission was enabled.

Phase 00 status remains `IMPLEMENTED_AWAITING_USER_REVIEW` until the user closes it. (Phase 00 is a historical bootstrap gate; the criteria above are retained as a record.)

## Phase Closure (ADR-0007)

From Phase 01 onward, the PROJECT_DIRECTOR may close a phase — no ritual user validation phrase required — when ALL of the following hold:

- CG-001: Mandatory acceptance criteria in the phase contract are met.
- CG-002: The mandatory build passes (evidence stored).
- CG-003: Mandatory tests pass (evidence stored).
- CG-004: Evidence exists under `docs/project/evidence/`.
- CG-005: Critical risks are handled; limitations are documented.
- CG-006: No open R3 blocker remains.
- CG-007: The deterministic completion gate passes (active contract, no critical open finding, `verification.testsPassed = true`).
- CG-008: A Judge audit was run for closure and returned `ALLOW` or `ALLOW_WITH_DEBT` (recommended; recorded debt is non-blocking).

A previously performed and recorded manual verification (e.g., a visual or hardware smoke test the user already reported) may be folded into evidence without a second confirmation. After closure the Director updates project-state and roadmap, creates a clean Git point when relevant, announces closure, and starts the next planned unblocked phase. Notification replaces the authorization request.

Governance/floor changes (this file, `.claude/**` governance, hooks, schemas, R0–R3 boundaries) remain R3 — user only.
