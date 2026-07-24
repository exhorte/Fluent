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
    ConvertTo-FvJson -Value ([ordered]@{}) -Compress | Write-Output
    exit 0
}

$nonTerminal = @("approved", "implementing", "verifying", "reviewing", "rework")
if ($nonTerminal -contains [string]$contract.status) {
    ConvertTo-FvJson -Value (New-FvBlockOutput -Reason "Active task remains non-terminal; update evidence or state before stopping.") -Compress | Write-Output
    exit 0
}

ConvertTo-FvJson -Value ([ordered]@{}) -Compress | Write-Output
exit 0
