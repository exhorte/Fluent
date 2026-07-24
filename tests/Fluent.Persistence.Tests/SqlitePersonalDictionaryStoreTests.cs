using System.Globalization;
using System.Text;
using Microsoft.Data.Sqlite;
using Fluent.Core.Dictionary;
using Fluent.Persistence.Dictionary;

namespace Fluent.Persistence.Tests;

public sealed class FluentDataPathTests
{
    [Fact]
    public void Path_resolution_and_store_construction_do_not_create_directories()
    {
        string localApplicationDataRoot = Path.Combine(
            AppContext.BaseDirectory,
            "path-resolution",
            Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));

        string databasePath = FluentDataPath.GetDatabasePath(
            localApplicationDataRoot);
        SqlitePersonalDictionaryStore store = new(databasePath);

        Assert.Equal(
            Path.Combine(
                Path.GetFullPath(localApplicationDataRoot),
                "Fluent",
                "fluent.db"),
            databasePath);
        Assert.Equal(databasePath, store.DatabasePath);
        Assert.False(Directory.Exists(localApplicationDataRoot));
    }

    [Fact]
    public void Default_path_resolves_under_local_application_data()
    {
        string expectedRoot = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.DoNotVerify);

        string databasePath = FluentDataPath.GetDefaultDatabasePath();

        Assert.Equal(
            Path.Combine(expectedRoot, "Fluent", "fluent.db"),
            databasePath);
    }
}

