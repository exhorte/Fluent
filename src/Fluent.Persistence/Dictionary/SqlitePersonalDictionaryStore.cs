using System.Globalization;
using Dapper;
using Microsoft.Data.Sqlite;
using Fluent.Core.Dictionary;

namespace Fluent.Persistence.Dictionary;

public sealed class SqlitePersonalDictionaryStore : IPersonalDictionaryStore
{
    private const int SupportedSchemaVersion = 1;
    private const int BusyTimeoutMilliseconds = 3000;
    private const int CommandTimeoutSeconds = 3;
    private const int MaximumStoredValueBytes = 1024;
    private const int MaximumStoredTimestampBytes = 128;
    private const string OrdinalIgnoreCaseCollation =
        "NYX_ORDINAL_IGNORE_CASE";

    private const string CreateSchemaSql =
        """
        CREATE TABLE personal_dictionary
        (
            spoken_form TEXT NOT NULL
                COLLATE NYX_ORDINAL_IGNORE_CASE
                PRIMARY KEY,
            replacement TEXT NOT NULL,
            updated_utc TEXT NOT NULL
        ) WITHOUT ROWID;
        """;

    private const string LoadSql =
        """
        SELECT
            CASE
                WHEN length(CAST(spoken_form AS BLOB)) <= @MaximumValueBytes
                THEN spoken_form
            END AS SpokenForm,
            CASE
                WHEN length(CAST(replacement AS BLOB)) <= @MaximumValueBytes
                THEN replacement
            END AS Replacement,
            CASE
                WHEN length(CAST(updated_utc AS BLOB)) <= @MaximumTimestampBytes
                THEN updated_utc
            END AS UpdatedUtc,
            length(CAST(spoken_form AS BLOB)) AS SpokenFormByteCount,
            length(CAST(replacement AS BLOB)) AS ReplacementByteCount,
            length(CAST(updated_utc AS BLOB)) AS UpdatedUtcByteCount
        FROM personal_dictionary
        ORDER BY spoken_form COLLATE NYX_ORDINAL_IGNORE_CASE
        LIMIT @LoadLimit;
        """;

    private const string UpsertSql =
        """
        INSERT INTO personal_dictionary
        (
            spoken_form,
            replacement,
            updated_utc
        )
        VALUES
        (
            @SpokenForm,
            @Replacement,
            @UpdatedUtc
        )
        ON CONFLICT(spoken_form) DO UPDATE SET
            spoken_form = excluded.spoken_form,
            replacement = excluded.replacement,
            updated_utc = excluded.updated_utc;
        """;

    private const string DeleteSql =
        """
        DELETE FROM personal_dictionary
        WHERE spoken_form = @SpokenForm;
        """;

    private readonly Lazy<string> _databasePath;

