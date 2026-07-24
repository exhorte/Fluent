param(
    [Parameter(Mandatory = $false)]
    [string] $InputJson
)

$ErrorActionPreference = "Stop"
$moduleRoot = Join-Path $PSScriptRoot "modules"
Import-Module (Join-Path $moduleRoot "HookJson.psm1") -Force

try {
    $hook = Read-FvHookInput -RawJson $InputJson
}
catch {
    ConvertTo-FvJson -Value ([ordered]@{ continue = $false; stopReason = $_.Exception.Message }) -Compress | Write-Output
    exit 0
}

$subject = ""
if ($hook.PSObject.Properties.Name -contains "task_subject") {
    $subject = [string]$hook.task_subject
}

if ($subject -notmatch '^\[FV-P[0-9]{2}-T[0-9]{3}\]\s+.+') {
    ConvertTo-FvJson -Value ([ordered]@{ continue = $false; stopReason = "Task subject must match [FV-PXX-TYYY] Description." }) -Compress | Write-Output
    exit 0
}

ConvertTo-FvJson -Value ([ordered]@{}) -Compress | Write-Output
exit 0
