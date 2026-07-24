# Development Loop

State machine:

```text
IDLE -> INTAKE -> CONTEXT_SYNC -> PLANNING -> PLAN_REVIEW
-> CONTRACT_APPROVED -> IMPLEMENTING -> VERIFYING -> REVIEWING
-> READY_TO_CLOSE -> COMPLETED
```

Alternative transitions:

- `PLAN_REVIEW -> REJECTED_PLAN -> PLANNING`
- `VERIFYING -> REWORK -> IMPLEMENTING`
- `REVIEWING -> REWORK -> IMPLEMENTING`
- any state -> `BLOCKED_TECHNICAL`
- any human-boundary state -> `WAITING_FOR_HUMAN`
- any critical error -> `HALTED`

Rules:

- No arbitrary transition.
- Every transition writes audit evidence.
- `COMPLETED` requires required evidence.
- `REWORK` increments the repair cycle.
- After three identical failures, choose a new strategy.
- Technical blocks remain system responsibility unless they require human authority.
