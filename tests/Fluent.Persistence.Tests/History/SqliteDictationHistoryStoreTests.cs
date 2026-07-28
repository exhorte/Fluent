using System.Globalization;
using Microsoft.Data.Sqlite;
using Fluent.Core.History;
using Fluent.Persistence;
using Fluent.Persistence.History;

namespace Fluent.Persistence.Tests.History;

public sealed class DictationHistoryDataPathTests
{
    [Fact]
    public void History_path_is_a_dedicated_file_under_local_application_data()
    {
        string root = Path.Combine(
            AppContext.BaseDirectory,
            "history-path",
            Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));

        string historyPath = FluentDataPath.GetHistoryDatabasePath(root);
        string dictionaryPath = FluentDataPath.GetDatabasePath(root);

        Assert.Equal(
            Path.Combine(Path.GetFullPath(root), "Fluent", "fluent-history.db"),
            historyPath);
        Assert.NotEqual(dictionaryPath, historyPath);
        Assert.False(Directory.Exists(root));
    }
}

public sealed class SqliteDictationHistoryStoreTests
{
    [Fact]
    public async Task Initialize_creates_schema_disabled_by_default_and_is_idempotent()
    {
        using TemporaryDatabase database = new();
        SqliteDictationHistoryStore store = new(database.DatabasePath);

        DictationHistorySnapshot first =
            await store.InitializeAndLoadAsync(CancellationToken.None);
        DictationHistorySnapshot second =
            await store.InitializeAndLoadAsync(CancellationToken.None);

        Assert.False(first.Preferences.IsEnabled);
        Assert.Empty(first.Entries);
        Assert.False(second.Preferences.IsEnabled);
        Assert.Empty(second.Entries);

        using SqliteConnection connection = OpenInspectionConnection(database.DatabasePath);
        Assert.Equal(1L, ExecuteScalarInt64(connection, "PRAGMA user_version;"));
        Assert.Equal(
            2L,
            ExecuteScalarInt64(
                connection,
                """
                SELECT COUNT(*) FROM sqlite_master
                WHERE type = 'table' AND name NOT LIKE 'sqlite_%';
                """));
    }

    [Fact]
    public async Task Opt_in_preference_persists_across_recreation()
    {
        using TemporaryDatabase database = new();
        SqliteDictationHistoryStore store = new(database.DatabasePath);
        await store.InitializeAndLoadAsync(CancellationToken.None);

        await store.SetEnabledAsync(true, CancellationToken.None);

        SqliteDictationHistoryStore reopened = new(database.DatabasePath);
        DictationHistorySnapshot snapshot =
            await reopened.InitializeAndLoadAsync(CancellationToken.None);

        Assert.True(snapshot.Preferences.IsEnabled);

        await reopened.SetEnabledAsync(false, CancellationToken.None);
        DictationHistorySnapshot afterDisable =
            await reopened.InitializeAndLoadAsync(CancellationToken.None);
        Assert.False(afterDisable.Preferences.IsEnabled);
    }

    [Fact]
    public async Task Append_stores_entries_newest_first_and_survives_recreation()
    {
        using TemporaryDatabase database = new();
        SqliteDictationHistoryStore store = new(database.DatabasePath);
        await store.InitializeAndLoadAsync(CancellationToken.None);

        DictationHistoryEntry older = new(
            Guid.NewGuid(),
            new DateTimeOffset(2026, 7, 23, 8, 0, 0, TimeSpan.Zero),
            "première dictée",
            "professional");
        DictationHistoryEntry newer = new(
            Guid.NewGuid(),
            new DateTimeOffset(2026, 7, 23, 9, 0, 0, TimeSpan.Zero),
            "seconde dictée",
            null);

        await store.AppendAsync(older, CancellationToken.None);
        await store.AppendAsync(newer, CancellationToken.None);

        SqliteDictationHistoryStore reopened = new(database.DatabasePath);
        DictationHistorySnapshot snapshot =
            await reopened.InitializeAndLoadAsync(CancellationToken.None);

        Assert.Equal(2, snapshot.Entries.Count);
        Assert.Equal(newer.Id, snapshot.Entries[0].Id);
        Assert.Equal("seconde dictée", snapshot.Entries[0].Text);
        Assert.Null(snapshot.Entries[0].ProfileId);
        Assert.Equal(older.Id, snapshot.Entries[1].Id);
        Assert.Equal("professional", snapshot.Entries[1].ProfileId);
    }

