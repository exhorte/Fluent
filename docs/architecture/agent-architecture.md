# Agent Architecture

The harness separates responsibilities:

- Lead Engineer orchestrates work.
- Specialist agents analyze or implement within their remit.
- Development Judge reviews plans, actions, evidence, and closure in read-only mode.
- Deterministic hooks enforce hard security boundaries.

No worker agent can approve its own work. The Judge cannot modify governance assets. Hooks outrank model judgment for forbidden operations.
