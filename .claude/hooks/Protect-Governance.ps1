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

$eventName = [string]$hook.hook_event_name
if ($eventName -ne "ConfigChange") {
    ConvertTo-FvJson -Value ([ordered]@{}) -Compress | Write-Output
    exit 0
}

$source = ""
if ($hook.PSObject.Properties.Name -contains "source") {
    $source = [string]$hook.source
}
elseif ($hook.PSObject.Properties.Name -contains "config_source") {
    $source = [string]$hook.config_source
}

$protectedSources = @("project_settings", "local_settings", "skills", "policy_settings")
if ($protectedSources -contains $source -and $env:FV_HUMAN_APPROVED_GOVERNANCE_CHANGE -ne "true") {
    ConvertTo-FvJson -Value (New-FvBlockOutput -Reason "Governance or settings change requires human approval.") -Compress | Write-Output
    exit 0
}

ConvertTo-FvJson -Value ([ordered]@{}) -Compress | Write-Output
exit 0
