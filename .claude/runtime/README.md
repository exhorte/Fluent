# Runtime Directory

Claude Code hooks may write local runtime state here during a session:

- active task contract
- loop state
- audit ledger
- compaction context

Runtime files are ignored by Git except this README and `.gitkeep`.
