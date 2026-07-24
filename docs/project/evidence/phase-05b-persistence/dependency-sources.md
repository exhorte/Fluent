# Phase 05B Dependency Sources

Verified on 2026-07-16 before restore.

## Rejected Default Bundle

The first approved restore of `Microsoft.Data.Sqlite 10.0.10` was blocked by `NU1903` because it resolved `SQLitePCLRaw.lib.e_sqlite3 2.1.11`, affected by the high-severity advisory `GHSA-2m69-gcr7-jv3q` / `CVE-2025-6965`.

- Advisory: <https://github.com/advisories/GHSA-2m69-gcr7-jv3q>
- Affected package versions: `SQLitePCLRaw.lib.e_sqlite3 <= 2.1.11`.
- Required SQLite version: `3.50.2` or newer.
- No warning suppression or audit bypass was introduced.

## Microsoft.Data.Sqlite.Core

- Selected stable version: `10.0.10`.
- Official package source: <https://www.nuget.org/packages/Microsoft.Data.Sqlite.Core/>
- Official provider overview: <https://learn.microsoft.com/dotnet/standard/data/sqlite/>
- Microsoft documents that this Core package intentionally allows an application to supply a different native SQLite binary.

## SQLitePCLRaw.bundle_e_sqlite3

- Selected stable bundle version: `3.0.3`.
- Official package source: <https://www.nuget.org/packages/SQLitePCLRaw.bundle_e_sqlite3/>
- The bundle resolves `SourceGear.sqlite3 >= 3.50.4.5`, newer than the advisory's required SQLite `3.50.2`.
- Fluent must call `SQLitePCL.Batteries_V2.Init()` inside `Fluent.Persistence` before opening a connection.

## Dapper

- Selected stable version: `2.1.79`.
- Official package source: <https://www.nuget.org/packages/Dapper/>
- Version `2.1.79` was already present in the local NuGet cache before Phase 05B.

## I/O Constraint

Microsoft documents that SQLite does not support asynchronous I/O and Microsoft.Data.Sqlite async ADO.NET methods execute synchronously. Phase 05B therefore performs the bounded synchronous database work on a background worker and never on the WPF dispatcher:

<https://learn.microsoft.com/dotnet/standard/data/sqlite/async>

## Judge Amendment

The Development Judge approved the dependency amendment after the `NU1903` failure. The default `Microsoft.Data.Sqlite` bundle remains forbidden for Phase 05B.

## Resolved Graph

The corrective mono-process restore passed. `src/Fluent.Persistence/obj/project.assets.json` resolves:

- `Microsoft.Data.Sqlite.Core 10.0.10`
- `Dapper 2.1.79`
- `SQLitePCLRaw.bundle_e_sqlite3 3.0.3`
- `SQLitePCLRaw.core 3.0.3`
- `SQLitePCLRaw.config.e_sqlite3 3.0.3`
- `SQLitePCLRaw.provider.e_sqlite3 3.0.3`
- `SourceGear.sqlite3 3.50.4.5`

`SQLitePCLRaw.lib.e_sqlite3 2.1.11` is absent from the resolved graph.
