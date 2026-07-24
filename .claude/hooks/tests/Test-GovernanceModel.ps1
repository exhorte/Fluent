# Governance model tests (ADR-0007). Deterministic, file-based invariants for the
# risk-based autonomous governance. These assert the installed configuration, not agent
# behavior; behavioral tiers (R3 escalation, Judge auditing, anti-loop) are verified by
# review, not fabricated here.

Register-FvTest "G-001 risk-authorization model and secrets policy are installed" {
    Assert-Fv (Test-Path -LiteralPath (Join-Path $script:ProjectRoot ".claude\judge\risk-authorization-model.md")) "risk-authorization-model.md missing."
    Assert-Fv (Test-Path -LiteralPath (Join-Path $script:ProjectRoot ".claude\judge\secrets-policy.md")) "secrets-policy.md missing."
}

Register-FvTest "G-002 PROJECT_DIRECTOR agent exists and ADR-0007 is present" {
    Assert-Fv (Test-Path -LiteralPath (Join-Path $script:ProjectRoot ".claude\agents\project-director.md")) "project-director agent missing."
    Assert-Fv (Test-Path -LiteralPath (Join-Path $script:ProjectRoot "docs\architecture\decisions\ADR-0007-risk-based-autonomous-governance.md")) "ADR-0007 missing."
}

Register-FvTest "G-003 Judge is an auditor: verdict enum is ALLOW/ALLOW_WITH_DEBT/BLOCK_CRITICAL" {
    $schema = Get-Content -LiteralPath (Join-Path $script:ProjectRoot ".claude\schemas\verdict.schema.json") -Raw | ConvertFrom-Json
    $enum = @($schema.properties.verdict.enum)
    Assert-Fv ($enum -contains "ALLOW_WITH_DEBT") "verdict enum must include ALLOW_WITH_DEBT."
    Assert-Fv ($enum -contains "BLOCK_CRITICAL") "verdict enum must include BLOCK_CRITICAL."
    Assert-Fv (-not ($enum -contains "DENY")) "verdict enum must no longer include the gatekeeper DENY."
}

Register-FvTest "G-004 development-judge agent describes the auditor role" {
    $judge = Get-Content -LiteralPath (Join-Path $script:ProjectRoot ".claude\agents\development-judge.md") -Raw
    Assert-Fv ($judge -match "ALLOW_WITH_DEBT") "Judge agent must document ALLOW_WITH_DEBT."
    Assert-Fv ($judge -match "(?i)auditor") "Judge agent must describe itself as an auditor."
}

Register-FvTest "G-005 non-force git push is standing-authorized in settings, force stays denied" {
    $settings = Get-Content -LiteralPath (Join-Path $script:ProjectRoot ".claude\settings.json") -Raw | ConvertFrom-Json
    Assert-Fv (@($settings.permissions.allow) -contains "PowerShell(git push*)") "git push must be in allow."
    Assert-Fv (-not (@($settings.permissions.ask) -contains "PowerShell(git push*)")) "git push must no longer be in ask."
    Assert-Fv (@($settings.permissions.deny) -contains "PowerShell(*git push --force*)") "force push must remain denied."
}

Register-FvTest "G-006 command policy no longer human-gates git push and protects project-director" {
    $policy = Get-Content -LiteralPath (Join-Path $script:ProjectRoot ".claude\judge\command-policy.json") -Raw | ConvertFrom-Json
    $humanIds = @($policy.requireHuman | ForEach-Object { $_.id })
    Assert-Fv (-not ($humanIds -contains "cmd.git-push")) "cmd.git-push must not require human approval anymore."
    Assert-Fv (@($policy.protectedPatterns) -contains ".claude/agents/project-director.md") "project-director must be a protected asset."
}

Register-FvTest "G-007 safety floor is intact: constitution forbids self-expansion of authority" {
    $constitution = Get-Content -LiteralPath (Join-Path $script:ProjectRoot ".claude\judge\constitution.md") -Raw
    Assert-Fv ($constitution -match "(?i)only the user") "Constitution must reserve boundary changes to the user."
    Assert-Fv ($constitution -match "(?i)read-only") "Judge must remain read-only."
}
