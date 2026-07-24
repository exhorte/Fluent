# Phase 01 - Windows Interaction Spike

Status: IMPLEMENTED_AWAITING_MANUAL_VERIFICATION

## Objective

Prove the minimum Windows interaction path for Fluent without building dictation features:

- register Ctrl+Space;
- detect the active window and focused element;
- display a non-activating floating capsule;
- insert fixed spike text into the original target;
- fall back to clipboard when the target changes;
- prepare manual verification for Notepad, browser, VS Code, and Windows Terminal.

## Included

- Win32 hotkey adapter.
- Active target detector with UI Automation metadata when available.
- SendInput adapter for Ctrl+V only.
- WPF capsule configured to avoid activation.
- Fixed text insertion orchestration.
- Automated policy tests.
- Manual verification checklist.
- Development Judge verdict after verification.

## Excluded

- Audio recording.
- Speech-to-text.
- AI rewriting.
- Dictionary.
- Dashboard.
- Packaging.
- Sending Enter.
- Executing dictated commands.

## Acceptance Criteria

- Ctrl+Space registration and cleanup are isolated behind testable interfaces.
- Target identity is captured before the spike starts.
- Password targets are blocked.
- Target change prevents paste and uses clipboard fallback.
- The capsule does not activate.
- Only fixed spike text is inserted.
- Automated tests pass.
- Build passes.
- Manual checklist is present.
- Judge verdict is produced before any next phase.

## Required Evidence

- `docs/project/evidence/phase-01/dotnet-build.json`
- `docs/project/evidence/phase-01/dotnet-test.json`
- `docs/project/evidence/phase-01/manual-verification-checklist.md`
- `docs/project/evidence/phase-01/development-judge-verdict.json`

## Current Judge Verdict

The Development Judge verdict is `DENY` for continuation beyond Phase 01 until manual target-app verification is executed and recorded.
