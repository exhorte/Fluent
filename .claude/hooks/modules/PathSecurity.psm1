Set-StrictMode -Version 2.0

function Get-FvProjectRoot {
    if (-not [string]::IsNullOrWhiteSpace($env:CLAUDE_PROJECT_DIR)) {
        return [System.IO.Path]::GetFullPath($env:CLAUDE_PROJECT_DIR)
    }

    $hooksDir = Split-Path -Parent $PSScriptRoot
    $claudeDir = Split-Path -Parent $hooksDir
    return [System.IO.Path]::GetFullPath((Split-Path -Parent $claudeDir))
}

function ConvertTo-FvRelativePath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $FullPath,

        [Parameter(Mandatory = $false)]
        [string] $Root = (Get-FvProjectRoot)
    )

    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $pathFull = [System.IO.Path]::GetFullPath($FullPath)
    if ($pathFull.Length -lt $rootFull.Length) {
        return $pathFull
    }

    $relative = $pathFull.Substring($rootFull.Length).TrimStart('\', '/')
    return ($relative -replace '\\', '/')
}

function Resolve-FvSecurePath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,

        [Parameter(Mandatory = $false)]
        [string] $Root = (Get-FvProjectRoot)
    )

    $rootFull = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $rootWithSep = $rootFull + [System.IO.Path]::DirectorySeparatorChar

    if ($Path -match '(^|[\\/])\.\.([\\/]|$)') {
        return [pscustomobject]@{
            IsValid = $false
            FullPath = $null
            RelativePath = $Path
            Reason = "Path traversal with '..' is forbidden."
        }
    }

    if ([System.IO.Path]::IsPathRooted($Path)) {
        $full = [System.IO.Path]::GetFullPath($Path)
    }
    else {
        $full = [System.IO.Path]::GetFullPath((Join-Path $rootFull $Path))
    }

    $isInside = $full.Equals($rootFull, [StringComparison]::OrdinalIgnoreCase) -or $full.StartsWith($rootWithSep, [StringComparison]::OrdinalIgnoreCase)
    $relative = ConvertTo-FvRelativePath -FullPath $full -Root $rootFull

    if (-not $isInside) {
        return [pscustomobject]@{
            IsValid = $false
            FullPath = $full
            RelativePath = $relative
            Reason = "Path is outside the project directory."
        }
    }

    return [pscustomobject]@{
        IsValid = $true
        FullPath = $full
        RelativePath = $relative
        Reason = ""
    }
}

function Test-FvGlobMatch {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,

        [Parameter(Mandatory = $true)]
        [string] $Pattern
    )

    $normalizedPath = ($Path -replace '\\', '/').TrimStart('/')
    $normalizedPattern = ($Pattern -replace '\\', '/').TrimStart('/')
    $normalizedPattern = $normalizedPattern -replace '\*\*', '*'

    if ($normalizedPath -like $normalizedPattern) {
        return $true
    }

    if ($normalizedPattern.EndsWith('/*')) {
        $prefix = $normalizedPattern.Substring(0, $normalizedPattern.Length - 1)
        return $normalizedPath.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)
    }

    return $false
}

function Test-FvSecretPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $RelativePath
    )

    $path = ($RelativePath -replace '\\', '/').TrimStart('/')
    $patterns = @(
        ".env",
        ".env.*",
        "secrets/*",
        "credentials/*",
        "*.key",
        "*.pem",
        "*.pfx",
        "*.p12",
        "*.cer",
        "*.crt",
        "*.der"
    )

    foreach ($pattern in $patterns) {
        if (Test-FvGlobMatch -Path $path -Pattern $pattern) {
            return $true
        }
    }

    return $false
}

function Test-FvProtectedPath {
    param(
        [Parameter(Mandatory = $true)]
        [string] $RelativePath
    )

    $path = ($RelativePath -replace '\\', '/').TrimStart('/')
    $patterns = @(
        "CLAUDE.md",
        ".claude/settings.json",
        ".claude/judge/*",
        ".claude/hooks/*",
        ".claude/schemas/*",
        ".claude/agents/development-judge.md",
        "docs/engineering/quality-gates.md",
        "docs/engineering/definition-of-done.md",
        ".git/*"
    )

    foreach ($pattern in $patterns) {
        if (Test-FvGlobMatch -Path $path -Pattern $pattern) {
            return $true
        }
    }

    return $false
}

