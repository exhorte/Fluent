# Hook Inventory

| Event | Matcher | Command | Args |
| --- | --- | --- | --- |
| ConfigChange | project_settings|local_settings|policy_settings|skills | pwsh | -NoProfile -ExecutionPolicy Bypass -File ${CLAUDE_PROJECT_DIR}\.claude\hooks\Protect-Governance.ps1 |
| PermissionRequest | * | pwsh | -NoProfile -ExecutionPolicy Bypass -File ${CLAUDE_PROJECT_DIR}\.claude\hooks\Invoke-HardSecurityGate.ps1 |
| PostCompact |  | pwsh | -NoProfile -ExecutionPolicy Bypass -File ${CLAUDE_PROJECT_DIR}\.claude\hooks\Restore-CompactionContext.ps1 |
| PostToolUse | * | pwsh | -NoProfile -ExecutionPolicy Bypass -File ${CLAUDE_PROJECT_DIR}\.claude\hooks\Write-AuditEvent.ps1 |
| PostToolUseFailure | * | pwsh | -NoProfile -ExecutionPolicy Bypass -File ${CLAUDE_PROJECT_DIR}\.claude\hooks\Write-AuditEvent.ps1 |
| PreCompact |  | pwsh | -NoProfile -ExecutionPolicy Bypass -File ${CLAUDE_PROJECT_DIR}\.claude\hooks\Save-CompactionContext.ps1 |
| PreToolUse | Bash|PowerShell|Write|Edit|Read|WebFetch|WebSearch|mcp__* | pwsh | -NoProfile -ExecutionPolicy Bypass -File ${CLAUDE_PROJECT_DIR}\.claude\hooks\Invoke-HardSecurityGate.ps1 |
| SessionEnd |  | pwsh | -NoProfile -ExecutionPolicy Bypass -File ${CLAUDE_PROJECT_DIR}\.claude\hooks\Write-AuditEvent.ps1 |
| SessionStart |  | pwsh | -NoProfile -ExecutionPolicy Bypass -File ${CLAUDE_PROJECT_DIR}\.claude\hooks\Initialize-SessionContext.ps1 |
| Stop |  | pwsh | -NoProfile -ExecutionPolicy Bypass -File ${CLAUDE_PROJECT_DIR}\.claude\hooks\Test-SessionStop.ps1 |
| SubagentStart | * | pwsh | -NoProfile -ExecutionPolicy Bypass -File ${CLAUDE_PROJECT_DIR}\.claude\hooks\Initialize-SessionContext.ps1 |
| SubagentStop | * | pwsh | -NoProfile -ExecutionPolicy Bypass -File ${CLAUDE_PROJECT_DIR}\.claude\hooks\Test-SubagentCompletion.ps1 |
| TaskCompleted |  | pwsh | -NoProfile -ExecutionPolicy Bypass -File ${CLAUDE_PROJECT_DIR}\.claude\hooks\Test-TaskCompletion.ps1 |
| TaskCreated |  | pwsh | -NoProfile -ExecutionPolicy Bypass -File ${CLAUDE_PROJECT_DIR}\.claude\hooks\Test-TaskCreation.ps1 |
