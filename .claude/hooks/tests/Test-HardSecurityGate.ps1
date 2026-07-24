Register-FvTest "T-001 git status is allowed" {
    Clear-FvRuntime
    $hook = [pscustomobject]@{
        hook_event_name = "PreToolUse"
        tool_name = "Bash"
        tool_input = @{ command = "git status --short" }
    }
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook) -ViaStdin
    Assert-Fv ($result.ExitCode -eq 0) "Expected exit code 0."
    Assert-Fv ((Get-FvHookDecision $result) -eq "allow") "Expected allow."
}

Register-FvTest "T-002 git diff is allowed" {
    Clear-FvRuntime
    $hook = [pscustomobject]@{
        hook_event_name = "PreToolUse"
        tool_name = "Bash"
        tool_input = @{ command = "git diff -- README.md" }
    }
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "allow") "Expected allow."
}

Register-FvTest "T-003 dotnet build in active contract is allowed" {
    Set-FvActiveContract
    $hook = Get-FvFixture -Name "safe-build.json"
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "allow") "Expected allow."
}

Register-FvTest "T-007 .env read is denied" {
    Set-FvActiveContract
    $hook = Get-FvFixture -Name "secret-read.json"
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "deny") "Expected deny."
}

Register-FvTest "T-008 certificate read is denied" {
    Set-FvActiveContract
    $path = Join-Path $script:ProjectRoot "secret.pfx"
    $hook = [pscustomobject]@{
        hook_event_name = "PreToolUse"
        tool_name = "Read"
        tool_input = @{ file_path = $path }
    }
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "deny") "Expected deny."
}

Register-FvTest "T-009 git reset hard is denied" {
    $hook = [pscustomobject]@{ hook_event_name = "PreToolUse"; tool_name = "Bash"; tool_input = @{ command = "git reset --hard HEAD" } }
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "deny") "Expected deny."
}

Register-FvTest "T-010 destructive git clean is denied" {
    $hook = [pscustomobject]@{ hook_event_name = "PreToolUse"; tool_name = "Bash"; tool_input = @{ command = "git clean -fdx" } }
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "deny") "Expected deny."
}

Register-FvTest "T-011 git push force is denied" {
    $hook = Get-FvFixture -Name "force-push.json"
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "deny") "Expected deny."
}

Register-FvTest "T-012 recursive project delete is denied" {
    $hook = Get-FvFixture -Name "dangerous-delete.json"
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "deny") "Expected deny."
}

Register-FvTest "T-013 dangerous command after semicolon is detected" {
    $hook = [pscustomobject]@{ hook_event_name = "PreToolUse"; tool_name = "PowerShell"; tool_input = @{ command = "git status; git reset --hard" } }
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "deny") "Expected deny."
}

Register-FvTest "T-014 dangerous command after && is detected" {
    $hook = [pscustomobject]@{ hook_event_name = "PreToolUse"; tool_name = "Bash"; tool_input = @{ command = "echo ok && git clean -fdx" } }
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "deny") "Expected deny."
}

Register-FvTest "T-015 dangerous command in pipe is detected" {
    $hook = [pscustomobject]@{ hook_event_name = "PreToolUse"; tool_name = "PowerShell"; tool_input = @{ command = "Get-ChildItem . | Remove-Item -Recurse -Force" } }
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "deny") "Expected deny."
}

Register-FvTest "T-017 malformed JSON fails safely" {
    $fixture = Get-Content -LiteralPath (Join-Path $PSScriptRoot "fixtures\malformed-input.json") -Raw | ConvertFrom-Json
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json ([string]$fixture.raw) -ViaStdin
    Assert-Fv ($result.ExitCode -ne 0) "Expected non-zero exit."
    Assert-Fv ((Get-FvHookDecision $result) -eq "deny") "Expected deny."
}

Register-FvTest "T-022 non-force git push is allowed (R1, ADR-0007)" {
    Clear-FvRuntime
    $hook = Get-FvFixture -Name "external-push.json"
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "allow") "Expected allow for non-force work-branch push."
}

Register-FvTest "T-030 deployment without a recorded contract family is denied" {
    Set-FvActiveContract
    $hook = [pscustomobject]@{ hook_event_name = "PreToolUse"; tool_name = "PowerShell"; tool_input = @{ command = "kubectl apply -f deploy.yaml" } }
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "deny") "Expected deny when deploy family is not in the contract."
}

Register-FvTest "T-031 deployment recorded in the contract family is allowed (R1/R2)" {
    Set-FvActiveContract -AllowedCommandFamilies @("dotnet build", "dotnet test", "kubectl apply")
    $hook = [pscustomobject]@{ hook_event_name = "PreToolUse"; tool_name = "PowerShell"; tool_input = @{ command = "kubectl apply -f deploy.yaml" } }
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "allow") "Expected allow when deploy family is recorded in the contract."
}

Register-FvTest "T-023 registry modification asks user" {
    $hook = [pscustomobject]@{ hook_event_name = "PreToolUse"; tool_name = "PowerShell"; tool_input = @{ command = "reg add HKLM\Software\NyxVoice /v Test /d 1" } }
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "ask") "Expected ask."
}

Register-FvTest "T-024 administrator install asks user" {
    $hook = [pscustomobject]@{ hook_event_name = "PreToolUse"; tool_name = "PowerShell"; tool_input = @{ command = "winget install Some.Tool" } }
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "ask") "Expected ask."
}

Register-FvTest "T-025 ordinary technical choice does not ask user" {
    $hook = [pscustomobject]@{ hook_event_name = "PreToolUse"; tool_name = "PowerShell"; tool_input = @{ command = "Write-Output 'choose internal class name'" } }
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "allow") "Expected allow."
}

Register-FvTest "T-026 decisions write valid redacted JSONL audit entries" {
    Clear-FvRuntime
    $hook = [pscustomobject]@{ hook_event_name = "PreToolUse"; tool_name = "PowerShell"; tool_input = @{ command = "Write-Output super-secret-fixture" } }
    $null = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv (Test-Path -LiteralPath $env:FV_AUDIT_LEDGER_PATH) "Audit ledger missing."
    $lines = @(Get-Content -LiteralPath $env:FV_AUDIT_LEDGER_PATH)
    Assert-Fv ($lines.Count -ge 1) "Audit ledger has no entries."
    foreach ($line in $lines) {
        $entry = $line | ConvertFrom-Json
        Assert-Fv ($entry.PSObject.Properties.Name -contains "timestamp") "Audit entry missing timestamp."
        Assert-Fv ($line -notmatch "super-secret-fixture") "Audit ledger leaked fixture secret."
    }
}
