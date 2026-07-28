using System.Globalization;
using Dapper;
using Microsoft.Data.Sqlite;
using Fluent.Core.History;

namespace Fluent.Persistence.History;

/// <summary>
/// Local SQLite store for dictation history. Uses a dedicated database file so
/// it never interferes with the single-table dictionary database. History is
/// opt-in (disabled by default); no audio or secret is ever stored.
/// </summary>
public sealed class SqliteDictationHistoryStore : IDictationHistoryStore
{
    private const int SupportedSchemaVersion = 1;
    private const int BusyTimeoutMilliseconds = 3000;
    private const int CommandTimeoutSeconds = 3;

    private const int MaximumStoredIdBytes = 64;
    private const int MaximumStoredTimestampBytes = 128;
    private const int MaximumStoredTextBytes =
        DictationHistoryLimits.MaximumTextLength * 4;
    private const int MaximumStoredProfileIdBytes =
        DictationHistoryLimits.MaximumProfileIdLength * 4;

    private const string CreateSchemaSql =
        """
        CREATE TABLE dictation_history
        (
            seq INTEGER PRIMARY KEY AUTOINCREMENT,
            id TEXT NOT NULL UNIQUE,
            created_utc TEXT NOT NULL,
            text TEXT NOT NULL,
            profile_id TEXT
        );
        CREATE TABLE history_preference
        (
            id INTEGER PRIMARY KEY,
            enabled INTEGER NOT NULL
        );
        INSERT INTO history_preference (id, enabled) VALUES (1, 0);
        """;

    private const string LoadEntriesSql =
        """
        SELECT
            id AS Id,
            created_utc AS CreatedUtc,
            text AS Text,
            profile_id AS ProfileId,
            length(CAST(id AS BLOB)) AS IdByteCount,
            length(CAST(created_utc AS BLOB)) AS CreatedUtcByteCount,
            length(CAST(text AS BLOB)) AS TextByteCount,
            length(CAST(COALESCE(profile_id, '') AS BLOB)) AS ProfileIdByteCount
        FROM dictation_history
        ORDER BY seq DESC
        LIMIT @LoadLimit;
        """;

    private const string InsertSql =
        """
        INSERT INTO dictation_history (id, created_utc, text, profile_id)
        VALUES (@Id, @CreatedUtc, @Text, @ProfileId);
        """;

    private const string PruneSql =
        """
        DELETE FROM dictation_history
        WHERE seq NOT IN
        (
            SELECT seq FROM dictation_history ORDER BY seq DESC LIMIT @KeepLimit
        );
        """;

    private const string DeleteSql =
        "DELETE FROM dictation_history WHERE id = @Id;";

    private const string ClearSql = "DELETE FROM dictation_history;";

    private const string SetEnabledSql =
        "UPDATE history_preference SET enabled = @Enabled WHERE id = 1;";

    private readonly Lazy<string> _databasePath;

    public SqliteDictationHistoryStore()
    {
        _databasePath = new Lazy<string>(
            () => Path.GetFullPath(FluentDataPath.GetDefaultHistoryDatabasePath()),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public SqliteDictationHistoryStore(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        string fullDatabasePath = Path.GetFullPath(databasePath);
        _databasePath = new Lazy<string>(
            () => fullDatabasePath,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public string DatabasePath => _databasePath.Value;

    public Task<DictationHistorySnapshot> InitializeAndLoadAsync(
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => InitializeAndLoad(cancellationToken),
            cancellationToken);
    }

    public Task AppendAsync(
        DictationHistoryEntry entry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return Task.Run(() => Append(entry, cancellationToken), cancellationToken);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.Run(() => Delete(id, cancellationToken), cancellationToken);
    }

    public Task<int> ClearAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => Clear(cancellationToken), cancellationToken);
    }

    public Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken)
    {
        return Task.Run(() => SetEnabled(enabled, cancellationToken), cancellationToken);
    }

    private DictationHistorySnapshot InitializeAndLoad(
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string databasePath = DatabasePath;

        if (!File.Exists(databasePath))
        {
            return CreateMissingDatabase(databasePath, cancellationToken);
        }

        return InspectExistingDatabase(databasePath, cancellationToken);
    }

