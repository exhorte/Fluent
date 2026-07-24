param(
    [Parameter(Mandatory = $false)]
    [string] $InputJson
)

$ErrorActionPreference = "Stop"
$moduleRoot = Join-Path $PSScriptRoot "modules"
Import-Module (Join-Path $moduleRoot "HookJson.psm1") -Force
Import-Module (Join-Path $moduleRoot "PathSecurity.psm1") -Force

try {
    $hook = Read-FvHookInput -RawJson $InputJson
}
catch {
    ConvertTo-FvJson -Value ([ordered]@{}) -Compress | Write-Output
    exit 0
}

$root = Get-FvProjectRoot
$path = Join-Path $root ".claude\runtime\compaction-context.json"
$summary = "PostCompact: reload CLAUDE.md, docs/project/project-state.md, active task contract, and Judge constitution."
if (Test-Path -LiteralPath $path) {
    $summary = "PostCompact restored context from .claude/runtime/compaction-context.json. Reload canonical project documents before mutable work."
}

ConvertTo-FvJson -Value (New-FvAdditionalContextOutput -HookEventName ([string]$hook.hook_event_name) -Context $summary) -Compress | Write-Output
exit 0
