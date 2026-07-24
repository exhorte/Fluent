# Claude Code 2.1.207 Capability Notes

- Detected version: `2.1.207 (Claude Code)`.
- Official docs consulted: https://code.claude.com/docs/en/hooks and https://code.claude.com/docs/en/settings.
- Confirmed hook families used in this repo: `SessionStart`, `PreToolUse`, `PermissionRequest`, `PostToolUse`, `PostToolUseFailure`, `SubagentStart`, `SubagentStop`, `TaskCreated`, `TaskCompleted`, `Stop`, `ConfigChange`, `PreCompact`, `PostCompact`, `SessionEnd`.
- `PreToolUse` uses `hookSpecificOutput.permissionDecision`.
- `TaskCompleted` uses `continue: false` rather than `decision:block`.
- Agent and skill frontmatter was kept to documented fields used by Claude Code; no local official schema validator was available in this environment.
- Live hook activation should be verified in a fresh Claude Code session because this bootstrap ran under Codex.