function Get-FvActiveTaskPath {
    param(
        [Parameter(Mandatory = $false)]
        [string] $Root = (Get-FvProjectRoot)
    )

    if (-not [string]::IsNullOrWhiteSpace($env:FV_ACTIVE_TASK_PATH)) {
        return $env:FV_ACTIVE_TASK_PATH
    }

    return (Join-Path $Root ".claude\runtime\active-task.json")
}

function Get-FvActiveTask {
    param(
        [Parameter(Mandatory = $false)]
        [string] $Root = (Get-FvProjectRoot)
    )

    $path = Get-FvActiveTaskPath -Root $Root
    if (-not (Test-Path -LiteralPath $path)) {
        return $null
    }

    try {
        return (Get-Content -LiteralPath $path -Raw | ConvertFrom-Json -ErrorAction Stop)
    }
    catch {
        return $null
    }
}

function Test-FvContractActive {
    param(
        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [object] $Contract
    )

    if ($null -eq $Contract) {
        return [pscustomobject]@{ Allowed = $false; Reason = "No active task contract exists." }
    }

    if (-not $Contract.approvedByJudge) {
        return [pscustomobject]@{ Allowed = $false; Reason = "Active task contract is not Judge-approved." }
    }

    $terminal = @("draft", "under-review", "blocked", "completed")
    if ($terminal -contains [string]$Contract.status) {
        return [pscustomobject]@{ Allowed = $false; Reason = "Active task contract status cannot authorize new mutable work." }
    }

    if ($Contract.PSObject.Properties.Name -contains "expiresAt") {
        $expiresValue = $Contract.expiresAt
        if ($expiresValue -is [DateTime]) {
            $expiresDate = [DateTime]$expiresValue
            if ($expiresDate.Kind -eq [DateTimeKind]::Utc) {
                if ($expiresDate -lt [DateTime]::UtcNow) {
                    return [pscustomobject]@{ Allowed = $false; Reason = "Active task contract is expired." }
                }
            }
            elseif ($expiresDate -lt [DateTime]::Now) {
                return [pscustomobject]@{ Allowed = $false; Reason = "Active task contract is expired." }
            }
        }
        else {
            $expires = [DateTimeOffset]::MinValue
            if ([DateTimeOffset]::TryParse([string]$expiresValue, [ref]$expires)) {
                if ($expires.UtcDateTime -lt [DateTime]::UtcNow) {
                    return [pscustomobject]@{ Allowed = $false; Reason = "Active task contract is expired." }
                }
            }
        }
    }

    return [pscustomobject]@{ Allowed = $true; Reason = "Active task contract is approved." }
}

function Test-FvContractAllowsPath {
    param(
        [Parameter(Mandatory = $true)]
        [AllowNull()]
        [object] $Contract,

        [Parameter(Mandatory = $true)]
        [string] $RelativePath
    )

    $active = Test-FvContractActive -Contract $Contract
    if (-not $active.Allowed) {
        return $active
    }

    foreach ($pattern in @($Contract.forbiddenPaths)) {
        if (Test-FvGlobMatch -Path $RelativePath -Pattern ([string]$pattern)) {
            return [pscustomobject]@{ Allowed = $false; Reason = "Path is forbidden by active contract: $pattern" }
        }
    }

    foreach ($pattern in @($Contract.allowedPaths)) {
        if (Test-FvGlobMatch -Path $RelativePath -Pattern ([string]$pattern)) {
            return [pscustomobject]@{ Allowed = $true; Reason = "Path is allowed by active contract: $pattern" }
        }
    }

    return [pscustomobject]@{ Allowed = $false; Reason = "Path is not listed in active contract allowedPaths." }
}

Export-ModuleMember -Function Get-FvProjectRoot, ConvertTo-FvRelativePath, Resolve-FvSecurePath, Test-FvGlobMatch, Test-FvSecretPath, Test-FvProtectedPath, Get-FvActiveTaskPath, Get-FvActiveTask, Test-FvContractActive, Test-FvContractAllowsPath