    [Fact]
    public async Task Append_prunes_to_the_retention_cap_keeping_newest()
    {
        using TemporaryDatabase database = new();
        SqliteDictationHistoryStore store = new(database.DatabasePath);
        await store.InitializeAndLoadAsync(CancellationToken.None);

        Guid oldestId = Guid.NewGuid();
        SeedRawEntries(
            database.DatabasePath,
            DictationHistoryLimits.MaximumEntryCount,
            oldestId);

        Guid newestId = Guid.NewGuid();
        await store.AppendAsync(
            new DictationHistoryEntry(
                newestId,
                DateTimeOffset.UtcNow,
                "entrée la plus récente",
                null),
            CancellationToken.None);

        DictationHistorySnapshot snapshot =
            await store.InitializeAndLoadAsync(CancellationToken.None);

        Assert.Equal(DictationHistoryLimits.MaximumEntryCount, snapshot.Entries.Count);
        Assert.Equal(newestId, snapshot.Entries[0].Id);
        Assert.DoesNotContain(snapshot.Entries, entry => entry.Id == oldestId);
    }

    [Fact]
    public async Task Delete_removes_a_single_entry_and_reports_missing_ids()
    {
        using TemporaryDatabase database = new();
        SqliteDictationHistoryStore store = new(database.DatabasePath);
        await store.InitializeAndLoadAsync(CancellationToken.None);

        Guid keep = Guid.NewGuid();
        Guid remove = Guid.NewGuid();
        await store.AppendAsync(
            new DictationHistoryEntry(keep, DateTimeOffset.UtcNow, "à garder", null),
            CancellationToken.None);
        await store.AppendAsync(
            new DictationHistoryEntry(remove, DateTimeOffset.UtcNow, "à supprimer", null),
            CancellationToken.None);

        Assert.True(await store.DeleteAsync(remove, CancellationToken.None));
        Assert.False(await store.DeleteAsync(Guid.NewGuid(), CancellationToken.None));

        DictationHistorySnapshot snapshot =
            await store.InitializeAndLoadAsync(CancellationToken.None);
        DictationHistoryEntry remaining = Assert.Single(snapshot.Entries);
        Assert.Equal(keep, remaining.Id);
    }

    [Fact]
    public async Task Clear_removes_everything_and_returns_count()
    {
        using TemporaryDatabase database = new();
        SqliteDictationHistoryStore store = new(database.DatabasePath);
        await store.InitializeAndLoadAsync(CancellationToken.None);

        await store.AppendAsync(
            new DictationHistoryEntry(Guid.NewGuid(), DateTimeOffset.UtcNow, "un", null),
            CancellationToken.None);
        await store.AppendAsync(
            new DictationHistoryEntry(Guid.NewGuid(), DateTimeOffset.UtcNow, "deux", null),
            CancellationToken.None);

        int removed = await store.ClearAsync(CancellationToken.None);
        Assert.Equal(2, removed);

        DictationHistorySnapshot snapshot =
            await store.InitializeAndLoadAsync(CancellationToken.None);
        Assert.Empty(snapshot.Entries);
    }

    [Fact]
    public async Task Hostile_text_is_stored_only_as_a_parameter_value()
    {
        using TemporaryDatabase database = new();
        SqliteDictationHistoryStore store = new(database.DatabasePath);
        await store.InitializeAndLoadAsync(CancellationToken.None);

        Guid id = Guid.NewGuid();
        const string hostile =
            "'); DROP TABLE dictation_history; PRAGMA user_version = 99; --";
        await store.AppendAsync(
            new DictationHistoryEntry(id, DateTimeOffset.UtcNow, hostile, hostile),
            CancellationToken.None);

        DictationHistorySnapshot snapshot =
            await store.InitializeAndLoadAsync(CancellationToken.None);
        DictationHistoryEntry entry = Assert.Single(snapshot.Entries);
        Assert.Equal(hostile, entry.Text);

        using SqliteConnection connection = OpenInspectionConnection(database.DatabasePath);
        Assert.Equal(1L, ExecuteScalarInt64(connection, "PRAGMA user_version;"));
        Assert.Equal(
            1L,
            ExecuteScalarInt64(
                connection,
                """
                SELECT COUNT(*) FROM sqlite_master
                WHERE type = 'table' AND name = 'dictation_history';
                """));
    }

