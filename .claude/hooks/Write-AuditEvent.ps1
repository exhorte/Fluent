param(
    [Parameter(Mandatory = $false)]
    [string] $InputJson
)

$ErrorActionPreference = "Stop"
$moduleRoot = Join-Path $PSScriptRoot "modules"
Import-Module (Join-Path $moduleRoot "HookJson.psm1") -Force
Import-Module (Join-Path $moduleRoot "PathSecurity.psm1") -Force
Import-Module (Join-Path $moduleRoot "AuditLedger.psm1") -Force

try {
    $hook = Read-FvHookInput -RawJson $InputJson
}
catch {
    ConvertTo-FvJson -Value ([ordered]@{}) -Compress | Write-Output
    exit 0
}

$root = Get-FvProjectRoot
$contract = Get-FvActiveTask -Root $root
$taskId = "NO_ACTIVE_TASK"
if ($null -ne $contract -and ($contract.PSObject.Properties.Name -contains "taskId")) {
    $taskId = [string]$contract.taskId
}

$action = ""
if ($hook.PSObject.Properties.Name -contains "tool_input" -and $null -ne $hook.tool_input) {
    $action = Protect-FvSecretText -Text (($hook.tool_input | ConvertTo-Json -Depth 8 -Compress))
}

Write-FvAuditEvent -Root $root -Event @{
    timestamp = Get-FvUtcNow
    event = [string]$hook.hook_event_name
    taskId = $taskId
    toolName = [string]$hook.tool_name
    action = $action
    decision = "OBSERVED"
    riskLevel = "low"
    reason = "Post-event audit entry."
    rules = @("audit")
} | Out-Null

ConvertTo-FvJson -Value ([ordered]@{}) -Compress | Write-Output
exit 0
