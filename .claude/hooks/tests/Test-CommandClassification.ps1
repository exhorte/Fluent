Import-Module (Join-Path $script:TestRoot "modules\CommandClassification.psm1") -Force -WarningAction SilentlyContinue

Register-FvTest "Command classifier normalizes chained destructive command" {
    $result = Classify-FvCommand -Command "Write-Output ok ; git reset --hard"
    Assert-Fv ($result.Verdict -eq "DENY") "Expected DENY."
}

Register-FvTest "Command classifier allows non-force git push as R1 (ADR-0007)" {
    $result = Classify-FvCommand -Command "git push origin work-branch"
    Assert-Fv ($result.Verdict -eq "ALLOW") "Expected ALLOW for non-force push."
    Assert-Fv (-not $result.RequiresContract) "Non-force push must not require a contract."
}

Register-FvTest "Command classifier denies force git push" {
    Assert-Fv ((Classify-FvCommand -Command "git push --force origin main").Verdict -eq "DENY") "Expected DENY for --force."
    Assert-Fv ((Classify-FvCommand -Command "git push -f origin main").Verdict -eq "DENY") "Expected DENY for -f."
    Assert-Fv ((Classify-FvCommand -Command "git push --force-with-lease").Verdict -eq "DENY") "Expected DENY for --force-with-lease."
}

Register-FvTest "Command classifier gates deployment behind the active contract (R1/R2)" {
    $result = Classify-FvCommand -Command "kubectl apply -f deploy.yaml"
    Assert-Fv ($result.Verdict -eq "ALLOW") "Deployment is ALLOW at classifier level."
    Assert-Fv ($result.RequiresContract) "Deployment must require a recorded contract command family."
}

Register-FvTest "Command classifier still asks user for registry and admin install" {
    Assert-Fv ((Classify-FvCommand -Command "reg add HKLM\Software\Fluent /v X /d 1").Verdict -eq "ASK_USER") "Registry must ASK_USER."
    Assert-Fv ((Classify-FvCommand -Command "winget install Some.Tool").Verdict -eq "ASK_USER") "Admin install must ASK_USER."
}

Register-FvTest "Command classifier treats git status as low risk allow" {
    $result = Classify-FvCommand -Command "git status --short"
    Assert-Fv ($result.Verdict -eq "ALLOW") "Expected ALLOW."
    Assert-Fv ($result.RiskLevel -eq "low") "Expected low risk."
}