    private static DictationHistorySnapshot CreateMissingDatabase(
        string databasePath,
        CancellationToken cancellationToken)
    {
        string databaseDirectory = Path.GetDirectoryName(databasePath)
            ?? throw new InvalidOperationException(
                "The database path has no parent directory.");

        Directory.CreateDirectory(databaseDirectory);
        cancellationToken.ThrowIfCancellationRequested();

        using SqliteConnection connection = OpenConnection(
            databasePath,
            SqliteOpenMode.ReadWriteCreate);

        int schemaVersion = ReadSchemaVersion(connection);
        EnsureSupportedSchemaVersion(schemaVersion);

        if (schemaVersion == 0)
        {
            EnsureVersionZeroDatabaseIsEmpty(connection);
            CreateVersionOneSchema(connection, cancellationToken);
        }
        else
        {
            ValidateVersionOneSchema(connection);
        }

        return Load(connection, cancellationToken);
    }

    private static DictationHistorySnapshot InspectExistingDatabase(
        string databasePath,
        CancellationToken cancellationToken)
    {
        using (SqliteConnection readOnlyConnection = OpenConnection(
                   databasePath,
                   SqliteOpenMode.ReadOnly))
        {
            int schemaVersion = ReadSchemaVersion(readOnlyConnection);
            EnsureSupportedSchemaVersion(schemaVersion);

            if (schemaVersion == SupportedSchemaVersion)
            {
                ValidateVersionOneSchema(readOnlyConnection);
                return Load(readOnlyConnection, cancellationToken);
            }

            EnsureVersionZeroDatabaseIsEmpty(readOnlyConnection);
        }

        cancellationToken.ThrowIfCancellationRequested();

        using SqliteConnection writableConnection = OpenConnection(
            databasePath,
            SqliteOpenMode.ReadWrite);
        EnsureVersionZeroDatabaseIsEmpty(writableConnection);
        CreateVersionOneSchema(writableConnection, cancellationToken);
        return Load(writableConnection, cancellationToken);
    }

    private void Append(
        DictationHistoryEntry entry,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NormalizedEntry normalized = Normalize(entry);

        using SqliteConnection connection = OpenConnection(
            DatabasePath,
            SqliteOpenMode.ReadWrite);
        EnsureWritableVersionOneDatabase(connection);
        cancellationToken.ThrowIfCancellationRequested();

        using SqliteTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            InsertSql,
            new
            {
                normalized.Id,
                normalized.CreatedUtc,
                normalized.Text,
                normalized.ProfileId
            },
            transaction,
            commandTimeout: CommandTimeoutSeconds);

        connection.Execute(
            PruneSql,
            new { KeepLimit = DictationHistoryLimits.MaximumEntryCount },
            transaction,
            commandTimeout: CommandTimeoutSeconds);

