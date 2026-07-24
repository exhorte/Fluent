# Threat Model

## Threats

- Wrong target field.
- Password field insertion.
- Clipboard leakage.
- Elevated target application.
- Keyboard injection.
- Dictated terminal command.
- Audio retention.
- Malicious or compromised local model.
- Prompt injection in dictated text.
- Compromised dependency.
- Sensitive logs.
- Secret exposure.

## Initial Mitigations

- Password-field blocking.
- Target identity verification.
- Clipboard fallback on target loss.
- No automatic Enter.
- No command execution.
- No audio saved by default.
- No telemetry.
- Minimal logs with redaction.
- Win32 isolation behind testable interfaces.
