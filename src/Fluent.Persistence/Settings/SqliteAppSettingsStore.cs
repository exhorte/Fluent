using Dapper;
using Microsoft.Data.Sqlite;
using Fluent.Core.Settings;

namespace Fluent.Persistence.Settings;

/// <summary>
/// Local SQLite store for reversible application preferences. Uses a dedicated
/// database file, isolated from the dictionary and history databases. No secret
/// and no Cloud consent is ever stored.
/// Schema v3 adds the transcription language; v2 databases are migrated in place.
/// </summary>
public sealed class SqliteAppSettingsStore : IAppSettingsStore
{
    private const int CurrentSchemaVersion = 3;
    private const int BusyTimeoutMilliseconds = 3000;
    private const int CommandTimeoutSeconds = 3;
    private const int MaximumStoredProfileIdBytes =
        AppSettingsLimits.MaximumProfileIdLength * 4;
    private const int MaximumStoredLanguageBytes = 32;
    private const int MaximumStoredTranscriptionLanguageBytes = 32;

    private const string CreateSchemaSql =
        """
        CREATE TABLE app_settings
        (
            id INTEGER PRIMARY KEY,
            preferred_profile_id TEXT,
            language TEXT NOT NULL,
            transcription_language TEXT NOT NULL
        );
        INSERT INTO app_settings (id, preferred_profile_id, language, transcription_language)
        VALUES (1, NULL, 'en', 'auto');
        """;

    private const string MigrateV1ToV2Sql =
        """
        ALTER TABLE app_settings ADD COLUMN language TEXT;
        UPDATE app_settings SET language = 'en' WHERE language IS NULL;
        """;

    private const string MigrateV2ToV3Sql =
        """
        ALTER TABLE app_settings ADD COLUMN transcription_language TEXT;
        UPDATE app_settings SET transcription_language = 'fr' WHERE transcription_language IS NULL;
        """;

    private const string LoadSql =
        """
        SELECT
            preferred_profile_id AS PreferredProfileId,
            language AS Language,
            transcription_language AS TranscriptionLanguage,
            length(CAST(COALESCE(preferred_profile_id, '') AS BLOB)) AS PreferredProfileIdByteCount,
            length(CAST(COALESCE(language, '') AS BLOB)) AS LanguageByteCount,
            length(CAST(COALESCE(transcription_language, '') AS BLOB)) AS TranscriptionLanguageByteCount
        FROM app_settings
        WHERE id = 1;
        """;

    private const string SetPreferredProfileSql =
        "UPDATE app_settings SET preferred_profile_id = @ProfileId WHERE id = 1;";

    private const string SetLanguageSql =
        "UPDATE app_settings SET language = @Language WHERE id = 1;";

    private const string SetTranscriptionLanguageSql =
        "UPDATE app_settings SET transcription_language = @TranscriptionLanguage WHERE id = 1;";

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