    [Fact]
    public async Task Oversized_stored_text_is_rejected()
    {
        using TemporaryDatabase database = new();
        SqliteDictationHistoryStore store = new(database.DatabasePath);
        await store.InitializeAndLoadAsync(CancellationToken.None);

        using (SqliteConnection connection = OpenInspectionConnection(database.DatabasePath))
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO dictation_history (id, created_utc, text, profile_id)
                VALUES ($id, $created, $text, NULL);
                """;
            command.Parameters.AddWithValue(
                "$id",
                Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue(
                "$created",
                DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$text", new string('x', 60000));
            command.ExecuteNonQuery();
        }

        await Assert.ThrowsAsync<InvalidDataException>(
            () => store.InitializeAndLoadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Invalid_preference_value_is_rejected()
    {
        using TemporaryDatabase database = new();
        SqliteDictationHistoryStore store = new(database.DatabasePath);
        await store.InitializeAndLoadAsync(CancellationToken.None);

        using (SqliteConnection connection = OpenInspectionConnection(database.DatabasePath))
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "UPDATE history_preference SET enabled = 5 WHERE id = 1;";
            command.ExecuteNonQuery();
        }

        await Assert.ThrowsAsync<InvalidDataException>(
            () => store.InitializeAndLoadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Newer_schema_version_is_rejected_without_mutation()
    {
        using TemporaryDatabase database = new();
        SqliteDictationHistoryStore store = new(database.DatabasePath);
        await store.InitializeAndLoadAsync(CancellationToken.None);

        using (SqliteConnection connection = OpenInspectionConnection(database.DatabasePath))
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version = 2;";
            command.ExecuteNonQuery();
        }

        byte[] before = File.ReadAllBytes(database.DatabasePath);
        SqliteDictationHistoryStore newerStore = new(database.DatabasePath);

        NotSupportedException exception =
            await Assert.ThrowsAsync<NotSupportedException>(
                () => newerStore.InitializeAndLoadAsync(CancellationToken.None));

        Assert.Contains("version 2", exception.Message, StringComparison.Ordinal);
        Assert.Equal(before, File.ReadAllBytes(database.DatabasePath));
        AssertNoSQLiteSidecars(database.DatabasePath);
    }

    [Fact]
    public async Task Precancelled_operations_perform_no_io()
    {
        using TemporaryDatabase database = new();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        SqliteDictationHistoryStore uninitialized = new(database.DatabasePath);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => uninitialized.InitializeAndLoadAsync(cancellation.Token));
        Assert.False(Directory.Exists(database.RootPath));

        using TemporaryDatabase ready = new();
        SqliteDictationHistoryStore store = new(ready.DatabasePath);
        await store.InitializeAndLoadAsync(CancellationToken.None);
        byte[] before = File.ReadAllBytes(ready.DatabasePath);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => store.AppendAsync(
                new DictationHistoryEntry(Guid.NewGuid(), DateTimeOffset.UtcNow, "x", null),
                cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => store.ClearAsync(cancellation.Token));

        Assert.Equal(before, File.ReadAllBytes(ready.DatabasePath));
    }

    private static void SeedRawEntries(string databasePath, int count, Guid firstId)
    {
        using SqliteConnection connection = OpenInspectionConnection(databasePath);
        using SqliteTransaction transaction = connection.BeginTransaction();
        using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            INSERT INTO dictation_history (id, created_utc, text, profile_id)
            VALUES ($id, $created, $text, NULL);
            """;
        SqliteParameter id = command.Parameters.Add("$id", SqliteType.Text);
        SqliteParameter created = command.Parameters.Add("$created", SqliteType.Text);
        SqliteParameter text = command.Parameters.Add("$text", SqliteType.Text);

        for (int index = 0; index < count; index++)
        {
            Guid entryId = index == 0 ? firstId : Guid.NewGuid();
            id.Value = entryId.ToString("D", CultureInfo.InvariantCulture);
            created.Value = DateTimeOffset.UtcNow
                .AddSeconds(index)
                .ToString("O", CultureInfo.InvariantCulture);
            text.Value = $"entrée {index:D4}";
            command.ExecuteNonQuery();
        }

        transaction.Commit();
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
                "temporary-history-databases",
                Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
            DatabasePath = Path.Combine(RootPath, "fluent-history-test.db");
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
