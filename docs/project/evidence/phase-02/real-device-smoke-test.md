# Phase 02 Real-Device Smoke Test

Status: PASS_WITH_QUALITY_ISSUES

## Single Test

1. Launch `src\Fluent.App\bin\Release\net10.0-windows\Fluent.App.exe`.
2. Focus a normal editable field in Notepad.
3. Press Ctrl+Space, say `Bonjour, ceci est un test de transcription locale`, then press Ctrl+Space again.
4. On first use, allow the model download to finish.

Pass when the spoken French text is inserted in Notepad and no audio file is created. If it fails, record the exact status and message displayed by Fluent below.

Result: PASS for microphone capture, local transcription, and text delivery.

Observed status/message:

- User report on 2026-07-14: the application records, transcribes, and returns text.
- Perceived stop-to-text latency is too high.
- Word recognition accuracy is not sufficient.
- No microphone, model-loading, or insertion failure was reported.
