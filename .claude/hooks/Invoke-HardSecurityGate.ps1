param(
    [Parameter(Mandatory = $false)]
    [string] $InputJson
)

$ErrorActionPreference = "Stop"
$moduleRoot = Join-Path $PSScriptRoot "modules"
Import-Module (Join-Path $moduleRoot "HookJson.psm1") -Force
Import-Module (Join-Path $moduleRoot "PathSecurity.psm1") -Force
Import-Module (Join-Path $moduleRoot "CommandClassification.psm1") -Force -WarningAction SilentlyContinue
Import-Module (Join-Path $moduleRoot "AuditLedger.psm1") -Force

function Write-Decision {
    param(
        [Parameter(Mandatory = $true)]
        [object] $Output
    )

    ConvertTo-FvJson -Value $Output -Compress | Write-Output
}

function Get-ToolAction {
    param(
        [Parameter(Mandatory = $true)]
        [object] $Hook
    )

    $toolInput = $Hook.tool_input
    if ($null -eq $toolInput) {
        return ""
    }

    foreach ($name in @("command", "file_path", "path", "url", "query", "prompt", "description")) {
        if ($toolInput.PSObject.Properties.Name -contains $name) {
            return [string]$toolInput.$name
        }
    }

    return ($toolInput | ConvertTo-Json -Depth 8 -Compress)
}

function Add-Audit {
    param(
        [Parameter(Mandatory = $true)]
        [object] $Hook,

        [Parameter(Mandatory = $true)]
        [string] $Decision,

        [Parameter(Mandatory = $true)]
        [string] $Reason,

        [Parameter(Mandatory = $false)]
        [string] $RiskLevel = "medium",

        [Parameter(Mandatory = $false)]
        [string[]] $Rules = @()
    )

    $root = Get-FvProjectRoot
    $contract = Get-FvActiveTask -Root $root
    $taskId = "NO_ACTIVE_TASK"
    if ($null -ne $contract -and ($contract.PSObject.Properties.Name -contains "taskId")) {
        $taskId = [string]$contract.taskId
    }

    $action = Protect-FvSecretText -Text (Get-ToolAction -Hook $Hook)
    $event = @{
        timestamp = Get-FvUtcNow
        event = [string]$Hook.hook_event_name
        taskId = $taskId
        toolName = [string]$Hook.tool_name
        action = $action
        decision = $Decision
        riskLevel = $RiskLevel
        reason = $Reason
        rules = $Rules
    }

    Write-FvAuditEvent -Event $event -Root $root | Out-Null
}

try {
    $hook = Read-FvHookInput -RawJson $InputJson
}
catch {
    $reason = $_.Exception.Message
    Write-Decision -Output (New-FvPreToolUseOutput -Decision "deny" -Reason $reason)
    exit 2
}

$eventName = [string]$hook.hook_event_name
$toolName = [string]$hook.tool_name
$toolInput = $hook.tool_input
$root = Get-FvProjectRoot
$contract = Get-FvActiveTask -Root $root

if ($eventName -eq "PermissionRequest") {
    if ($toolName -eq "Bash" -or $toolName -eq "PowerShell") {
        $classification = Classify-FvCommand -Command ([string]$toolInput.command)
        if ($classification.Verdict -eq "DENY") {
            Add-Audit -Hook $hook -Decision "DENY" -Reason $classification.Reason -RiskLevel $classification.RiskLevel -Rules $classification.Rules
            Write-Decision -Output (New-FvPermissionRequestOutput -Behavior "deny" -Message $classification.Reason)
            exit 0
        }

        if ($classification.Verdict -eq "ASK_USER") {
            Add-Audit -Hook $hook -Decision "ASK_USER" -Reason $classification.Reason -RiskLevel $classification.RiskLevel -Rules $classification.Rules
            Write-Decision -Output (New-FvPermissionRequestOutput -Behavior "deny" -Message $classification.Reason)
            exit 0
        }
    }

    Add-Audit -Hook $hook -Decision "ALLOW" -Reason "Permission request passed hard security gate." -RiskLevel "low" -Rules @("permissionrequest-hardgate")
    Write-Decision -Output (New-FvPermissionRequestOutput -Behavior "allow" -Message "Allowed by NyxVoice hard gate." -UpdatedInput $toolInput)
    exit 0
}

if ($eventName -ne "PreToolUse") {
    Add-Audit -Hook $hook -Decision "ALLOW" -Reason "No blocking rule for this event in hard gate." -RiskLevel "low" -Rules @("event-pass")
    Write-Decision -Output ([ordered]@{})
    exit 0
}

