# Phase 05B - Persistent Local Dictionary

Status: COMPLETED AND USER-ACCEPTED

## Objective

Persist the accepted personal dictionary locally with SQLite so validated corrections survive an application restart without changing the transcription, rewrite, target-safety, or insertion behavior.

## Included

- A versioned SQLite schema owned by `Fluent.Persistence`.
- One injected application-data path with the production default under `%LOCALAPPDATA%\Fluent`.
- Parameterized access through Microsoft.Data.Sqlite.Core and Dapper with the patched SQLitePCLRaw 3 bundle.
- Off-UI-thread initialization, load, upsert, and delete operations.
- Validation of every loaded row against the accepted Phase 05A dictionary rules.
- Deterministic hydration of one in-memory working dictionary used by dictation snapshots.
- Persisted add, update, and delete operations with memory updated only after a successful database write.
- A truthful `LOCAL · ENREGISTRÉ` UI state and an explicit session-only fallback when storage initialization fails.
- Temporary-file persistence tests, focused rewrite tests, a clean Release build, the complete suite, focused reviews, evidence, and a Windows restart smoke.

## Excluded

- Audio, transcript, rewrite-output, history, analytics, telemetry, cloud, synchronization, accounts, or secrets in SQLite.
- Automatic deletion, repair, replacement, or migration of an incompatible or corrupt real user database.
- Changes to Whisper, recording, hotkey, Win32, capsule, target identity, password protection, clipboard fallback, insertion policy, or Enter behavior.
- Ollama, additional rewrite profiles, packaging, deployment, registry changes, administrator installation, Git commit, push, release, or publication.
- Automated tests against the real `%LOCALAPPDATA%\Fluent` directory.

## Storage Contract

- Production database: `%LOCALAPPDATA%\Fluent\fluent.db`.
- Test databases: unique injected temporary directories deleted by their owning fixtures.
- Schema version 1 stores only normalized spoken form, replacement, and local update timestamp.
- Migrations run transactionally and are idempotent.
- A database newer than the supported schema is opened fail-closed and is not modified.
- A corrupt or inaccessible database remains untouched; Fluent continues with an explicitly disclosed session-only dictionary.
- No secret, audio sample, raw transcript, rewritten text, target metadata, or telemetry is stored.

## Completed Tasks

1. Add centrally pinned Microsoft.Data.Sqlite.Core, Dapper, and SQLitePCLRaw 3 bundle dependencies to `Fluent.Persistence`.
2. Define the Core persistence boundary and local dictionary storage records.
3. Implement the LocalAppData path provider, SQLite connection factory, migration runner, and repository.
4. Add a serialized persistent dictionary coordinator that validates loaded data and preserves the accepted session snapshot behavior.
5. Integrate asynchronous initialization and mutation handling into the existing Dictionary page.
6. Replace the Phase 05A non-persistence copy with truthful persistent and fallback states.
7. Add focused persistence, coordinator, and integration tests.
8. Run the bounded verification and review loop, then request a restart smoke before phase closure.

## Automated Verification

- Release solution build: PASS with 0 warnings and 0 errors.
- Focused persistence tests: 19 / 19 passed.
- Focused rewrite dictionary tests: 55 / 55 passed.
- Focused persistent integration tests: 3 / 3 passed.
- Complete solution: 147 / 147 tests passed.
- Architecture, migration, security, WPF, and test reviews: PASS after two bounded repair cycles.
- Final Development Judge: ALLOW to proceed to the user Windows restart smoke without closing the phase.
- Windows restart smoke: PASS; user accepted on 2026-07-17.
- Closure Development Judge: ALLOW on 2026-07-17; FV-P05-T002 and Phase 05B are closed.

## Acceptance

- Valid corrections survive disposal and recreation of the repository and application coordinator.
- Add, update, and delete operations are reflected in both SQLite and the in-memory snapshot.
- Case-insensitive uniqueness, entry limits, Unicode validation, protected-span processing, and non-recursive replacement behavior remain unchanged.
- Migration version 1 is transactional, repeatable, and refuses unsupported newer schemas without mutation.
- All SQL values are parameterized.
- Startup and UI mutations do not block the WPF dispatcher.
- Corrupt, inaccessible, invalid, or over-capacity persisted data is not silently accepted and does not prevent dictation from running with an empty session-only fallback.
- The UI never claims persistence before successful initialization and clearly reports a storage failure.
- Existing password, target-recapture, clipboard, no-Enter, local-audio, shutdown, and capsule protections remain unchanged.
- Automated tests never create or migrate the user's real LocalAppData database.
- Focused tests, clean Release build, complete suite, focused reviews, final Judge verdict, and user restart smoke pass before closure.

## Required Evidence

- `docs/project/evidence/phase-05b-persistence/plan-judge-verdict.json`
- `docs/project/evidence/phase-05b-persistence/dependency-amendment-judge-verdict.json`
- `docs/project/evidence/phase-05b-persistence/phase-05a-baseline-manifest.md`
- `docs/project/evidence/phase-05b-persistence/dependency-sources.md`
- `docs/project/evidence/phase-05b-persistence/dotnet-build.log`
- `docs/project/evidence/phase-05b-persistence/dotnet-test.log`
- `docs/project/evidence/phase-05b-persistence/focused-review.md`
- `docs/project/evidence/phase-05b-persistence/windows-restart-smoke.md`
- `docs/project/evidence/phase-05b-persistence/implementation-summary.md`
- `docs/project/evidence/phase-05b-persistence/development-judge-verdict.json`

## Risks

- A corrupt or manually edited database could contain invalid or duplicate rows.
- A migration or interrupted write could damage persisted state.
- SQLite operations could block the WPF dispatcher if executed directly by UI code.
- Memory and disk could diverge if mutation ordering is not serialized.
- LocalAppData can be unavailable or read-only.
- New package restore requires network access and writes to the local NuGet cache.

## Rollback

- Restore the accepted Phase 05A session-only coordinator and UI disclosure.
- Remove only the Phase 05B package references, persistence implementation, integration, and tests.
- Never delete an existing user database automatically during rollback.
- Rebuild and rerun the complete suite after rollback.
