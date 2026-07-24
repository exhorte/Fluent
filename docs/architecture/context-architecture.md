# Context Architecture

Canonical project context is versioned and lives in:

- `CLAUDE.md`
- `.claude/rules/`
- `.claude/judge/`
- `.claude/schemas/`
- `.claude/templates/`
- `docs/`
- ADRs
- execution contracts
- evidence reports
- audit ledger entries

Claude Code skills provide procedural context on demand. Agents provide role-specific instructions. Auto memory is not canonical and cannot replace documents.

Compaction context may summarize the current phase and task, but the source of truth remains the repository files.
