using System.Globalization;
using Microsoft.Data.Sqlite;
using Fluent.Core.Settings;
using Fluent.Persistence;
using Fluent.Persistence.Settings;

namespace Fluent.Persistence.Tests.Settings;

public sealed class AppSettingsDataPathTests
{
    [Fact]
    public void Settings_path_is_a_dedicated_file()
    {
        string root = Path.Combine(
            AppContext.BaseDirectory,
            "settings-path",
            Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));

        string settingsPath = FluentDataPath.GetSettingsDatabasePath(root);

        Assert.Equal(
            Path.Combine(Path.GetFullPath(root), "Fluent", "fluent-settings.db"),
            settingsPath);
        Assert.NotEqual(FluentDataPath.GetDatabasePath(root), settingsPath);
        Assert.NotEqual(FluentDataPath.GetHistoryDatabasePath(root), settingsPath);
        Assert.False(Directory.Exists(root));
    }
}

public sealed class SqliteAppSettingsStoreTests
{
    [Fact]
    public async Task Initialize_returns_empty_preferences_and_is_idempotent()
    {
        using TemporaryDatabase database = new();
        SqliteAppSettingsStore store = new(database.DatabasePath);

        AppPreferences first = await store.InitializeAndLoadAsync(CancellationToken.None);
        AppPreferences second = await store.InitializeAndLoadAsync(CancellationToken.None);

        Assert.Null(first.PreferredProfileId);
        Assert.Equal("en", first.Language);
        Assert.Null(second.PreferredProfileId);
        Assert.Equal("en", second.Language);

        using SqliteConnection connection = OpenInspectionConnection(database.DatabasePath);
        Assert.Equal(3L, ExecuteScalarInt64(connection, "PRAGMA user_version;"));
        Assert.Equal("auto", first.TranscriptionLanguageId);
    }

    [Fact]
    public async Task Language_defaults_to_english_and_persists()
    {
        using TemporaryDatabase database = new();
        SqliteAppSettingsStore store = new(database.DatabasePath);
        AppPreferences initial = await store.InitializeAndLoadAsync(CancellationToken.None);
        Assert.Equal("en", initial.Language);

        await store.SetLanguageAsync("fr", CancellationToken.None);

        SqliteAppSettingsStore reopened = new(database.DatabasePath);
        AppPreferences after = await reopened.InitializeAndLoadAsync(CancellationToken.None);
        Assert.Equal("fr", after.Language);

        await reopened.SetLanguageAsync("XX", CancellationToken.None);
        AppPreferences normalized = await reopened.InitializeAndLoadAsync(CancellationToken.None);
        Assert.Equal("en", normalized.Language);
    }

