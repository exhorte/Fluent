Register-FvTest "T-020 untested task cannot be completed" {
    Set-FvActiveContract -Status "verifying"
    $hook = Get-FvFixture -Name "task-completion.json"
    $result = Invoke-FvHook -ScriptPath $script:CompletionScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "block") "Expected block."
}

Register-FvTest "T-021 task with critical finding cannot be completed" {
    Set-FvActiveContract -Status "verifying" -Verification @{ testsPassed = $true } -OpenFindings @(@{ severity = "critical"; title = "critical fixture" })
    $hook = Get-FvFixture -Name "task-completion.json"
    $result = Invoke-FvHook -ScriptPath $script:CompletionScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "block") "Expected block."
}

Register-FvTest "T-028 compaction context contains phase and task" {
    Set-FvActiveContract
    $hook = [pscustomobject]@{
        hook_event_name = "PreCompact"
        trigger = "manual"
    }
    $result = Invoke-FvHook -ScriptPath $script:SaveCompactScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ($result.ExitCode -eq 0) "Expected exit code 0."
    $path = Join-Path $script:ProjectRoot ".claude\runtime\compaction-context.json"
    Assert-Fv (Test-Path -LiteralPath $path) "Compaction context missing."
    $context = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
    Assert-Fv ($context.phaseId -eq "FV-P00") "Phase missing from compaction context."
    Assert-Fv ($context.taskId -eq "FV-P00-T999") "Task missing from compaction context."
}

Register-FvTest "T-029 Development Judge has no write tools" {
    $path = Join-Path $script:ProjectRoot ".claude\agents\development-judge.md"
    Assert-Fv (Test-Path -LiteralPath $path) "Development Judge agent file missing."
    $content = Get-Content -LiteralPath $path -Raw
    $frontmatter = [regex]::Match($content, '(?s)^---\s*(.*?)\s*---')
    Assert-Fv ($frontmatter.Success) "Agent frontmatter missing."
    $yaml = $frontmatter.Groups[1].Value
    Assert-Fv ($yaml -match '(?m)^tools:\s*Read,\s*Glob,\s*Grep\s*$') "Judge tools must be Read, Glob, Grep only."
    Assert-Fv ($yaml -notmatch '(?i)\b(Write|Edit|Bash|PowerShell)\b') "Judge has a write or shell tool."
}

Register-FvTest "T-030 Development Judge cannot modify constitution" {
    Set-FvActiveContract -AllowedPaths @(".claude/**")
    $path = Join-Path $script:ProjectRoot ".claude\judge\constitution.md"
    $hook = [pscustomobject]@{ hook_event_name = "PreToolUse"; tool_name = "Write"; tool_input = @{ file_path = $path; content = "bad" } }
    $result = Invoke-FvHook -ScriptPath $script:HookScript -Json (ConvertTo-FvFixtureJson $hook)
    Assert-Fv ((Get-FvHookDecision $result) -eq "deny") "Expected deny."
}