public sealed class SqlitePersonalDictionaryStoreTests
{
    [Fact]
    public async Task Migration_creates_version_one_schema_and_is_idempotent()
    {
        using TemporaryDatabase database = new();
        SqlitePersonalDictionaryStore store = new(database.DatabasePath);

        IReadOnlyList<PersonalDictionaryStorageEntry> firstLoad =
            await store.InitializeAndLoadAsync(CancellationToken.None);
        IReadOnlyList<PersonalDictionaryStorageEntry> secondLoad =
            await store.InitializeAndLoadAsync(CancellationToken.None);

        Assert.Empty(firstLoad);
        Assert.Empty(secondLoad);

        using SqliteConnection connection = OpenInspectionConnection(
            database.DatabasePath);

        Assert.Equal(1L, ExecuteScalarInt64(connection, "PRAGMA user_version;"));
        Assert.Equal(
            1L,
            ExecuteScalarInt64(
                connection,
                """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type = 'table'
                  AND name = 'personal_dictionary';
                """));

        using SqliteCommand schemaCommand = connection.CreateCommand();
        schemaCommand.CommandText =
            """
            SELECT sql
            FROM sqlite_master
            WHERE type = 'table'
              AND name = 'personal_dictionary';
            """;

        string schema = Assert.IsType<string>(schemaCommand.ExecuteScalar());
        Assert.Contains(
            "NYX_ORDINAL_IGNORE_CASE",
            schema,
            StringComparison.Ordinal);
        Assert.Contains("PRIMARY KEY", schema, StringComparison.Ordinal);
        Assert.Contains("updated_utc", schema, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Persistent_identity_exactly_matches_ordinal_ignore_case()
    {
        using TemporaryDatabase database = new();
        SqlitePersonalDictionaryStore store = new(database.DatabasePath);
        await store.InitializeAndLoadAsync(CancellationToken.None);

        Assert.False(StringComparer.OrdinalIgnoreCase.Equals("s", "ſ"));
        Assert.Equal("S", "s".ToUpperInvariant());
        Assert.Equal("S", "ſ".ToUpperInvariant());

        await store.UpsertAsync(
            new PersonalDictionaryStorageEntry("s", "lettre s"),
            CancellationToken.None);
        await store.UpsertAsync(
            new PersonalDictionaryStorageEntry("ſ", "s long"),
            CancellationToken.None);

        SqlitePersonalDictionaryStore recreatedStore = new(database.DatabasePath);
        IReadOnlyList<PersonalDictionaryStorageEntry> entries =
            await recreatedStore.InitializeAndLoadAsync(CancellationToken.None);

        Assert.Equal(2, entries.Count);
        Assert.Contains(
            new PersonalDictionaryStorageEntry("s", "lettre s"),
            entries);
        Assert.Contains(
            new PersonalDictionaryStorageEntry("ſ", "s long"),
            entries);
    }

    [Fact]
    public async Task Crud_survives_store_recreation_and_load_order_is_deterministic()
    {
        using TemporaryDatabase database = new();
        SqlitePersonalDictionaryStore firstStore = new(database.DatabasePath);
        await firstStore.InitializeAndLoadAsync(CancellationToken.None);

        await firstStore.UpsertAsync(
            new PersonalDictionaryStorageEntry(" beta ", " deuxième "),
            CancellationToken.None);
        await firstStore.UpsertAsync(
            new PersonalDictionaryStorageEntry("école", "classe"),
            CancellationToken.None);
        await firstStore.UpsertAsync(
            new PersonalDictionaryStorageEntry("ÉCOLE", "établissement"),
            CancellationToken.None);
        await firstStore.UpsertAsync(
            new PersonalDictionaryStorageEntry("alpha", "premier"),
            CancellationToken.None);

        SqlitePersonalDictionaryStore recreatedStore = new(database.DatabasePath);
        IReadOnlyList<PersonalDictionaryStorageEntry> recreatedEntries =
            await recreatedStore.InitializeAndLoadAsync(CancellationToken.None);

        Assert.Equal(
            [
                new PersonalDictionaryStorageEntry("alpha", "premier"),
                new PersonalDictionaryStorageEntry("beta", "deuxième"),
                new PersonalDictionaryStorageEntry("ÉCOLE", "établissement")
            ],
            recreatedEntries);

        Assert.True(
            await recreatedStore.DeleteAsync("  école  ", CancellationToken.None));
        Assert.False(
            await recreatedStore.DeleteAsync("ÉCOLE", CancellationToken.None));

        SqlitePersonalDictionaryStore afterDeleteStore = new(database.DatabasePath);
        IReadOnlyList<PersonalDictionaryStorageEntry> afterDeleteEntries =
            await afterDeleteStore.InitializeAndLoadAsync(CancellationToken.None);

        Assert.Equal(
            [
                new PersonalDictionaryStorageEntry("alpha", "premier"),
                new PersonalDictionaryStorageEntry("beta", "deuxième")
            ],
            afterDeleteEntries);
    }

    [Fact]
    public async Task Over_capacity_real_database_is_bounded_and_rejected()
    {
        using TemporaryDatabase database = new();
        SqlitePersonalDictionaryStore store = new(database.DatabasePath);
        await store.InitializeAndLoadAsync(CancellationToken.None);

        using (SqliteConnection connection = OpenInspectionConnection(
                   database.DatabasePath))
        using (SqliteTransaction transaction = connection.BeginTransaction())
        using (SqliteCommand command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText =
                """
                INSERT INTO personal_dictionary
                    (spoken_form, replacement, updated_utc)
                VALUES
                    ($spokenForm, $replacement, $updatedUtc);
                """;
            SqliteParameter spokenForm = command.Parameters.Add(
                "$spokenForm",
                SqliteType.Text);
            SqliteParameter replacement = command.Parameters.Add(
                "$replacement",
                SqliteType.Text);
            command.Parameters.AddWithValue(
                "$updatedUtc",
                DateTimeOffset.UtcNow.ToString(
                    "O",
                    CultureInfo.InvariantCulture));

            for (int index = 0;
                 index <= PersonalDictionaryLimits.MaximumEntryCount;
                 index++)
            {
                spokenForm.Value = $"mot{index:D3}";
                replacement.Value = $"terme{index:D3}";
                command.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        await Assert.ThrowsAsync<InvalidDataException>(
            () => store.InitializeAndLoadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Apostrophes_and_sql_text_are_stored_only_as_parameter_values()
    {
        using TemporaryDatabase database = new();
        SqlitePersonalDictionaryStore store = new(database.DatabasePath);
        await store.InitializeAndLoadAsync(CancellationToken.None);

        PersonalDictionaryStorageEntry hostileEntry = new(
            "l'application'); DROP TABLE personal_dictionary; --",
            "Fluent d'aujourd'hui'); PRAGMA user_version = 99; --");

        await store.UpsertAsync(hostileEntry, CancellationToken.None);

        SqlitePersonalDictionaryStore recreatedStore = new(database.DatabasePath);
        PersonalDictionaryStorageEntry loadedEntry = Assert.Single(
            await recreatedStore.InitializeAndLoadAsync(CancellationToken.None));

        Assert.Equal(hostileEntry, loadedEntry);

        using SqliteConnection connection = OpenInspectionConnection(
            database.DatabasePath);
        Assert.Equal(
            1L,
            ExecuteScalarInt64(
                connection,
                """
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type = 'table'
                  AND name = 'personal_dictionary';
                """));
        Assert.Equal(1L, ExecuteScalarInt64(connection, "PRAGMA user_version;"));
    }

    [Fact]
    public async Task Newer_schema_version_is_rejected_without_mutating_database()
    {
        using TemporaryDatabase database = new();
        SqlitePersonalDictionaryStore initialStore = new(database.DatabasePath);
        await initialStore.InitializeAndLoadAsync(CancellationToken.None);

        using (SqliteConnection connection = OpenInspectionConnection(
                   database.DatabasePath))
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                CREATE TABLE sentinel(value TEXT NOT NULL);
                INSERT INTO sentinel(value) VALUES ('preserve me');
                PRAGMA user_version = 2;
                """;
            command.ExecuteNonQuery();
        }

        byte[] bytesBefore = File.ReadAllBytes(database.DatabasePath);
        SqlitePersonalDictionaryStore newerVersionStore = new(
            database.DatabasePath);

        NotSupportedException exception =
            await Assert.ThrowsAsync<NotSupportedException>(
                () => newerVersionStore.InitializeAndLoadAsync(
                    CancellationToken.None));

        Assert.Contains("version 2", exception.Message, StringComparison.Ordinal);
        Assert.Equal(bytesBefore, File.ReadAllBytes(database.DatabasePath));
        AssertNoSQLiteSidecars(database.DatabasePath);
    }

    [Fact]
    public async Task Negative_schema_version_is_rejected_without_mutation()
    {
        using TemporaryDatabase database = new();
        SqlitePersonalDictionaryStore initialStore = new(database.DatabasePath);
        await initialStore.InitializeAndLoadAsync(CancellationToken.None);

        using (SqliteConnection connection = OpenInspectionConnection(
                   database.DatabasePath))
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "PRAGMA user_version = -1;";
            command.ExecuteNonQuery();
            Assert.Equal(
                -1L,
                ExecuteScalarInt64(connection, "PRAGMA user_version;"));
        }

        byte[] bytesBefore = File.ReadAllBytes(database.DatabasePath);
        SqlitePersonalDictionaryStore store = new(database.DatabasePath);

        NotSupportedException exception =
            await Assert.ThrowsAsync<NotSupportedException>(
                () => store.InitializeAndLoadAsync(CancellationToken.None));

        Assert.Contains("version -1", exception.Message, StringComparison.Ordinal);
        Assert.Equal(bytesBefore, File.ReadAllBytes(database.DatabasePath));
        AssertNoSQLiteSidecars(database.DatabasePath);
    }

    [Fact]
    public async Task Structurally_invalid_version_one_schema_is_rejected()
    {
        using TemporaryDatabase database = new();
        SqlitePersonalDictionaryStore initialStore = new(database.DatabasePath);
        await initialStore.InitializeAndLoadAsync(CancellationToken.None);

        using (SqliteConnection connection = OpenInspectionConnection(
                   database.DatabasePath))
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                DROP TABLE personal_dictionary;
                CREATE TABLE personal_dictionary
                (
                    spoken_form TEXT PRIMARY KEY,
                    replacement TEXT NOT NULL,
                    updated_utc TEXT NOT NULL
                );
                PRAGMA user_version = 1;
                """;
            command.ExecuteNonQuery();
        }

        byte[] bytesBefore = File.ReadAllBytes(database.DatabasePath);
        SqlitePersonalDictionaryStore store = new(database.DatabasePath);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => store.InitializeAndLoadAsync(CancellationToken.None));

        Assert.Equal(bytesBefore, File.ReadAllBytes(database.DatabasePath));
        AssertNoSQLiteSidecars(database.DatabasePath);
    }

    [Fact]
    public async Task Version_one_schema_with_binary_primary_key_is_rejected()
    {
        using TemporaryDatabase database = new();
        SqlitePersonalDictionaryStore initialStore = new(database.DatabasePath);
        await initialStore.InitializeAndLoadAsync(CancellationToken.None);

        using (SqliteConnection connection = OpenInspectionConnection(
                   database.DatabasePath))
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                DROP TABLE personal_dictionary;
                CREATE TABLE personal_dictionary
                (
                    spoken_form TEXT NOT NULL PRIMARY KEY,
                    replacement TEXT NOT NULL,
                    updated_utc TEXT NOT NULL
                ) WITHOUT ROWID;
                PRAGMA user_version = 1;
                """;
            command.ExecuteNonQuery();
        }

        byte[] bytesBefore = File.ReadAllBytes(database.DatabasePath);
        SqlitePersonalDictionaryStore store = new(database.DatabasePath);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => store.InitializeAndLoadAsync(CancellationToken.None));

        Assert.Equal(bytesBefore, File.ReadAllBytes(database.DatabasePath));
        AssertNoSQLiteSidecars(database.DatabasePath);
    }

    [Fact]
    public async Task Version_one_schema_with_rowid_table_is_rejected()
    {
        using TemporaryDatabase database = new();
        SqlitePersonalDictionaryStore initialStore = new(database.DatabasePath);
        await initialStore.InitializeAndLoadAsync(CancellationToken.None);

        using (SqliteConnection connection = OpenInspectionConnection(
                   database.DatabasePath))
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                DROP TABLE personal_dictionary;
                CREATE TABLE personal_dictionary
                (
                    spoken_form TEXT NOT NULL
                        COLLATE NYX_ORDINAL_IGNORE_CASE
                        PRIMARY KEY,
                    replacement TEXT NOT NULL,
                    updated_utc TEXT NOT NULL
                );
                PRAGMA user_version = 1;
                """;
            command.ExecuteNonQuery();
        }

        byte[] bytesBefore = File.ReadAllBytes(database.DatabasePath);
        SqlitePersonalDictionaryStore store = new(database.DatabasePath);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => store.InitializeAndLoadAsync(CancellationToken.None));

        Assert.Equal(bytesBefore, File.ReadAllBytes(database.DatabasePath));
        AssertNoSQLiteSidecars(database.DatabasePath);
    }

    [Fact]
    public async Task Version_one_schema_with_decoy_markers_is_rejected()
    {
        using TemporaryDatabase database = new();
        SqlitePersonalDictionaryStore initialStore = new(database.DatabasePath);
        await initialStore.InitializeAndLoadAsync(CancellationToken.None);

        using (SqliteConnection connection = OpenInspectionConnection(
                   database.DatabasePath))
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                DROP TABLE personal_dictionary;
                CREATE TABLE personal_dictionary
                (
                    spoken_form TEXT NOT NULL PRIMARY KEY,
                    replacement TEXT NOT NULL,
                    updated_utc TEXT NOT NULL,
                    CHECK
                    (
                        'NYX_ORDINAL_IGNORE_CASE' <> ''
                        AND 'WITHOUT ROWID' <> ''
                    )
                );
                PRAGMA user_version = 1;
                """;
            command.ExecuteNonQuery();
        }

        byte[] bytesBefore = File.ReadAllBytes(database.DatabasePath);
        SqlitePersonalDictionaryStore store = new(database.DatabasePath);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => store.InitializeAndLoadAsync(CancellationToken.None));

        Assert.Equal(bytesBefore, File.ReadAllBytes(database.DatabasePath));
        AssertNoSQLiteSidecars(database.DatabasePath);
    }

    [Fact]
    public async Task Oversized_persisted_value_is_rejected_with_bounded_loading()
    {
        using TemporaryDatabase database = new();
        SqlitePersonalDictionaryStore store = new(database.DatabasePath);
        await store.InitializeAndLoadAsync(CancellationToken.None);

        using (SqliteConnection connection = OpenInspectionConnection(
                   database.DatabasePath))
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO personal_dictionary
                    (spoken_form, replacement, updated_utc)
                VALUES
                    ($spokenForm, $replacement, $updatedUtc);
                """;
            command.Parameters.AddWithValue("$spokenForm", "source");
            command.Parameters.AddWithValue(
                "$replacement",
                new string('x', 5000));
            command.Parameters.AddWithValue(
                "$updatedUtc",
                DateTimeOffset.UtcNow.ToString(
                    "O",
                    CultureInfo.InvariantCulture));
            command.ExecuteNonQuery();
        }

        await Assert.ThrowsAsync<InvalidDataException>(
            () => store.InitializeAndLoadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Oversized_spoken_form_is_rejected_with_bounded_loading()
    {
        using TemporaryDatabase database = new();
        SqlitePersonalDictionaryStore store = new(database.DatabasePath);
        await store.InitializeAndLoadAsync(CancellationToken.None);

        using (SqliteConnection connection = OpenInspectionConnection(
                   database.DatabasePath))
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO personal_dictionary
                    (spoken_form, replacement, updated_utc)
                VALUES
                    ($spokenForm, $replacement, $updatedUtc);
                """;
            command.Parameters.AddWithValue(
                "$spokenForm",
                new string('s', 5000));
            command.Parameters.AddWithValue("$replacement", "replacement");
            command.Parameters.AddWithValue(
                "$updatedUtc",
                DateTimeOffset.UtcNow.ToString(
                    "O",
                    CultureInfo.InvariantCulture));
            command.ExecuteNonQuery();
        }

        await Assert.ThrowsAsync<InvalidDataException>(
            () => store.InitializeAndLoadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Corrupt_database_is_rejected_and_left_byte_for_byte_unchanged()
    {
        using TemporaryDatabase database = new();
        Directory.CreateDirectory(database.RootPath);

        byte[] corruptBytes = Enumerable.Range(0, 512)
            .Select(index => unchecked((byte)(index * 31 + 17)))
            .ToArray();
        Encoding.ASCII.GetBytes("not-a-sqlite-db").CopyTo(corruptBytes, 0);
        File.WriteAllBytes(database.DatabasePath, corruptBytes);

        SqlitePersonalDictionaryStore store = new(database.DatabasePath);

        await Assert.ThrowsAsync<SqliteException>(
            () => store.InitializeAndLoadAsync(CancellationToken.None));

        Assert.Equal(corruptBytes, File.ReadAllBytes(database.DatabasePath));
        AssertNoSQLiteSidecars(database.DatabasePath);
    }

    [Fact]
    public async Task Precancelled_initialization_and_mutations_perform_no_io()
    {
        using TemporaryDatabase cancelledInitializationDatabase = new();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        SqlitePersonalDictionaryStore cancelledInitializationStore = new(
            cancelledInitializationDatabase.DatabasePath);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => cancelledInitializationStore.InitializeAndLoadAsync(
                cancellation.Token));

        Assert.False(
            Directory.Exists(cancelledInitializationDatabase.RootPath));

        using TemporaryDatabase initializedDatabase = new();
        SqlitePersonalDictionaryStore initializedStore = new(
            initializedDatabase.DatabasePath);
        await initializedStore.InitializeAndLoadAsync(CancellationToken.None);
        byte[] bytesBefore = File.ReadAllBytes(initializedDatabase.DatabasePath);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => initializedStore.UpsertAsync(
                new PersonalDictionaryStorageEntry("source", "replacement"),
                cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => initializedStore.DeleteAsync("source", cancellation.Token));

        Assert.Equal(bytesBefore, File.ReadAllBytes(initializedDatabase.DatabasePath));
    }

    [Fact]
    public async Task Version_zero_database_with_existing_schema_is_not_repaired()
    {
        using TemporaryDatabase database = new();
        Directory.CreateDirectory(database.RootPath);
        SqlitePersonalDictionaryStore bootstrapStore = new(database.DatabasePath);
        await bootstrapStore.InitializeAndLoadAsync(CancellationToken.None);

        using (SqliteConnection connection = OpenInspectionConnection(
                   database.DatabasePath))
        {
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText =
                """
                DROP TABLE personal_dictionary;
                CREATE TABLE foreign_schema(value TEXT NOT NULL);
                INSERT INTO foreign_schema(value) VALUES ('preserve me');
                PRAGMA user_version = 0;
                """;
            command.ExecuteNonQuery();
        }

        byte[] bytesBefore = File.ReadAllBytes(database.DatabasePath);
        SqlitePersonalDictionaryStore store = new(database.DatabasePath);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => store.InitializeAndLoadAsync(CancellationToken.None));

        Assert.Equal(bytesBefore, File.ReadAllBytes(database.DatabasePath));
    }

    private static SqliteConnection OpenInspectionConnection(string databasePath)
    {
        SqliteConnectionStringBuilder connectionStringBuilder = new()
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWrite,
            Pooling = false
        };

        SqliteConnection connection = new(connectionStringBuilder.ToString());
        connection.Open();
        connection.CreateCollation(
            "NYX_ORDINAL_IGNORE_CASE",
            StringComparer.OrdinalIgnoreCase.Compare);
        return connection;
    }

    private static void AssertNoSQLiteSidecars(string databasePath)
    {
        Assert.False(File.Exists(databasePath + "-journal"));
        Assert.False(File.Exists(databasePath + "-wal"));
        Assert.False(File.Exists(databasePath + "-shm"));
    }

    private static long ExecuteScalarInt64(
        SqliteConnection connection,
        string commandText)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = commandText;
        return Convert.ToInt64(
            command.ExecuteScalar(),
            CultureInfo.InvariantCulture);
    }

    private sealed class TemporaryDatabase : IDisposable
    {
        public TemporaryDatabase()
        {
            RootPath = Path.Combine(
                AppContext.BaseDirectory,
                "temporary-databases",
                Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
            DatabasePath = Path.Combine(RootPath, "fluent-test.db");
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