    [Fact]
    public async Task Version_one_database_is_migrated_preserving_the_profile()
    {
        using TemporaryDatabase database = new();
        Directory.CreateDirectory(database.RootPath);

        // Build a legacy v1 database (no language column).
        SqliteConnectionStringBuilder legacyBuilder = new()
        {
            DataSource = database.DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        };
        using (SqliteConnection connection = new(legacyBuilder.ToString()))
        {
            connection.Open();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                CREATE TABLE app_settings (id INTEGER PRIMARY KEY, preferred_profile_id TEXT);
                INSERT INTO app_settings (id, preferred_profile_id) VALUES (1, 'developer');
                PRAGMA user_version = 1;
                """;
            command.ExecuteNonQuery();
        }

        SqliteAppSettingsStore store = new(database.DatabasePath);
        AppPreferences migrated = await store.InitializeAndLoadAsync(CancellationToken.None);

        Assert.Equal("developer", migrated.PreferredProfileId);
        Assert.Equal("en", migrated.Language);
        Assert.Equal("fr", migrated.TranscriptionLanguageId);

        using SqliteConnection check = OpenInspectionConnection(database.DatabasePath);
        Assert.Equal(3L, ExecuteScalarInt64(check, "PRAGMA user_version;"));
    }

    [Fact]
    public async Task Preferred_profile_persists_across_recreation()
    {
        using TemporaryDatabase database = new();
        SqliteAppSettingsStore store = new(database.DatabasePath);
        await store.InitializeAndLoadAsync(CancellationToken.None);

        await store.SetPreferredProfileAsync("developer", CancellationToken.None);

        SqliteAppSettingsStore reopened = new(database.DatabasePath);
        AppPreferences preferences =
            await reopened.InitializeAndLoadAsync(CancellationToken.None);

        Assert.Equal("developer", preferences.PreferredProfileId);
    }

    [Fact]
    public async Task Preferred_profile_can_be_cleared()
    {
        using TemporaryDatabase database = new();
        SqliteAppSettingsStore store = new(database.DatabasePath);
        await store.InitializeAndLoadAsync(CancellationToken.None);

        await store.SetPreferredProfileAsync("professional-fr", CancellationToken.None);
        await store.SetPreferredProfileAsync(null, CancellationToken.None);

        AppPreferences preferences =
            await store.InitializeAndLoadAsync(CancellationToken.None);
        Assert.Null(preferences.PreferredProfileId);
    }

    [Fact]
    public async Task Whitespace_profile_is_stored_as_null()
    {
        using TemporaryDatabase database = new();
        SqliteAppSettingsStore store = new(database.DatabasePath);
        await store.InitializeAndLoadAsync(CancellationToken.None);

        await store.SetPreferredProfileAsync("   ", CancellationToken.None);

        AppPreferences preferences =
            await store.InitializeAndLoadAsync(CancellationToken.None);
        Assert.Null(preferences.PreferredProfileId);
    }

    [Fact]
    public async Task Hostile_profile_value_is_stored_only_as_a_parameter()
    {
        using TemporaryDatabase database = new();
        SqliteAppSettingsStore store = new(database.DatabasePath);
        await store.InitializeAndLoadAsync(CancellationToken.None);

        const string hostile = "x'); DROP TABLE app_settings; --";
        await store.SetPreferredProfileAsync(hostile, CancellationToken.None);

        AppPreferences preferences =
            await store.InitializeAndLoadAsync(CancellationToken.None);
        Assert.Equal(hostile, preferences.PreferredProfileId);

        using SqliteConnection connection = OpenInspectionConnection(database.DatabasePath);
        Assert.Equal(
            1L,
            ExecuteScalarInt64(
                connection,
                """
                SELECT COUNT(*) FROM sqlite_master
                WHERE type = 'table' AND name = 'app_settings';
                """));
    }

    [Fact]
    public async Task Oversized_stored_value_is_rejected()
    {
        using TemporaryDatabase database = new();
        SqliteAppSettingsStore store = new(database.DatabasePath);
        await store.InitializeAndLoadAsync(CancellationToken.None);

        using (SqliteConnection connection = OpenInspectionConnection(database.DatabasePath))
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                "UPDATE app_settings SET preferred_profile_id = $value WHERE id = 1;";
            command.Parameters.AddWithValue("$value", new string('x', 400));
            command.ExecuteNonQuery();
        }

        await Assert.ThrowsAsync<InvalidDataException>(
            () => store.InitializeAndLoadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Newer_schema_version_is_rejected_without_mutation()
    {
        using TemporaryDatabase database = new();
        SqliteAppSettingsStore store = new(database.DatabasePath);
        await store.InitializeAndLoadAsync(CancellationToken.None);

        using (SqliteConnection connection = OpenInspectionConnection(database.DatabasePath))
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version = 99;";
            command.ExecuteNonQuery();
        }

        byte[] before = File.ReadAllBytes(database.DatabasePath);
        SqliteAppSettingsStore newerStore = new(database.DatabasePath);

        NotSupportedException exception =
            await Assert.ThrowsAsync<NotSupportedException>(
                () => newerStore.InitializeAndLoadAsync(CancellationToken.None));

        Assert.Contains("99", exception.Message, StringComparison.Ordinal);
        Assert.Equal(before, File.ReadAllBytes(database.DatabasePath));
        AssertNoSQLiteSidecars(database.DatabasePath);
    }

    [Fact]
    public async Task Transcription_language_defaults_to_auto_and_persists()
    {
        using TemporaryDatabase database = new();
        SqliteAppSettingsStore store = new(database.DatabasePath);
        AppPreferences initial = await store.InitializeAndLoadAsync(CancellationToken.None);
        Assert.Equal("auto", initial.TranscriptionLanguageId);

        await store.SetTranscriptionLanguageAsync("en", CancellationToken.None);

        SqliteAppSettingsStore reopened = new(database.DatabasePath);
        AppPreferences after = await reopened.InitializeAndLoadAsync(CancellationToken.None);
        Assert.Equal("en", after.TranscriptionLanguageId);

        await reopened.SetTranscriptionLanguageAsync("XX", CancellationToken.None);
        AppPreferences normalized = await reopened.InitializeAndLoadAsync(CancellationToken.None);
        Assert.Equal("auto", normalized.TranscriptionLanguageId);
    }

    [Fact]
    public async Task Version_two_database_is_migrated_to_three()
    {
        using TemporaryDatabase database = new();
        Directory.CreateDirectory(database.RootPath);

        // Build a v2 database (with language, without transcription_language).
        SqliteConnectionStringBuilder builder = new()
        {
            DataSource = database.DatabasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Pooling = false
        };
        using (SqliteConnection connection = new(builder.ToString()))
        {
            connection.Open();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                CREATE TABLE app_settings
                (
                    id INTEGER PRIMARY KEY,
                    preferred_profile_id TEXT,
                    language TEXT NOT NULL
                );
                INSERT INTO app_settings (id, preferred_profile_id, language)
                VALUES (1, 'developer', 'fr');
                PRAGMA user_version = 2;
                """;
            command.ExecuteNonQuery();
        }

        SqliteAppSettingsStore store = new(database.DatabasePath);
        AppPreferences migrated = await store.InitializeAndLoadAsync(CancellationToken.None);

        Assert.Equal("developer", migrated.PreferredProfileId);
        Assert.Equal("fr", migrated.Language);
        Assert.Equal("fr", migrated.TranscriptionLanguageId);

        using SqliteConnection check = OpenInspectionConnection(database.DatabasePath);
        Assert.Equal(3L, ExecuteScalarInt64(check, "PRAGMA user_version;"));
    }

