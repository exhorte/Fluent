param(
    [Parameter(Mandatory = $false)]
    [string] $ReportPath
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version 2.0

$script:TestRoot = Split-Path -Parent $PSScriptRoot
$script:ProjectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot "..\..\.."))
$script:HookScript = Join-Path $script:TestRoot "Invoke-HardSecurityGate.ps1"
$script:CompletionScript = Join-Path $script:TestRoot "Test-TaskCompletion.ps1"
$script:StopScript = Join-Path $script:TestRoot "Test-SessionStop.ps1"
$script:SaveCompactScript = Join-Path $script:TestRoot "Save-CompactionContext.ps1"
$script:Tests = New-Object System.Collections.Generic.List[object]
$env:CLAUDE_PROJECT_DIR = $script:ProjectRoot
$env:FV_ACTIVE_TASK_PATH = Join-Path $script:ProjectRoot ".claude\runtime\test-active-task.json"
$env:FV_AUDIT_LEDGER_PATH = Join-Path $script:ProjectRoot "docs\project\evidence\phase-00\hook-audit-ledger.jsonl"

if ([string]::IsNullOrWhiteSpace($ReportPath)) {
    $ReportPath = Join-Path $script:ProjectRoot "docs\project\evidence\phase-00\hook-tests.json"
}

function Register-FvTest {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name,

        [Parameter(Mandatory = $true)]
        [scriptblock] $Body
    )

    $script:Tests.Add([pscustomobject]@{ Name = $Name; Body = $Body }) | Out-Null
}

function Assert-Fv {
    param(
        [Parameter(Mandatory = $true)]
        [bool] $Condition,

        [Parameter(Mandatory = $true)]
        [string] $Message
    )

    if (-not $Condition) {
        throw $Message
    }
}

function Get-FvPowerShellExe {
    $process = Get-Process -Id $PID
    return $process.Path
}

function ConvertTo-FvFixtureJson {
    param(
        [Parameter(Mandatory = $true)]
        [object] $Value
    )

    return ($Value | ConvertTo-Json -Depth 32 -Compress)
}

function Get-FvFixture {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Name
    )

    $path = Join-Path $PSScriptRoot "fixtures\$Name"
    $raw = Get-Content -LiteralPath $path -Raw
    $raw = $raw.Replace('$PROJECT_ROOT', ($script:ProjectRoot -replace '\\', '\\'))
    return $raw | ConvertFrom-Json
}

function Invoke-FvHook {
    param(
        [Parameter(Mandatory = $true)]
        [string] $ScriptPath,

        [Parameter(Mandatory = $true)]
        [string] $Json,

        [Parameter(Mandatory = $false)]
        [switch] $ViaStdin
    )

    if (-not $ViaStdin) {
        $output = & $ScriptPath -InputJson $Json
        $exit = $LASTEXITCODE
        if ($null -eq $exit) {
            $exit = 0
        }
        $parsed = $null
        if (-not [string]::IsNullOrWhiteSpace(($output -join ""))) {
            $parsed = ($output -join "`n") | ConvertFrom-Json
        }
        return [pscustomobject]@{ ExitCode = $exit; StdOut = ($output -join "`n"); Json = $parsed }
    }

    $psi = [System.Diagnostics.ProcessStartInfo]::new()
    $psi.FileName = Get-FvPowerShellExe
    $psi.ArgumentList.Add("-NoProfile")
    $psi.ArgumentList.Add("-ExecutionPolicy")
    $psi.ArgumentList.Add("Bypass")
    $psi.ArgumentList.Add("-File")
    $psi.ArgumentList.Add($ScriptPath)
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.UseShellExecute = $false
    $psi.Environment["CLAUDE_PROJECT_DIR"] = $script:ProjectRoot
    $psi.Environment["FV_ACTIVE_TASK_PATH"] = $env:FV_ACTIVE_TASK_PATH
    $psi.Environment["FV_AUDIT_LEDGER_PATH"] = $env:FV_AUDIT_LEDGER_PATH
    $proc = [System.Diagnostics.Process]::Start($psi)
    $proc.StandardInput.Write($Json)
    $proc.StandardInput.Close()
    $stdout = $proc.StandardOutput.ReadToEnd()
    $stderr = $proc.StandardError.ReadToEnd()
    $proc.WaitForExit()
    $parsed = $null
    if (-not [string]::IsNullOrWhiteSpace($stdout)) {
        $parsed = $stdout | ConvertFrom-Json
    }
    return [pscustomobject]@{ ExitCode = $proc.ExitCode; StdOut = $stdout; StdErr = $stderr; Json = $parsed }
}

function Get-FvHookDecision {
    param(
        [Parameter(Mandatory = $true)]
        [object] $Result
    )

    if ($null -eq $Result.Json) {
        return ""
    }

    if ($Result.Json.PSObject.Properties.Name -contains "hookSpecificOutput") {
        $specific = $Result.Json.hookSpecificOutput
        if ($specific.PSObject.Properties.Name -contains "permissionDecision") {
            return [string]$specific.permissionDecision
        }
        if ($specific.PSObject.Properties.Name -contains "decision") {
            return [string]$specific.decision.behavior
        }
    }

    if ($Result.Json.PSObject.Properties.Name -contains "decision") {
        return [string]$Result.Json.decision
    }

    if ($Result.Json.PSObject.Properties.Name -contains "continue" -and $Result.Json.continue -eq $false) {
        return "block"
    }

    return "allow"
}