    public Task SetLanguageAsync(
        string language,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => SetLanguage(language, cancellationToken),
            cancellationToken);
    }

    public Task SetTranscriptionLanguageAsync(
        string transcriptionLanguageId,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => SetTranscriptionLanguage(transcriptionLanguageId, cancellationToken),
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
        BringToCurrentSchema(connection, cancellationToken);
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

            if (schemaVersion == CurrentSchemaVersion)
            {
                ValidateVersionThreeSchema(readOnlyConnection);
                return Load(readOnlyConnection);
            }

            if (schemaVersion == 2)
            {
                ValidateVersionTwoSchema(readOnlyConnection);
            }
            else if (schemaVersion == 1)
            {
                ValidateVersionOneSchema(readOnlyConnection);
            }
            else
            {
                EnsureVersionZeroDatabaseIsEmpty(readOnlyConnection);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        using SqliteConnection writableConnection = OpenConnection(
            databasePath,
            SqliteOpenMode.ReadWrite);
        BringToCurrentSchema(writableConnection, cancellationToken);
        return Load(writableConnection);
    }

    private static void BringToCurrentSchema(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        int schemaVersion = ReadSchemaVersion(connection);
        EnsureSupportedSchemaVersion(schemaVersion);

        switch (schemaVersion)
        {
            case 0:
                EnsureVersionZeroDatabaseIsEmpty(connection);
                CreateVersionThreeSchema(connection, cancellationToken);
                break;
            case 1:
                ValidateVersionOneSchema(connection);
                MigrateVersionOneToTwo(connection, cancellationToken);
                goto case 2;
            case 2:
                ValidateVersionTwoSchema(connection);
                MigrateVersionTwoToThree(connection, cancellationToken);
                break;
            default:
                ValidateVersionThreeSchema(connection);
                break;
        }
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
        EnsureWritableCurrentDatabase(connection);
        cancellationToken.ThrowIfCancellationRequested();

        connection.Execute(
            SetPreferredProfileSql,
            new { ProfileId = normalized },
            commandTimeout: CommandTimeoutSeconds);
    }

    private void SetLanguage(string language, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string normalized = AppSettingsLimits.NormalizeLanguage(language);

        using SqliteConnection connection = OpenConnection(
            DatabasePath,
            SqliteOpenMode.ReadWrite);
        EnsureWritableCurrentDatabase(connection);
        cancellationToken.ThrowIfCancellationRequested();

        connection.Execute(
            SetLanguageSql,
            new { Language = normalized },
            commandTimeout: CommandTimeoutSeconds);
    }

    private void SetTranscriptionLanguage(
        string transcriptionLanguageId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string normalized =
            AppSettingsLimits.NormalizeTranscriptionLanguage(transcriptionLanguageId);

        using SqliteConnection connection = OpenConnection(
            DatabasePath,
            SqliteOpenMode.ReadWrite);
        EnsureWritableCurrentDatabase(connection);
        cancellationToken.ThrowIfCancellationRequested();

        connection.Execute(
            SetTranscriptionLanguageSql,
            new { TranscriptionLanguage = normalized },
            commandTimeout: CommandTimeoutSeconds);
    }

    private static AppPreferences Load(SqliteConnection connection)
    {
        SettingsRow? row = connection.QuerySingleOrDefault<SettingsRow>(
            LoadSql,
            commandTimeout: CommandTimeoutSeconds);

        if (row is null)
        {
            throw new InvalidDataException("The stored settings row is missing.");
        }

        if (row.PreferredProfileIdByteCount > MaximumStoredProfileIdBytes ||
            row.LanguageByteCount > MaximumStoredLanguageBytes ||
            row.TranscriptionLanguageByteCount > MaximumStoredTranscriptionLanguageBytes)
        {
            throw new InvalidDataException("The stored settings contain invalid data.");
        }

        string? preferredProfileId = string.IsNullOrEmpty(row.PreferredProfileId)
            ? null
            : row.PreferredProfileId;

        return new AppPreferences(
            preferredProfileId,
            AppSettingsLimits.NormalizeLanguage(row.Language),
            AppSettingsLimits.NormalizeTranscriptionLanguage(row.TranscriptionLanguage));
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
        if (schemaVersion is 0 or 1 or 2 or CurrentSchemaVersion)
        {
            return;
        }

        throw new NotSupportedException(
            $"Database schema version {schemaVersion} is not supported. " +
            $"Supported versions are 0, 1, 2 and {CurrentSchemaVersion}.");
    }

    private static void EnsureWritableCurrentDatabase(SqliteConnection connection)
    {
        int schemaVersion = ReadSchemaVersion(connection);
        if (schemaVersion != CurrentSchemaVersion)
        {
            throw new NotSupportedException(
                $"Database schema version {schemaVersion} is not writable. " +
                $"Expected version {CurrentSchemaVersion}.");
        }

        ValidateVersionThreeSchema(connection);
    }

    private static void EnsureVersionZeroDatabaseIsEmpty(SqliteConnection connection)
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
        ValidateSchema(connection, ["id", "preferred_profile_id"], "version-one");
    }

    private static void ValidateVersionTwoSchema(SqliteConnection connection)
    {
        ValidateSchema(
            connection,
            ["id", "preferred_profile_id", "language"],
            "version-two");
    }

    private static void ValidateVersionThreeSchema(SqliteConnection connection)
    {
        ValidateSchema(
            connection,
            ["id", "preferred_profile_id", "language", "transcription_language"],
            "version-three");
    }

    private static void ValidateSchema(
        SqliteConnection connection,
        string[] expectedColumns,
        string label)
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
            columns.SequenceEqual(expectedColumns, StringComparer.Ordinal) &&
            settingsRowCount == 1;

        if (!hasExpectedSchema)
        {
            throw new InvalidDataException(
                $"The {label} settings schema is invalid.");
        }
    }

    private static void CreateVersionThreeSchema(
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
            $"PRAGMA user_version = {CurrentSchemaVersion};",
            transaction: transaction,
            commandTimeout: CommandTimeoutSeconds);

        cancellationToken.ThrowIfCancellationRequested();
        transaction.Commit();
    }

    private static void MigrateVersionOneToTwo(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        using SqliteTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            MigrateV1ToV2Sql,
            transaction: transaction,
            commandTimeout: CommandTimeoutSeconds);

        cancellationToken.ThrowIfCancellationRequested();

        connection.Execute(
            $"PRAGMA user_version = 2;",
            transaction: transaction,
            commandTimeout: CommandTimeoutSeconds);

        cancellationToken.ThrowIfCancellationRequested();
        transaction.Commit();
    }

    private static void MigrateVersionTwoToThree(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        using SqliteTransaction transaction = connection.BeginTransaction();

        connection.Execute(
            MigrateV2ToV3Sql,
            transaction: transaction,
            commandTimeout: CommandTimeoutSeconds);

        cancellationToken.ThrowIfCancellationRequested();

        connection.Execute(
            $"PRAGMA user_version = {CurrentSchemaVersion};",
            transaction: transaction,
            commandTimeout: CommandTimeoutSeconds);

        cancellationToken.ThrowIfCancellationRequested();
        transaction.Commit();
    }

    private sealed class SettingsRow
    {
        public string? PreferredProfileId { get; set; }

        public string? Language { get; set; }

        public string? TranscriptionLanguage { get; set; }

        public long PreferredProfileIdByteCount { get; set; }

        public long LanguageByteCount { get; set; }

        public long TranscriptionLanguageByteCount { get; set; }
    }
}
