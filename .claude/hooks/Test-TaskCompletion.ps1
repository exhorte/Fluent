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
if ($null -eq $contract) {
    ConvertTo-FvJson -Value ([ordered]@{ continue = $false; stopReason = "Task completion requires an active contract." }) -Compress | Write-Output
    exit 0
}

if ($contract.PSObject.Properties.Name -contains "openFindings") {
    foreach ($finding in @($contract.openFindings)) {
        if ([string]$finding.severity -eq "critical" -or [string]$finding -match '(?i)critical') {
            ConvertTo-FvJson -Value ([ordered]@{ continue = $false; stopReason = "Task completion blocked by critical finding." }) -Compress | Write-Output
            exit 0
        }
    }
}

if ($contract.PSObject.Properties.Name -notcontains "verification" -or $null -eq $contract.verification) {
    ConvertTo-FvJson -Value ([ordered]@{ continue = $false; stopReason = "Task completion requires verification evidence." }) -Compress | Write-Output
    exit 0
}

if (-not $contract.verification.testsPassed) {
    ConvertTo-FvJson -Value ([ordered]@{ continue = $false; stopReason = "Task completion requires passing tests." }) -Compress | Write-Output
    exit 0
}

ConvertTo-FvJson -Value ([ordered]@{}) -Compress | Write-Output
exit 0
