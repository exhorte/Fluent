# Phase 05B Focused Review

Date: 2026-07-16
Final result: PASS
Repair cycles used: 2 / 2

## Review Cycle 1

Architecture, security, WPF, and test reviewers identified the following material issues:

- SQLite key identity did not exactly match `StringComparer.OrdinalIgnoreCase` for a Unicode edge case.
- Existing databases were opened writable before their version and schema were classified.
- Negative schema versions were not rejected.
- Default LocalAppData resolution could fail before the coordinator entered its caught fallback path.
- Persisted loading needed explicit row and value bounds.
- Some persistent versus session-only UI/result wording was ambiguous.
- Corrupt, locked, concurrent, cancellation, and real-capacity paths needed stronger tests.

All findings were repaired. The implementation now uses the registered `NYX_ORDINAL_IGNORE_CASE` collation, inspects existing databases read-only, rejects unsupported versions, resolves the default path lazily inside the caught initialization path, bounds stored data, and reports truthful storage mode. The requested regression and integration tests were added.

## Review Cycle 2

The reviewers found one remaining material issue: schema validation trusted marker text inside `sqlite_master.sql`. A manipulated ROWID/BINARY table could place `NYX_ORDINAL_IGNORE_CASE` and `WITHOUT ROWID` in a comment or constraint and pass that textual check.

The repair replaced textual validation with structural metadata:

- `pragma_table_list` verifies the table, column count, and real `WITHOUT ROWID` mode.
- `pragma_index_list` verifies the unique, non-partial primary-key index.
- `pragma_index_xinfo` verifies that the first and only key column is `spoken_form` with the real `NYX_ORDINAL_IGNORE_CASE` collation.

Independent tests now reject:

- a `WITHOUT ROWID` table with a BINARY primary key;
- a ROWID table with the expected collation;
- a ROWID/BINARY table containing both expected strings only as decoy literals;
- an oversized persisted spoken form.

## Final Independent Verdicts

- Architecture and migration review: PASS; no material blocking defect remains.
- Security and privacy review: PASS; no material blocking defect remains.
- Test review: PASS; the structural bypasses and bounded-load paths are covered.
- WPF integration findings from cycle 1 remain repaired: initialization precedes hotkey registration and persistent/session wording is truthful.

## Verification Considered

- Release build: 0 warnings, 0 errors.
- Focused persistence tests: 19 / 19 passed.
- Focused rewrite dictionary tests: 55 / 55 passed.
- Focused persistent integration tests: 3 / 3 passed.
- Complete solution: 147 / 147 tests passed.

The reviewers performed their final inspections read-only and made no project changes.

