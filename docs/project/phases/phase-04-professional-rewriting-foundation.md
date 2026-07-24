# Phase 04A - Professional French Safe Rewriting Foundation

Status: COMPLETED

## Objective

Apply a deterministic local Professional French formatting profile between transcription and insertion while guaranteeing exact raw-transcript fallback whenever rewriting is empty, invalid, cancelled, or fails.

## Included

- One fixed Professional French profile.
- Spacing and punctuation normalization only.
- Lexical and sensitive-token output validation.
- Exact source fallback on empty, failed, or invalid output.
- Cancellation propagation.
- Integration before the existing target revalidation and insertion policy.
- Honest stop-to-text timing through rewrite validation.
- Offline deterministic tests.

## Excluded

- Ollama, model installation or download, network access, and new packages.
- Additional profiles, profile-selection UI, dictionary, history, persistence, packaging, and cloud processing.
- Any change to audio, Whisper, hotkeys, insertion safety, Win32, permissions, or publication.

## Acceptance

- Rewriting never adds, removes, changes, duplicates, or reorders lexical content.
- Numbers, versions, URLs, paths, code segments, and identifiers remain exact.
- Unsafe output falls back to the exact original transcript.
- The real dictation flow uses only validated output or the exact transcript.
- Release build and all deterministic tests pass.

## Verified result

- Focused rewrite suite: 37 / 37 passed.
- Full Release build: 0 warnings and 0 errors.
- Complete solution suite: 71 / 71 passed.
- Focused integration review: no finding.
- Focused safety review after bounded repairs: no blocking finding.
- Final Development Judge verdict: ALLOW on 2026-07-15.

## Evidence

- `docs/project/evidence/phase-04-rewrite/plan-judge-verdict.json`
- `docs/project/evidence/phase-04-rewrite/phase-03-baseline-manifest.md`
- `docs/project/evidence/phase-04-rewrite/dotnet-build.log`
- `docs/project/evidence/phase-04-rewrite/dotnet-test.log`
- `docs/project/evidence/phase-04-rewrite/focused-review.md`
- `docs/project/evidence/phase-04-rewrite/implementation-summary.md`
- `docs/project/evidence/phase-04-rewrite/development-judge-verdict.json`
