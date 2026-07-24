---
name: update-project-state
description: Update Fluent project-state with only proven facts: implemented, tested, pending, blocked, decisions, and next action.
argument-hint: "<task-id>"
allowed-tools: Read Glob Grep Write Edit
---

# Update Project State

Update only facts proven by files, evidence, user decisions, or test output.

Record:

- Implemented.
- Tested.
- Pending.
- Blocked.
- Decisions.
- Next action.

Do not mark a phase closed without user validation.
