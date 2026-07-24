Set-StrictMode -Version 2.0

function Read-FvHookInput {
    param(
        [Parameter(Mandatory = $false)]
        [string] $RawJson
    )

    if ([string]::IsNullOrWhiteSpace($RawJson)) {
        $RawJson = [Console]::In.ReadToEnd()
    }

    if ([string]::IsNullOrWhiteSpace($RawJson)) {
        throw "Hook input JSON is empty."
    }

    try {
        return $RawJson | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        throw "Hook input JSON is malformed: $($_.Exception.Message)"
    }
}

function ConvertTo-FvJson {
    param(
        [Parameter(Mandatory = $true)]
        [object] $Value,

        [Parameter(Mandatory = $false)]
        [switch] $Compress
    )

    if ($Compress) {
        return ($Value | ConvertTo-Json -Depth 32 -Compress)
    }

    return ($Value | ConvertTo-Json -Depth 32)
}

function New-FvPreToolUseOutput {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet("allow", "ask", "deny", "defer")]
        [string] $Decision,

        [Parameter(Mandatory = $true)]
        [string] $Reason,

        [Parameter(Mandatory = $false)]
        [object] $UpdatedInput,

        [Parameter(Mandatory = $false)]
        [string] $AdditionalContext
    )

    $specific = [ordered]@{
        hookEventName = "PreToolUse"
        permissionDecision = $Decision
        permissionDecisionReason = $Reason
    }

    if ($null -ne $UpdatedInput) {
        $specific.updatedInput = $UpdatedInput
    }

    if (-not [string]::IsNullOrWhiteSpace($AdditionalContext)) {
        $specific.additionalContext = $AdditionalContext
    }

    return [ordered]@{
        hookSpecificOutput = $specific
    }
}

function New-FvPermissionRequestOutput {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet("allow", "deny")]
        [string] $Behavior,

        [Parameter(Mandatory = $true)]
        [string] $Message,

        [Parameter(Mandatory = $false)]
        [object] $UpdatedInput
    )

    $decision = [ordered]@{
        behavior = $Behavior
    }

    if ($Behavior -eq "deny") {
        $decision.message = $Message
        $decision.interrupt = $false
    }
    elseif ($null -ne $UpdatedInput) {
        $decision.updatedInput = $UpdatedInput
    }

    return [ordered]@{
        hookSpecificOutput = [ordered]@{
            hookEventName = "PermissionRequest"
            decision = $decision
        }
    }
}

function New-FvBlockOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Reason
    )

    return [ordered]@{
        decision = "block"
        reason = $Reason
    }
}

function New-FvAdditionalContextOutput {
    param(
        [Parameter(Mandatory = $true)]
        [string] $HookEventName,

        [Parameter(Mandatory = $true)]
        [string] $Context
    )

    return [ordered]@{
        hookSpecificOutput = [ordered]@{
            hookEventName = $HookEventName
            additionalContext = $Context
        }
    }
}

function Get-FvUtcNow {
    return ([DateTime]::UtcNow.ToString("o"))
}

function Protect-FvSecretText {
    param(
        [Parameter(Mandatory = $false)]
        [AllowNull()]
        [string] $Text
    )

    if ($null -eq $Text) {
        return ""
    }

    $redacted = $Text
    $redacted = $redacted -replace '(?i)(sk-[a-z0-9_-]{8,})', '[redacted-openai-key]'
    $redacted = $redacted -replace '(?i)(api[_-]?key|token|password|secret)\s*[:=]\s*["'']?[^"''\s]+', '$1=[redacted]'
    $redacted = $redacted -replace '(?i)(fixture-secret-value|super-secret-fixture)', '[redacted-fixture-secret]'
    return $redacted
}

Export-ModuleMember -Function Read-FvHookInput, ConvertTo-FvJson, New-FvPreToolUseOutput, New-FvPermissionRequestOutput, New-FvBlockOutput, New-FvAdditionalContextOutput, Get-FvUtcNow, Protect-FvSecretText
