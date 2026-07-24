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
    ConvertTo-FvJson -Value (New-FvBlockOutput -Reason $_.Exception.Message) -Compress | Write-Output
    exit 2
}

$root = Get-FvProjectRoot
$contract = Get-FvActiveTask -Root $root
$taskId = "none"
$phaseId = "none"
$objective = "none"
if ($null -ne $contract) {
    $taskId = [string]$contract.taskId
    $phaseId = [string]$contract.phaseId
    $objective = [string]$contract.objective
}

$context = [ordered]@{
    savedAt = Get-FvUtcNow
    hookEventName = [string]$hook.hook_event_name
    phaseId = $phaseId
    taskId = $taskId
    objective = $objective
    state = "see docs/project/project-state.md"
    filesTouched = @()
    testsExecuted = @()
    openFindings = @()
    nextAction = "reload canonical documents after compaction"
}

$path = Join-Path $root ".claude\runtime\compaction-context.json"
$context | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $path -Encoding UTF8

ConvertTo-FvJson -Value ([ordered]@{}) -Compress | Write-Output
exit 0
