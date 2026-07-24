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
$contract = Get-FvActiveTask -Root $root
$taskId = "none"
$status = "no active contract"
if ($null -ne $contract) {
    $taskId = [string]$contract.taskId
    $status = [string]$contract.status
}

$context = "NyxVoice context: root=$root; activeTask=$taskId; contractStatus=$status; canonical docs: CLAUDE.md, docs/project/project-state.md, .claude/judge/constitution.md."
ConvertTo-FvJson -Value (New-FvAdditionalContextOutput -HookEventName ([string]$hook.hook_event_name) -Context $context) -Compress | Write-Output
exit 0
