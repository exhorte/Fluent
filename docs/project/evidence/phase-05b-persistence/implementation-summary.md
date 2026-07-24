# Phase 05B Implementation Summary

Date: 2026-07-16
Task: `FV-P05-T002`
Status: implemented and automatically verified; user Windows restart smoke remains pending.

## Delivered

- Added centrally pinned `Microsoft.Data.Sqlite.Core` 10.0.10, `Dapper` 2.1.79, and `SQLitePCLRaw.bundle_e_sqlite3` 3.0.3 only to `Fluent.Persistence`.
- Added the narrow `IPersonalDictionaryStore` Core boundary and storage record.
- Added the production database path `%LOCALAPPDATA%\Fluent\fluent.db` with injected paths for every automated test.
- Added explicit SQLite runtime initialization and version-one transactional schema creation.
- Persisted only normalized spoken form, replacement, and update timestamp.
- Added parameterized, bounded, serialized load, upsert, and delete operations outside the WPF dispatcher.
- Added deterministic validated hydration with the accepted 200-entry and value-length limits.
- Added a custom SQLite collation backed by `StringComparer.OrdinalIgnoreCase` so disk identity matches in-memory identity, including Unicode edge cases.
- Added structural schema validation through `pragma_table_list`, `pragma_index_list`, and `pragma_index_xinfo`; schema comments, literals, or constraints cannot impersonate `WITHOUT ROWID` or the required primary-key collation.
- Added read-only inspection before accepting or rejecting an existing database. Corrupt, locked, invalid, over-capacity, negative-version, or newer databases are not repaired or overwritten.
- Added a serialized persistent coordinator that publishes memory only after a successful disk write and enters an explicit empty session-only fallback after storage failure.
- Integrated asynchronous dictionary initialization and CRUD into the WPF application.
- Added truthful `CHARGEMENT LOCAL`, `LOCAL · ENREGISTRÉ`, and `SESSION · NON ENREGISTRÉ` states.
- Preserved the accepted immutable per-dictation snapshot, Professional French rewrite ordering, target recapture, password blocking, clipboard fallback, no-Enter behavior, audio privacy, and shutdown handling.

## Verification

- Release solution build: 0 warnings, 0 errors.
- Focused persistence tests: 19 / 19 passed.
- Focused rewrite dictionary tests: 55 / 55 passed.
- Focused persistent integration tests: 3 / 3 passed.
- Complete suite: 147 / 147 passed.
- Corrupt and locked database fallback, restart hydration, rollback-on-write-failure, cancellation, capacity, SQL-input, Unicode identity, wrong-collation, rowid, and schema-token-decoy paths are covered.

## Remaining Closure Gate

The product phase remains active until the user verifies on Windows that a correction survives a complete application restart and explicitly accepts the result. No real LocalAppData database was created or migrated by automated tests.