    [Fact]
    public async Task Precancelled_operations_perform_no_io()
    {
        using TemporaryDatabase database = new();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        SqliteAppSettingsStore uninitialized = new(database.DatabasePath);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => uninitialized.InitializeAndLoadAsync(cancellation.Token));
        Assert.False(Directory.Exists(database.RootPath));

        using TemporaryDatabase ready = new();
        SqliteAppSettingsStore store = new(ready.DatabasePath);
        await store.InitializeAndLoadAsync(CancellationToken.None);
        byte[] before = File.ReadAllBytes(ready.DatabasePath);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => store.SetPreferredProfileAsync("developer", cancellation.Token));

        Assert.Equal(before, File.ReadAllBytes(ready.DatabasePath));
    }

    private static SqliteConnection OpenInspectionConnection(string databasePath)
    {
        SqliteConnectionStringBuilder builder = new()
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Pooling = false
        };

        SqliteConnection connection = new(builder.ToString());
        connection.Open();
        return connection;
    }

    private static void AssertNoSQLiteSidecars(string databasePath)
    {
        Assert.False(File.Exists(databasePath + "-journal"));
        Assert.False(File.Exists(databasePath + "-wal"));
        Assert.False(File.Exists(databasePath + "-shm"));
    }

    private static long ExecuteScalarInt64(SqliteConnection connection, string commandText)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    private sealed class TemporaryDatabase : IDisposable
    {
        public TemporaryDatabase()
        {
            RootPath = Path.Combine(
                AppContext.BaseDirectory,
                "temporary-settings-databases",
                Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
            DatabasePath = Path.Combine(RootPath, "fluent-settings-test.db");
        }

        public string RootPath { get; }

        public string DatabasePath { get; }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
    }
}
