Register-FvTest "T-004 write in allowedPaths is allowed" {
    Set-FvActiveContract -AllowedPaths @("docs/**")
    $path = Join-Path $script:ProjectRoot "docs\project\fixture-output.md"
    $hook = [pscustomobject]@{
        hook_event_name = "PreToolUse"
        tool_name = "Write"
        tool_input = @{ file_path = $path; content = "fixture" }
    }
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "allow") "Expected allow."
}

Register-FvTest "T-005 write outside allowedPaths is denied" {
    Set-FvActiveContract -AllowedPaths @("docs/**")
    $path = Join-Path $script:ProjectRoot "src\fixture-output.cs"
    $hook = [pscustomobject]@{ hook_event_name = "PreToolUse"; tool_name = "Write"; tool_input = @{ file_path = $path; content = "fixture" } }
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "deny") "Expected deny."
}

Register-FvTest "T-006 write in judge governance path is denied after lock" {
    Set-FvActiveContract -AllowedPaths @(".claude/**")
    $hook = Get-FvFixture -Name "protected-write.json"
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "deny") "Expected deny."
}

Register-FvTest "T-016 path traversal is denied" {
    Set-FvActiveContract -AllowedPaths @("docs/**")
    $hook = [pscustomobject]@{
        hook_event_name = "PreToolUse"
        tool_name = "Write"
        tool_input = @{ file_path = "..\outside.txt"; content = "fixture" }
    }
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "deny") "Expected deny."
}

Register-FvTest "T-018 task without contract cannot write" {
    Clear-FvRuntime
    $path = Join-Path $script:ProjectRoot "docs\project\fixture-output.md"
    $hook = [pscustomobject]@{ hook_event_name = "PreToolUse"; tool_name = "Write"; tool_input = @{ file_path = $path; content = "fixture" } }
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "deny") "Expected deny."
}

Register-FvTest "T-019 expired contract cannot write" {
    Set-FvActiveContract -AllowedPaths @("docs/**") -ExpiresAt ([DateTime]::UtcNow.AddMinutes(-1))
    $path = Join-Path $script:ProjectRoot "docs\project\fixture-output.md"
    $hook = [pscustomobject]@{ hook_event_name = "PreToolUse"; tool_name = "Write"; tool_input = @{ file_path = $path; content = "fixture" } }
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "deny") "Expected deny."
}

Register-FvTest "T-031 Windows path containing spaces is handled" {
    Set-FvActiveContract -AllowedPaths @("docs/**")
    $path = Join-Path $script:ProjectRoot "docs\project\path with spaces\fixture output.md"
    $hook = [pscustomobject]@{ hook_event_name = "PreToolUse"; tool_name = "Write"; tool_input = @{ file_path = $path; content = "fixture" } }
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "allow") "Expected allow for path containing spaces."
}