if ($toolName -eq "Bash" -or $toolName -eq "PowerShell") {
    $command = [string]$toolInput.command
    $classification = Classify-FvCommand -Command $command

    if ($classification.Verdict -eq "DENY") {
        Add-Audit -Hook $hook -Decision "DENY" -Reason $classification.Reason -RiskLevel $classification.RiskLevel -Rules $classification.Rules
        Write-Decision -Output (New-FvPreToolUseOutput -Decision "deny" -Reason $classification.Reason)
        exit 0
    }

    if ($classification.Verdict -eq "ASK_USER") {
        Add-Audit -Hook $hook -Decision "ASK_USER" -Reason $classification.Reason -RiskLevel $classification.RiskLevel -Rules $classification.Rules
        Write-Decision -Output (New-FvPreToolUseOutput -Decision "ask" -Reason $classification.Reason)
        exit 0
    }

    if ($classification.RequiresContract) {
        $active = Test-FvContractActive -Contract $contract
        if (-not $active.Allowed) {
            Add-Audit -Hook $hook -Decision "DENY" -Reason $active.Reason -RiskLevel "medium" -Rules @("contract-required")
            Write-Decision -Output (New-FvPreToolUseOutput -Decision "deny" -Reason $active.Reason)
            exit 0
        }

        $normalizedCommand = ConvertTo-FvNormalizedCommand -Command $command
        if (-not (Test-FvAllowedCommandFamily -Command $normalizedCommand -Contract $contract)) {
            $reason = "Command family is not listed in active contract allowedCommandFamilies."
            Add-Audit -Hook $hook -Decision "DENY" -Reason $reason -RiskLevel "medium" -Rules @("contract-command-family")
            Write-Decision -Output (New-FvPreToolUseOutput -Decision "deny" -Reason $reason)
            exit 0
        }
    }

    Add-Audit -Hook $hook -Decision "ALLOW" -Reason $classification.Reason -RiskLevel $classification.RiskLevel -Rules $classification.Rules
    Write-Decision -Output (New-FvPreToolUseOutput -Decision "allow" -Reason $classification.Reason)
    exit 0
}

if ($toolName -eq "Read" -or $toolName -eq "Write" -or $toolName -eq "Edit") {
    $pathValue = [string]$toolInput.file_path
    $resolved = Resolve-FvSecurePath -Path $pathValue -Root $root
    if (-not $resolved.IsValid) {
        Add-Audit -Hook $hook -Decision "DENY" -Reason $resolved.Reason -RiskLevel "critical" -Rules @("path-boundary")
        Write-Decision -Output (New-FvPreToolUseOutput -Decision "deny" -Reason $resolved.Reason)
        exit 0
    }

    if (Test-FvSecretPath -RelativePath $resolved.RelativePath) {
        $reason = "Secret or certificate path access is forbidden."
        Add-Audit -Hook $hook -Decision "DENY" -Reason $reason -RiskLevel "critical" -Rules @("secret-path")
        Write-Decision -Output (New-FvPreToolUseOutput -Decision "deny" -Reason $reason)
        exit 0
    }

    if ($toolName -eq "Read") {
        Add-Audit -Hook $hook -Decision "ALLOW" -Reason "Read is allowed for non-secret project path." -RiskLevel "low" -Rules @("read-non-secret")
        Write-Decision -Output (New-FvPreToolUseOutput -Decision "allow" -Reason "Read is allowed for non-secret project path.")
        exit 0
    }

    if (Test-FvProtectedPath -RelativePath $resolved.RelativePath) {
        $reason = "Protected governance path cannot be modified without human governance approval."
        Add-Audit -Hook $hook -Decision "DENY" -Reason $reason -RiskLevel "critical" -Rules @("protected-path")
        Write-Decision -Output (New-FvPreToolUseOutput -Decision "deny" -Reason $reason)
        exit 0
    }

    $pathAllowed = Test-FvContractAllowsPath -Contract $contract -RelativePath $resolved.RelativePath
    if (-not $pathAllowed.Allowed) {
        Add-Audit -Hook $hook -Decision "DENY" -Reason $pathAllowed.Reason -RiskLevel "medium" -Rules @("contract-path")
        Write-Decision -Output (New-FvPreToolUseOutput -Decision "deny" -Reason $pathAllowed.Reason)
        exit 0
    }

    Add-Audit -Hook $hook -Decision "ALLOW" -Reason $pathAllowed.Reason -RiskLevel "medium" -Rules @("contract-path")
    Write-Decision -Output (New-FvPreToolUseOutput -Decision "allow" -Reason $pathAllowed.Reason)
    exit 0
}

if ($toolName -eq "WebFetch" -or $toolName -eq "WebSearch") {
    $reason = "Network research requires contextual review or explicit contract allowance."
    Add-Audit -Hook $hook -Decision "ASK_USER" -Reason $reason -RiskLevel "high" -Rules @("external-network")
    Write-Decision -Output (New-FvPreToolUseOutput -Decision "ask" -Reason $reason)
    exit 0
}

if ($toolName -like "mcp__*") {
    $reason = "MCP tool requires explicit contextual review."
    Add-Audit -Hook $hook -Decision "ASK_USER" -Reason $reason -RiskLevel "high" -Rules @("mcp-tool")
    Write-Decision -Output (New-FvPreToolUseOutput -Decision "ask" -Reason $reason)
    exit 0
}

Add-Audit -Hook $hook -Decision "ALLOW" -Reason "No hard-gate rule blocked this tool." -RiskLevel "low" -Rules @("default-tool-allow")
Write-Decision -Output (New-FvPreToolUseOutput -Decision "allow" -Reason "No hard-gate rule blocked this tool.")
exit 0
