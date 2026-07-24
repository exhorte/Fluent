param(
    [Parameter(Mandatory = $false)]
    [string] $InputJson
)

& (Join-Path $PSScriptRoot "Protect-Governance.ps1") -InputJson $InputJson
exit $LASTEXITCODE
