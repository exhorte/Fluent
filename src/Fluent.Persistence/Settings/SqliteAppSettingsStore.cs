using Dapper;
using Microsoft.Data.Sqlite;
using Fluent.Core.Settings;

namespace Fluent.Persistence.Settings;

/// <summary>
/// Local SQLite store for reversible application preferences. Uses a dedicated
/// database file, isolated from the dictionary and history databases. No secret
/// and no Cloud consent is ever stored.
/// </summary>
public sealed class SqliteAppSettingsStore : IAppSettingsStore
{
    private const int SupportedSchemaVersion = 1;
    private const int BusyTimeoutMilliseconds = 3000;
    private const int CommandTimeoutSeconds = 3;
    private const int MaximumStoredProfileIdBytes =
        AppSettingsLimits.MaximumProfileIdLength * 4;

    private const string CreateSchemaSql =
        """
        CREATE TABLE app_settings
        (
            id INTEGER PRIMARY KEY,
            preferred_profile_id TEXT
        );
        INSERT INTO app_settings (id, preferred_profile_id) VALUES (1, NULL);
        """;

    private const string LoadSql =
        """
        SELECT
            preferred_profile_id AS PreferredProfileId,
            length(CAST(COALESCE(preferred_profile_id, '') AS BLOB)) AS PreferredProfileIdByteCount
        FROM app_settings
        WHERE id = 1;
        """;

    private const string SetPreferredProfileSql =
        "UPDATE app_settings SET preferred_profile_id = @ProfileId WHERE id = 1;";

    private readonly Lazy<string> _databasePath;

    public SqliteAppSettingsStore()
    {
        _databasePath = new Lazy<string>(
            () => Path.GetFullPath(FluentDataPath.GetDefaultSettingsDatabasePath()),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public SqliteAppSettingsStore(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        string fullDatabasePath = Path.GetFullPath(databasePath);
        _databasePath = new Lazy<string>(
            () => fullDatabasePath,
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public string DatabasePath => _databasePath.Value;

    public Task<AppPreferences> InitializeAndLoadAsync(
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => InitializeAndLoad(cancellationToken),
            cancellationToken);
    }

    public Task SetPreferredProfileAsync(
        string? profileId,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => SetPreferredProfile(profileId, cancellationToken),
            cancellationToken);
    }

    private AppPreferences InitializeAndLoad(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string databasePath = DatabasePath;

        if (!File.Exists(databasePath))
        {
            return CreateMissingDatabase(databasePath, cancellationToken);
        }

        return InspectExistingDatabase(databasePath, cancellationToken);
    }

    private static AppPreferences CreateMissingDatabase(
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

        return Load(connection);
    }

    private static AppPreferences InspectExistingDatabase(
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
                return Load(readOnlyConnection);
            }

            EnsureVersionZeroDatabaseIsEmpty(readOnlyConnection);
        }

        cancellationToken.ThrowIfCancellationRequested();

        using SqliteConnection writableConnection = OpenConnection(
            databasePath,
            SqliteOpenMode.ReadWrite);
        EnsureVersionZeroDatabaseIsEmpty(writableConnection);
        CreateVersionOneSchema(writableConnection, cancellationToken);
        return Load(writableConnection);
    }

    private void SetPreferredProfile(
        string? profileId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string? normalized = NormalizeProfileId(profileId);

        using SqliteConnection connection = OpenConnection(
            DatabasePath,
            SqliteOpenMode.ReadWrite);
        EnsureWritableVersionOneDatabase(connection);
        cancellationToken.ThrowIfCancellationRequested();

        connection.Execute(
            SetPreferredProfileSql,
            new { ProfileId = normalized },
            commandTimeout: CommandTimeoutSeconds);
    }

    private static AppPreferences Load(SqliteConnection connection)
    {
        SettingsRow? row = connection.QuerySingleOrDefault<SettingsRow>(
            LoadSql,
            commandTimeout: CommandTimeoutSeconds);

        if (row is null)
        {
            throw new InvalidDataException(
                "The stored settings row is missing.");
        }

        if (row.PreferredProfileIdByteCount > MaximumStoredProfileIdBytes)
        {
            throw new InvalidDataException(
                "The stored settings contain invalid data.");
        }

        string? preferredProfileId = string.IsNullOrEmpty(row.PreferredProfileId)
            ? null
            : row.PreferredProfileId;

        return new AppPreferences(preferredProfileId);
    }

    private static string? NormalizeProfileId(string? profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId))
        {
            return null;
        }

        string trimmed = profileId.Trim();
        if (trimmed.Length > AppSettingsLimits.MaximumProfileIdLength)
        {
            throw new ArgumentException(
                "The profile identifier exceeds the supported length.",
                nameof(profileId));
        }

        return trimmed;
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

        string[] columns = connection.Query<string>(
                "SELECT name FROM pragma_table_info('app_settings') ORDER BY cid;",
                commandTimeout: CommandTimeoutSeconds)
            .ToArray();

        long settingsRowCount = connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM app_settings WHERE id = 1;",
            commandTimeout: CommandTimeoutSeconds);

        bool hasExpectedSchema =
            tableCount == 1 &&
            columns.SequenceEqual(
                ["id", "preferred_profile_id"],
                StringComparer.Ordinal) &&
            settingsRowCount == 1;

        if (!hasExpectedSchema)
        {
            throw new InvalidDataException(
                "The version-one settings schema is invalid.");
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

    private sealed class SettingsRow
    {
        public string? PreferredProfileId { get; set; }

        public long PreferredProfileIdByteCount { get; set; }
    }
}