        transaction.Commit();
    }

    private bool Delete(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using SqliteConnection connection = OpenConnection(
            DatabasePath,
            SqliteOpenMode.ReadWrite);
        EnsureWritableVersionOneDatabase(connection);
        cancellationToken.ThrowIfCancellationRequested();

        int affectedRows = connection.Execute(
            DeleteSql,
            new { Id = FormatId(id) },
            commandTimeout: CommandTimeoutSeconds);

        return affectedRows == 1;
    }

    private int Clear(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using SqliteConnection connection = OpenConnection(
            DatabasePath,
            SqliteOpenMode.ReadWrite);
        EnsureWritableVersionOneDatabase(connection);
        cancellationToken.ThrowIfCancellationRequested();

        return connection.Execute(ClearSql, commandTimeout: CommandTimeoutSeconds);
    }

    private void SetEnabled(bool enabled, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using SqliteConnection connection = OpenConnection(
            DatabasePath,
            SqliteOpenMode.ReadWrite);
        EnsureWritableVersionOneDatabase(connection);
        cancellationToken.ThrowIfCancellationRequested();

        connection.Execute(
            SetEnabledSql,
            new { Enabled = enabled ? 1 : 0 },
            commandTimeout: CommandTimeoutSeconds);
    }

    private static DictationHistorySnapshot Load(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        long enabledValue = connection.ExecuteScalar<long>(
            "SELECT enabled FROM history_preference WHERE id = 1;",
            commandTimeout: CommandTimeoutSeconds);

        if (enabledValue is not (0 or 1))
        {
            throw new InvalidDataException(
                "The stored history preference is invalid.");
        }

        EntryRow[] rows = connection.Query<EntryRow>(
                LoadEntriesSql,
                new { LoadLimit = DictationHistoryLimits.MaximumEntryCount + 1 },
                commandTimeout: CommandTimeoutSeconds)
            .ToArray();

        cancellationToken.ThrowIfCancellationRequested();

        if (rows.Length > DictationHistoryLimits.MaximumEntryCount)
        {
            throw new InvalidDataException(
                "The stored history exceeds its supported capacity.");
        }

        DictationHistoryEntry[] entries = new DictationHistoryEntry[rows.Length];
        for (int index = 0; index < rows.Length; index++)
        {
            entries[index] = MapValidatedRow(rows[index]);
        }

        return new DictationHistorySnapshot(
            new DictationHistoryPreferences(enabledValue == 1),
            Array.AsReadOnly(entries));
    }

    private static DictationHistoryEntry MapValidatedRow(EntryRow row)
    {
        if (row.Id is null ||
            row.CreatedUtc is null ||
            row.Text is null ||
            row.IdByteCount > MaximumStoredIdBytes ||
            row.CreatedUtcByteCount > MaximumStoredTimestampBytes ||
            row.TextByteCount > MaximumStoredTextBytes ||
            row.ProfileIdByteCount > MaximumStoredProfileIdBytes ||
            !Guid.TryParse(row.Id, out Guid id) ||
            !DateTimeOffset.TryParseExact(
                row.CreatedUtc,
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTimeOffset createdUtc))
        {
            throw new InvalidDataException(
                "The stored history contains invalid data.");
        }

        string? profileId = string.IsNullOrEmpty(row.ProfileId)
            ? null
            : row.ProfileId;

        return new DictationHistoryEntry(id, createdUtc, row.Text, profileId);
    }

    private static SqliteConnection OpenConnection(
        string databasePath,
        SqliteOpenMode mode)
    {
        SqliteRuntime.EnsureInitialized();

        SqliteConnectionStringBuilder connectionStringBuilder = new()
        {
            DataSource = databasePath,
            Mode = mode,
            Cache = SqliteCacheMode.Private,
            Pooling = false
        };

        SqliteConnection connection = new(connectionStringBuilder.ToString());
        try
        {
            connection.Open();
            connection.Execute(
                FormattableString.Invariant(
                    $"PRAGMA busy_timeout = {BusyTimeoutMilliseconds};"),
                commandTimeout: CommandTimeoutSeconds);
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    private static int ReadSchemaVersion(SqliteConnection connection)
    {
        long schemaVersion = connection.ExecuteScalar<long>(
            "PRAGMA user_version;",
            commandTimeout: CommandTimeoutSeconds);

        if (schemaVersion is < int.MinValue or > int.MaxValue)
        {
            throw new NotSupportedException(
                "The database schema version is outside the supported range.");
        }

        return (int)schemaVersion;
    }

    private static void EnsureSupportedSchemaVersion(int schemaVersion)
    {
        if (schemaVersion is 0 or SupportedSchemaVersion)
        {
            return;
        }

        throw new NotSupportedException(
            $"Database schema version {schemaVersion} is not supported. " +
            $"Supported versions are 0 and {SupportedSchemaVersion}.");
    }

    private static void EnsureWritableVersionOneDatabase(
        SqliteConnection connection)
    {
        int schemaVersion = ReadSchemaVersion(connection);
        if (schemaVersion != SupportedSchemaVersion)
        {
            throw new NotSupportedException(
                $"Database schema version {schemaVersion} is not writable. " +
                $"Expected version {SupportedSchemaVersion}.");
        }

        ValidateVersionOneSchema(connection);
    }

    private static void EnsureVersionZeroDatabaseIsEmpty(
        SqliteConnection connection)
    {
        long existingUserObjectCount = connection.ExecuteScalar<long>(
            """
            SELECT COUNT(*)
            FROM sqlite_master
            WHERE name NOT LIKE 'sqlite_%';
            """,
            commandTimeout: CommandTimeoutSeconds);

        if (existingUserObjectCount != 0)
        {
            throw new InvalidDataException(
                "A version-zero database with an existing schema cannot be migrated safely.");
        }
    }

    private static void ValidateVersionOneSchema(SqliteConnection connection)
    {
        long tableCount = connection.ExecuteScalar<long>(
            """
            SELECT COUNT(*)
            FROM sqlite_master
            WHERE type = 'table'
              AND name NOT LIKE 'sqlite_%';
            """,
            commandTimeout: CommandTimeoutSeconds);

        string[] historyColumns = connection.Query<string>(
                "SELECT name FROM pragma_table_info('dictation_history') ORDER BY cid;",
                commandTimeout: CommandTimeoutSeconds)
            .ToArray();

        string[] preferenceColumns = connection.Query<string>(
                "SELECT name FROM pragma_table_info('history_preference') ORDER BY cid;",
                commandTimeout: CommandTimeoutSeconds)
            .ToArray();

        long preferenceRowCount = connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM history_preference WHERE id = 1;",
            commandTimeout: CommandTimeoutSeconds);

        bool hasExpectedSchema =
            tableCount == 2 &&
            historyColumns.SequenceEqual(
                ["seq", "id", "created_utc", "text", "profile_id"],
                StringComparer.Ordinal) &&
            preferenceColumns.SequenceEqual(
                ["id", "enabled"],
                StringComparer.Ordinal) &&
            preferenceRowCount == 1;

        if (!hasExpectedSchema)
        {
            throw new InvalidDataException(
                "The version-one history schema is invalid.");
        }
    }

    private static void CreateVersionOneSchema(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        using SqliteTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            CreateSchemaSql,
            transaction: transaction,
            commandTimeout: CommandTimeoutSeconds);

        cancellationToken.ThrowIfCancellationRequested();

        connection.Execute(
            $"PRAGMA user_version = {SupportedSchemaVersion};",
            transaction: transaction,
            commandTimeout: CommandTimeoutSeconds);

        cancellationToken.ThrowIfCancellationRequested();
        transaction.Commit();
    }

    private static NormalizedEntry Normalize(DictationHistoryEntry entry)
    {
        if (entry.Id == Guid.Empty)
        {
            throw new ArgumentException(
                "The history entry identifier must not be empty.",
                nameof(entry));
        }

        string text = entry.Text?.Trim()
            ?? throw new ArgumentException(
                "The history entry text must not be null.",
                nameof(entry));

        if (text.Length == 0)
        {
            throw new ArgumentException(
                "The history entry text must not be empty.",
                nameof(entry));
        }

        if (text.Length > DictationHistoryLimits.MaximumTextLength)
        {
            throw new ArgumentException(
                "The history entry text exceeds the supported length.",
                nameof(entry));
        }

        string? profileId = string.IsNullOrWhiteSpace(entry.ProfileId)
            ? null
            : entry.ProfileId.Trim();

        if (profileId is { Length: > DictationHistoryLimits.MaximumProfileIdLength })
        {
            throw new ArgumentException(
                "The history entry profile identifier exceeds the supported length.",
                nameof(entry));
        }

        return new NormalizedEntry(
            FormatId(entry.Id),
            entry.CreatedUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            text,
            profileId);
    }

    private static string FormatId(Guid id)
    {
        return id.ToString("D", CultureInfo.InvariantCulture);
    }

    private sealed record NormalizedEntry(
        string Id,
        string CreatedUtc,
        string Text,
        string? ProfileId);

    private sealed class EntryRow
    {
        public string? Id { get; set; }

        public string? CreatedUtc { get; set; }

        public string? Text { get; set; }

        public string? ProfileId { get; set; }

        public long IdByteCount { get; set; }

        public long CreatedUtcByteCount { get; set; }

        public long TextByteCount { get; set; }

        public long ProfileIdByteCount { get; set; }
    }
}
