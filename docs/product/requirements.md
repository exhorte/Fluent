# Requirements

## Functional

- FR-001: The application shall toggle recording with Ctrl+Space.
- FR-002: The application shall display a floating recording capsule while recording.
- FR-003: The application shall transcribe French speech locally by default.
- FR-004: The application shall apply one selected transformation profile.
- FR-005: The application shall insert output only into the original validated target.
- FR-006: The application shall support a personal dictionary.
- FR-007: The application shall provide a local dashboard.

## Non-Functional

- NFR-001: The MVP shall run on Windows only.
- NFR-002: Core domain code shall not depend on WPF or Win32.
- NFR-003: Win32 calls shall be isolated behind interfaces.
- NFR-004: Build and test evidence shall be versioned for each phase.

## Security

- SEC-001: The application shall never paste automatically into password fields.
- SEC-002: The application shall never send Enter automatically.
- SEC-003: Dictated commands shall never be executed automatically.
- SEC-004: If the target changes, insertion shall be blocked and text copied to clipboard as fallback.

## Privacy

- PRIV-001: Audio shall not be saved by default.
- PRIV-002: Telemetry shall not be collected.
- PRIV-003: Cloud processing shall require future explicit consent and a product decision.

## Performance

- PERF-001: Recording and transcription latency shall be measured before optimization claims.
- PERF-002: Speech benchmarks shall include French and developer-oriented samples.

## User Experience

- UX-001: The floating capsule shall not steal focus.
- UX-002: Failure states shall be explicit and recoverable.
- UX-003: The user shall remain in the target application whenever safe.
