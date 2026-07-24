Set-StrictMode -Version 2.0

function Get-FvAuditLedgerPath {
    param(
        [Parameter(Mandatory = $false)]
        [string] $Root
    )

    if (-not [string]::IsNullOrWhiteSpace($env:FV_AUDIT_LEDGER_PATH)) {
        return $env:FV_AUDIT_LEDGER_PATH
    }

    if ([string]::IsNullOrWhiteSpace($Root)) {
        if (-not [string]::IsNullOrWhiteSpace($env:CLAUDE_PROJECT_DIR)) {
            $Root = $env:CLAUDE_PROJECT_DIR
        }
        else {
            $hooksDir = Split-Path -Parent $PSScriptRoot
            $claudeDir = Split-Path -Parent $hooksDir
            $Root = Split-Path -Parent $claudeDir
        }
    }

    return (Join-Path $Root ".claude\runtime\audit-ledger.jsonl")
}

function Write-FvAuditEvent {
    param(
        [Parameter(Mandatory = $true)]
        [hashtable] $Event,

        [Parameter(Mandatory = $false)]
        [string] $Root
    )

    $path = Get-FvAuditLedgerPath -Root $Root
    $dir = Split-Path -Parent $path
    if (-not (Test-Path -LiteralPath $dir)) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }

    $json = $Event | ConvertTo-Json -Depth 16 -Compress
    Add-Content -LiteralPath $path -Value $json -Encoding UTF8
    return $path
}

Export-ModuleMember -Function Get-FvAuditLedgerPath, Write-FvAuditEvent