function Set-FvActiveContract {
    param(
        [Parameter(Mandatory = $false)]
        [string] $Status = "implementing",

        [Parameter(Mandatory = $false)]
        [string[]] $AllowedPaths = @("docs/**", "src/**", "tests/**", ".claude/runtime/**"),

        [Parameter(Mandatory = $false)]
        [string[]] $AllowedCommandFamilies = @("dotnet build", "dotnet test", "dotnet restore", "pwsh", "powershell"),

        [Parameter(Mandatory = $false)]
        [bool] $Approved = $true,

        [Parameter(Mandatory = $false)]
        [datetime] $ExpiresAt = ([DateTime]::UtcNow.AddHours(1)),

        [Parameter(Mandatory = $false)]
        [object] $Verification = $null,

        [Parameter(Mandatory = $false)]
        [object[]] $OpenFindings = @()
    )

    $contract = [ordered]@{
        taskId = "FV-P00-T999"
        phaseId = "FV-P00"
        title = "Fixture active contract"
        objective = "Validate hook harness."
        status = $Status
        createdAt = [DateTime]::UtcNow.AddMinutes(-5).ToString("o")
        updatedAt = [DateTime]::UtcNow.ToString("o")
        expiresAt = $ExpiresAt.ToUniversalTime().ToString("o")
        approvedByJudge = $Approved
        scope = @{ included = @("tests"); excluded = @("product features") }
        allowedPaths = $AllowedPaths
        readOnlyPaths = @()
        forbiddenPaths = @(".git/**", ".env", ".env.*", "secrets/**")
        allowedCommandFamilies = $AllowedCommandFamilies
        forbiddenCommandFamilies = @("git reset --hard", "git clean", "git push --force")
        acceptanceCriteria = @("fixture")
        requiredTests = @("fixture")
        requiredReviews = @()
        requiredEvidence = @("fixture")
        risks = @()
        rollbackPlan = @("delete fixture runtime")
        dependencies = @()
        humanApprovals = @()
        maxRepairCycles = 3
    }

    if ($null -ne $Verification) {
        $contract.verification = $Verification
    }
    if ($OpenFindings.Count -gt 0) {
        $contract.openFindings = $OpenFindings
    }

    $contract | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $env:FV_ACTIVE_TASK_PATH -Encoding UTF8
}

function Clear-FvRuntime {
    foreach ($path in @($env:FV_ACTIVE_TASK_PATH, $env:FV_AUDIT_LEDGER_PATH, (Join-Path $script:ProjectRoot ".claude\runtime\compaction-context.json"))) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Force
        }
    }
}

. (Join-Path $PSScriptRoot "Test-HardSecurityGate.ps1")
. (Join-Path $PSScriptRoot "Test-ProtectedPaths.ps1")
. (Join-Path $PSScriptRoot "Test-CommandClassification.ps1")
. (Join-Path $PSScriptRoot "Test-CompletionGate.ps1")
. (Join-Path $PSScriptRoot "Test-GovernanceModel.ps1")

$started = [DateTime]::UtcNow
$results = New-Object System.Collections.Generic.List[object]
Clear-FvRuntime

foreach ($test in $script:Tests) {
    $testStarted = [DateTime]::UtcNow
    try {
        & $test.Body
        $results.Add([pscustomobject]@{
            name = $test.Name
            status = "PASS"
            durationMs = [int](([DateTime]::UtcNow - $testStarted).TotalMilliseconds)
            error = ""
        }) | Out-Null
    }
    catch {
        $results.Add([pscustomobject]@{
            name = $test.Name
            status = "FAIL"
            durationMs = [int](([DateTime]::UtcNow - $testStarted).TotalMilliseconds)
            error = $_.Exception.Message
        }) | Out-Null
    }
}

$finished = [DateTime]::UtcNow
$failed = @($results | Where-Object { $_.status -ne "PASS" })
$summary = [ordered]@{
    type = "hook-test-report"
    taskId = "FV-P00-T001"
    command = ".claude/hooks/tests/Invoke-AllHookTests.ps1"
    startedAt = $started.ToString("o")
    finishedAt = $finished.ToString("o")
    exitCode = $(if ($failed.Count -eq 0) { 0 } else { 1 })
    status = $(if ($failed.Count -eq 0) { "PASS" } else { "FAIL" })
    total = $results.Count
    passed = @($results | Where-Object { $_.status -eq "PASS" }).Count
    failed = $failed.Count
    anomalies = @($failed | ForEach-Object { "$($_.name): $($_.error)" })
    results = $results
    evidencePaths = @($ReportPath, $env:FV_AUDIT_LEDGER_PATH)
}

$dir = Split-Path -Parent $ReportPath
if (-not (Test-Path -LiteralPath $dir)) {
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
}
$summary | ConvertTo-Json -Depth 32 | Set-Content -LiteralPath $ReportPath -Encoding UTF8

"Hook tests: $($summary.passed)/$($summary.total) passed. Report: $ReportPath"
if ($failed.Count -gt 0) {
    $failed | ForEach-Object { "FAIL $($_.name): $($_.error)" }
    exit 1
}

exit 0
