# Phase 02 - Local French Dictation

Status: FUNCTIONALLY_VERIFIED_QUALITY_RECHECK_PENDING

## Objective

Deliver the smallest useful dictation path:

- start and stop default-microphone capture with Ctrl+Space;
- retain captured audio in memory only;
- transcribe French locally;
- insert the transcript only into the original validated target;
- keep all failure paths explicit and recoverable.

## Included

- NAudio WASAPI capture as 16 kHz, 16-bit, mono PCM.
- PCM16-to-normalized-float conversion without an intermediate audio file.
- Whisper.net CPU transcription in French.
- First-use multilingual Base Q8_0 model download to LocalAppData through a temporary file and atomic move.
- WPF recording, model preparation, and transcription states.
- Existing password and target-change insertion protections.
- Offline deterministic unit tests for capture lifecycle, conversion, transcript assembly, and insertion policy.

## Excluded

- Cloud services.
- Audio persistence or history.
- AI rewriting profiles.
- Dictionary, dashboard, packaging, and telemetry.
- GPU tuning and performance claims.

## Automated Acceptance

- Complete solution Release build: PASS, 0 warnings, 0 errors.
- Complete solution tests: PASS, 35 / 35 after the measured quality optimization.
- Audio is represented only by an in-memory sample array.
- Empty transcription cannot reach insertion.
- Unverified or unknown target security cannot reach the clipboard or input injection.
- No Enter key or dictated command execution was added.

## Remaining Acceptance

The real-device smoke test confirmed microphone capture, local transcription, and text delivery. One follow-up voice test must confirm whether the measured Q8_0 latency and quality improvement resolves the user's reported experience. Phase 01 remains separately awaiting its broader cross-application checklist.

## Required Evidence

- `docs/project/evidence/phase-02/plan-judge-verdict.json`
- `docs/project/evidence/phase-02/dotnet-build.log`
- `docs/project/evidence/phase-02/dotnet-test.log`
- `docs/project/evidence/phase-02/implementation-summary.md`
- `docs/project/evidence/phase-02/development-judge-verdict.json`
- a renewed `ALLOW` verdict after the real-device result