    public SqlitePersonalDictionaryStore()
    {
        _databasePath = new Lazy<string>(
            () => Path.GetFullPath(FluentDataPath.GetDefaultDatabasePath()),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public SqlitePersonalDictionaryStore(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        string fullDatabasePath = Path.GetFullPath(databasePath);
        _databasePath = new Lazy<string>(
            () => fullDatabasePath,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public string DatabasePath => _databasePath.Value;

    public Task<IReadOnlyList<PersonalDictionaryStorageEntry>> InitializeAndLoadAsync(
        CancellationToken cancellationToken)
    {
        return Task.Run<IReadOnlyList<PersonalDictionaryStorageEntry>>(
            () => InitializeAndLoad(cancellationToken),
            cancellationToken);
    }

    public Task UpsertAsync(
        PersonalDictionaryStorageEntry entry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return Task.Run(
            () => Upsert(entry, cancellationToken),
            cancellationToken);
    }

    public Task<bool> DeleteAsync(
        string spokenForm,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => Delete(spokenForm, cancellationToken),
            cancellationToken);
    }

    private IReadOnlyList<PersonalDictionaryStorageEntry> InitializeAndLoad(
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

    private static IReadOnlyList<PersonalDictionaryStorageEntry>
        CreateMissingDatabase(
            string databasePath,
            CancellationToken cancellationToken)
    {
        string databaseDirectory = Path.GetDirectoryName(databasePath)
            ?? throw new InvalidOperationException(
                "The database path has no parent directory.");

        Directory.CreateDirectory(databaseDirectory);
        cancellationToken.ThrowIfCancellationRequested();

        if (File.Exists(databasePath))
        {
            return InspectExistingDatabase(databasePath, cancellationToken);
        }

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

        return LoadValidatedEntries(connection, cancellationToken);
    }

    private static IReadOnlyList<PersonalDictionaryStorageEntry>
        InspectExistingDatabase(
            string databasePath,
            CancellationToken cancellationToken)
    {
        int schemaVersion;
        using (SqliteConnection readOnlyConnection = OpenConnection(
                   databasePath,
                   SqliteOpenMode.ReadOnly))
        {
            schemaVersion = ReadSchemaVersion(readOnlyConnection);
            EnsureSupportedSchemaVersion(schemaVersion);

            if (schemaVersion == SupportedSchemaVersion)
            {
                ValidateVersionOneSchema(readOnlyConnection);
                return LoadValidatedEntries(
                    readOnlyConnection,
                    cancellationToken);
            }

            EnsureVersionZeroDatabaseIsEmpty(readOnlyConnection);
        }

        cancellationToken.ThrowIfCancellationRequested();

        using SqliteConnection writableConnection = OpenConnection(
            databasePath,
            SqliteOpenMode.ReadWrite);
        schemaVersion = ReadSchemaVersion(writableConnection);
        EnsureSupportedSchemaVersion(schemaVersion);

        if (schemaVersion == 0)
        {
            EnsureVersionZeroDatabaseIsEmpty(writableConnection);
            CreateVersionOneSchema(writableConnection, cancellationToken);
        }
        else
        {
            ValidateVersionOneSchema(writableConnection);
        }

        return LoadValidatedEntries(writableConnection, cancellationToken);
    }

    private void Upsert(
        PersonalDictionaryStorageEntry entry,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NormalizedStorageEntry normalizedEntry = Normalize(entry);

        using SqliteConnection connection = OpenConnection(
            DatabasePath,
            SqliteOpenMode.ReadWrite);
        EnsureWritableVersionOneDatabase(connection);
        cancellationToken.ThrowIfCancellationRequested();

        connection.Execute(
            UpsertSql,
            new
            {
                normalizedEntry.SpokenForm,
                normalizedEntry.Replacement,
                UpdatedUtc = DateTimeOffset.UtcNow.ToString(
                    "O",
                    CultureInfo.InvariantCulture)
            },
            commandTimeout: CommandTimeoutSeconds);
    }

    private bool Delete(
        string spokenForm,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string normalizedSpokenForm = NormalizeRequiredValue(
            spokenForm,
            nameof(spokenForm));

        using SqliteConnection connection = OpenConnection(
            DatabasePath,
            SqliteOpenMode.ReadWrite);
        EnsureWritableVersionOneDatabase(connection);
        cancellationToken.ThrowIfCancellationRequested();

        int affectedRows = connection.Execute(
            DeleteSql,
            new { SpokenForm = normalizedSpokenForm },
            commandTimeout: CommandTimeoutSeconds);

        return affectedRows == 1;
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
            connection.CreateCollation(
                OrdinalIgnoreCaseCollation,
                StringComparer.OrdinalIgnoreCase.Compare);
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

    private static void ValidateVersionOneSchema(
        SqliteConnection connection)
    {
        long existingUserObjectCount = connection.ExecuteScalar<long>(
            """
            SELECT COUNT(*)
            FROM sqlite_master
            WHERE name NOT LIKE 'sqlite_%';
            """,
            commandTimeout: CommandTimeoutSeconds);

        SchemaTable? table = connection.QuerySingleOrDefault<SchemaTable>(
            """
            SELECT
                type AS ObjectType,
                ncol AS ColumnCount,
                wr AS WithoutRowId
            FROM pragma_table_list
            WHERE "schema" = 'main'
              AND name = 'personal_dictionary';
            """,
            commandTimeout: CommandTimeoutSeconds);

        SchemaColumn[] columns = connection.Query<SchemaColumn>(
                """
                SELECT
                    name AS Name,
                    upper(type) AS DeclaredType,
                    "notnull" AS IsNotNull,
                    pk AS PrimaryKeyOrder
                FROM pragma_table_info('personal_dictionary')
                ORDER BY cid;
                """,
                commandTimeout: CommandTimeoutSeconds)
            .ToArray();

        SchemaIndex[] primaryKeyIndexes = connection.Query<SchemaIndex>(
                """
                SELECT
                    name AS Name,
                    "unique" AS IsUnique,
                    origin AS Origin,
                    partial AS IsPartial
                FROM pragma_index_list('personal_dictionary')
                WHERE origin = 'pk'
                ORDER BY seq;
                """,
                commandTimeout: CommandTimeoutSeconds)
            .ToArray();

        SchemaIndexColumn[] primaryKeyColumns =
            primaryKeyIndexes.Length == 1
                ? connection.Query<SchemaIndexColumn>(
                        """
                        SELECT
                            seqno AS SequenceNumber,
                            cid AS ColumnId,
                            name AS ColumnName,
                            coll AS CollationName,
                            "key" AS IsKey
                        FROM pragma_index_xinfo(@IndexName)
                        WHERE "key" = 1
                        ORDER BY seqno;
                        """,
                        new
                        {
                            IndexName = primaryKeyIndexes[0].Name
                        },
                        commandTimeout: CommandTimeoutSeconds)
                    .ToArray()
                : [];

        bool hasExpectedSchema =
            existingUserObjectCount == 1 &&
            table is not null &&
            string.Equals(
                table.ObjectType,
                "table",
                StringComparison.Ordinal) &&
            table.ColumnCount == 3 &&
            table.WithoutRowId == 1 &&
            columns.Length == 3 &&
            IsExpectedColumn(columns[0], "spoken_form", primaryKeyOrder: 1) &&
            IsExpectedColumn(columns[1], "replacement", primaryKeyOrder: 0) &&
            IsExpectedColumn(columns[2], "updated_utc", primaryKeyOrder: 0) &&
            primaryKeyIndexes.Length == 1 &&
            IsExpectedPrimaryKeyIndex(primaryKeyIndexes[0]) &&
            primaryKeyColumns.Length == 1 &&
            IsExpectedPrimaryKeyColumn(primaryKeyColumns[0]);

        if (!hasExpectedSchema)
        {
            throw new InvalidDataException(
                "The version-one dictionary schema is invalid.");
        }
    }

    private static bool IsExpectedColumn(
        SchemaColumn column,
        string expectedName,
        int primaryKeyOrder)
    {
        return string.Equals(
                column.Name,
                expectedName,
                StringComparison.Ordinal) &&
            string.Equals(
                column.DeclaredType,
                "TEXT",
                StringComparison.Ordinal) &&
            column.IsNotNull == 1 &&
            column.PrimaryKeyOrder == primaryKeyOrder;
    }

    private static bool IsExpectedPrimaryKeyIndex(
        SchemaIndex index)
    {
        return !string.IsNullOrWhiteSpace(index.Name) &&
            index.IsUnique == 1 &&
            string.Equals(index.Origin, "pk", StringComparison.Ordinal) &&
            index.IsPartial == 0;
    }

    private static bool IsExpectedPrimaryKeyColumn(
        SchemaIndexColumn column)
    {
        return column.SequenceNumber == 0 &&
            column.ColumnId == 0 &&
            column.IsKey == 1 &&
            string.Equals(
                column.ColumnName,
                "spoken_form",
                StringComparison.Ordinal) &&
            string.Equals(
                column.CollationName,
                OrdinalIgnoreCaseCollation,
                StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<PersonalDictionaryStorageEntry>
        LoadValidatedEntries(
            SqliteConnection connection,
            CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        StorageRow[] rows = connection.Query<StorageRow>(
                LoadSql,
                new
                {
                    MaximumValueBytes = MaximumStoredValueBytes,
                    MaximumTimestampBytes = MaximumStoredTimestampBytes,
                    LoadLimit = PersonalDictionaryLimits.MaximumEntryCount + 1
                },
                commandTimeout: CommandTimeoutSeconds)
            .ToArray();

        cancellationToken.ThrowIfCancellationRequested();

        if (rows.Length > PersonalDictionaryLimits.MaximumEntryCount)
        {
            throw new InvalidDataException(
                "The stored dictionary exceeds its supported capacity.");
        }

        PersonalDictionaryStorageEntry[] entries = new
            PersonalDictionaryStorageEntry[rows.Length];

        for (int index = 0; index < rows.Length; index++)
        {
            StorageRow row = rows[index];
            if (row.SpokenForm is null ||
                row.Replacement is null ||
                row.UpdatedUtc is null ||
                row.SpokenFormByteCount > MaximumStoredValueBytes ||
                row.ReplacementByteCount > MaximumStoredValueBytes ||
                row.UpdatedUtcByteCount > MaximumStoredTimestampBytes ||
                !DateTimeOffset.TryParseExact(
                    row.UpdatedUtc,
                    "O",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _))
            {
                throw new InvalidDataException(
                    "The stored dictionary contains invalid data.");
            }

            entries[index] = new PersonalDictionaryStorageEntry(
                row.SpokenForm,
                row.Replacement);
        }

        return Array.AsReadOnly(entries);
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

    private static NormalizedStorageEntry Normalize(
        PersonalDictionaryStorageEntry entry)
    {
        string spokenForm = NormalizeRequiredValue(
            entry.SpokenForm,
            nameof(entry.SpokenForm));
        string replacement = NormalizeRequiredValue(
            entry.Replacement,
            nameof(entry.Replacement));

        return new NormalizedStorageEntry(
            spokenForm,
            replacement);
    }

    private static string NormalizeRequiredValue(
        string value,
        string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value.Trim();
    }

    private sealed record NormalizedStorageEntry(
        string SpokenForm,
        string Replacement);

    private sealed class StorageRow
    {
        public string? SpokenForm { get; set; }

        public string? Replacement { get; set; }

        public string? UpdatedUtc { get; set; }

        public long SpokenFormByteCount { get; set; }

        public long ReplacementByteCount { get; set; }

        public long UpdatedUtcByteCount { get; set; }
    }

    private sealed class SchemaColumn
    {
        public string Name { get; set; } = string.Empty;

        public string DeclaredType { get; set; } = string.Empty;

        public int IsNotNull { get; set; }

        public int PrimaryKeyOrder { get; set; }
    }

    private sealed class SchemaTable
    {
        public string ObjectType { get; set; } = string.Empty;

        public int ColumnCount { get; set; }

        public int WithoutRowId { get; set; }
    }

    private sealed class SchemaIndex
    {
        public string Name { get; set; } = string.Empty;

        public int IsUnique { get; set; }

        public string Origin { get; set; } = string.Empty;

        public int IsPartial { get; set; }
    }

    private sealed class SchemaIndexColumn
    {
        public int SequenceNumber { get; set; }

        public int ColumnId { get; set; }

        public string ColumnName { get; set; } = string.Empty;

        public string CollationName { get; set; } = string.Empty;

        public int IsKey { get; set; }
    }
}
