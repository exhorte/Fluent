Set-StrictMode -Version 2.0

function ConvertTo-FvNormalizedCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Command
    )

    $normalized = $Command.ToLowerInvariant()
    $normalized = $normalized -replace '[“”]', '"'
    $normalized = $normalized -replace "[‘’]", "'"
    $normalized = $normalized -replace '\\', '/'
    $normalized = $normalized -replace '\s+', ' '
    return $normalized.Trim()
}

function New-FvCommandResult {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet("ALLOW", "DENY", "ASK_USER")]
        [string] $Verdict,

        [Parameter(Mandatory = $true)]
        [ValidateSet("low", "medium", "high", "critical")]
        [string] $RiskLevel,

        [Parameter(Mandatory = $true)]
        [string] $Reason,

        [Parameter(Mandatory = $true)]
        [string[]] $Rules,

        [Parameter(Mandatory = $false)]
        [bool] $RequiresContract = $false
    )

    return [pscustomobject]@{
        Verdict = $Verdict
        RiskLevel = $RiskLevel
        Reason = $Reason
        Rules = $Rules
        RequiresContract = $RequiresContract
    }
}

function Test-FvAllowedCommandFamily {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Command,

        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [object] $Contract
    )

    if ($null -eq $Contract) {
        return $false
    }

    foreach ($family in @($Contract.allowedCommandFamilies)) {
        $normalizedFamily = ConvertTo-FvNormalizedCommand -Command ([string]$family)
        if ($Command.StartsWith($normalizedFamily, [StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }

    return $false
}

function Classify-FvCommand {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Command
    )

    $cmd = ConvertTo-FvNormalizedCommand -Command $Command

    $denyRules = @(
        @{ Id = "cmd.git-reset-hard"; Pattern = '\bgit\s+reset\s+--hard\b'; Reason = "git reset --hard is forbidden." },
        @{ Id = "cmd.git-clean-destructive"; Pattern = '\bgit\s+clean\s+.*(-f|-d|-x)'; Reason = "destructive git clean is forbidden." },
        @{ Id = "cmd.git-force-push"; Pattern = '\bgit\s+push\b.*(--force|-f|--force-with-lease)'; Reason = "force push is forbidden." },
        @{ Id = "cmd.recursive-delete"; Pattern = '\b(rm\s+-rf|remove-item\b.*-(recurse|r)\b|del\s+/s|rmdir\s+/s)\b'; Reason = "recursive deletion is forbidden by the hard gate." },
        @{ Id = "cmd.format-disk"; Pattern = '\b(format|diskpart)\b'; Reason = "disk formatting or partitioning is forbidden." },
        @{ Id = "cmd.bypass"; Pattern = '(bypasspermissions|dangerously-skip-permissions|disableallhooks)'; Reason = "permission bypass or hook disabling is forbidden." },
        @{ Id = "cmd.publish"; Pattern = '\b(npm\s+publish|dotnet\s+nuget\s+push|winget\s+create|choco\s+push)\b'; Reason = "publishing is outside the active phase." }
    )

    foreach ($rule in $denyRules) {
        if ($cmd -match $rule.Pattern) {
            return New-FvCommandResult -Verdict "DENY" -RiskLevel "critical" -Reason $rule.Reason -Rules @($rule.Id)
        }
    }

    # ADR-0007 risk model: registry and machine-level installation still require the
    # user (E-010/E-011) because they affect the user's Windows machine, not reversible
    # project/cloud state. A non-force `git push` is R1 (PROJECT_DIRECTOR standing
    # authority) and is intentionally NOT listed here; force-push variants remain in the
    # deny rules above.
    $humanRules = @(
        @{ Id = "cmd.registry"; Pattern = '\b(reg\s+(add|delete|import)|set-itemproperty\b.*hklm:|new-itemproperty\b.*hklm:)'; Category = "E-010"; Reason = "Windows registry modification requires human authorization." },
        @{ Id = "cmd.admin-install"; Pattern = '\b(msiexec|winget\s+install|choco\s+install|scoop\s+install)\b'; Category = "E-010"; Reason = "administrator or machine-level installation may require human authorization." }
    )

    foreach ($rule in $humanRules) {
        if ($cmd -match $rule.Pattern) {
            return New-FvCommandResult -Verdict "ASK_USER" -RiskLevel "high" -Reason $rule.Reason -Rules @($rule.Id, $rule.Category)
        }
    }

    # External deployment (R1 dev/staging, R2 pre-authorized production): allowed only
    # when the active contract records the target in allowedCommandFamilies. This makes
    # the authorization non-fabricable without prompting the user at execution time.
    if ($cmd -match '\b(terraform\s+apply|az\s+deployment|kubectl\s+apply|vercel\s+deploy|netlify\s+deploy)\b') {
        return New-FvCommandResult -Verdict "ALLOW" -RiskLevel "high" -Reason "Deployment is allowed only when the active contract records the target command family (R1/R2)." -Rules @("cmd.deploy", "contract-gated") -RequiresContract $true
    }

    if ($cmd -match '^git\s+status(\s|$)' -or $cmd -match '^git\s+diff(\s|$)' -or $cmd -match '^git\s+log(\s|$)') {
        return New-FvCommandResult -Verdict "ALLOW" -RiskLevel "low" -Reason "Read-only Git command is allowed." -Rules @("readOnlySafe")
    }

    if ($cmd -match '^dotnet\s+(restore|build|test)(\s|$)') {
        return New-FvCommandResult -Verdict "ALLOW" -RiskLevel "medium" -Reason "dotnet verification command may run inside an approved contract." -Rules @("judgeMayAllow") -RequiresContract $true
    }

    if ($cmd -match '^(pwsh|powershell)(\.exe)?\s+.*\.claude/hooks/tests/invoke-allhooktests\.ps1') {
        return New-FvCommandResult -Verdict "ALLOW" -RiskLevel "medium" -Reason "Harness test runner is allowed inside an approved contract." -Rules @("judgeMayAllow") -RequiresContract $true
    }

    if ($cmd -match '^(claude|git|dotnet|pwsh|powershell)(\.exe)?\s+.*(--version|--help|-h)(\s|$)') {
        return New-FvCommandResult -Verdict "ALLOW" -RiskLevel "low" -Reason "Version/help command is read-only." -Rules @("readOnlySafe")
    }

    return New-FvCommandResult -Verdict "ALLOW" -RiskLevel "medium" -Reason "No hard-gate violation detected; contextual contract checks still apply for mutable effects." -Rules @("default-allow") -RequiresContract $false
}

Export-ModuleMember -Function ConvertTo-FvNormalizedCommand, Classify-FvCommand, Test-FvAllowedCommandFamily
