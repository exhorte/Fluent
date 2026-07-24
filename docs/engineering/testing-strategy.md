# Testing Strategy

## Phase 00

- Deterministic PowerShell hook tests.
- JSON and frontmatter validation.
- .NET build.
- Initial .NET architecture tests.
- Adversarial command/path fixtures.

## Future Phases

- Unit tests for domain and validation logic.
- Integration tests for Windows adapters when feasible.
- Manual harnesses for focus, hotkey, overlay, and insertion behavior.
- Speech benchmarks with versioned French fixtures.

Tests must be deterministic and must not require Internet access.
