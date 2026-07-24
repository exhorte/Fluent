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
    ConvertTo-FvJson -Value (New-FvBlockOutput -Reason $_.Exception.Message) -Compress | Write-Output
    exit 2
}

$message = ""
if ($hook.PSObject.Properties.Name -contains "last_assistant_message") {
    $message = [string]$hook.last_assistant_message
}

if ($message -match '(?i)\b(success|complete|done)\b' -and $message -notmatch '(?i)\b(evidence|test|file|line|report)\b') {
    ConvertTo-FvJson -Value (New-FvBlockOutput -Reason "Subagent completion claims success without evidence references.") -Compress | Write-Output
    exit 0
}

ConvertTo-FvJson -Value ([ordered]@{}) -Compress | Write-Output
exit 0
