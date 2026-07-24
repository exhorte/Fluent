using System.Globalization;
using Fluent.Rewrite.Dictionary;
using Fluent.Persistence.Dictionary;

namespace Fluent.IntegrationTests;

public sealed class PersistentDictionaryIntegrationTests
{
    [Fact]
    public async Task Persisted_correction_is_reloaded_and_applied_after_restart()
    {
        using TemporaryDatabase database = new();

        PersistentPersonalDictionary firstSession = new(
            new SqlitePersonalDictionaryStore(database.DatabasePath));
        await firstSession.InitializeAsync();

        DictionaryMutationResult added = await firstSession.AddOrUpdateAsync(
            "nyx voice",
            "Fluent");

        Assert.Equal(DictionaryMutationOutcome.Added, added.Outcome);
        Assert.Equal(DictionaryStorageMode.Persistent, firstSession.StorageMode);

        PersistentPersonalDictionary restartedSession = new(
            new SqlitePersonalDictionaryStore(database.DatabasePath));
        await restartedSession.InitializeAsync();

        DictionaryProcessingResult processingResult =
            new PersonalDictionaryProcessor().Apply(
                "lancer nyx voice",
                restartedSession.CreateSnapshot());

        Assert.Equal(DictionaryProcessingOutcome.Applied, processingResult.Outcome);
        Assert.Equal("lancer Fluent", processingResult.Text);

        DictionaryMutationResult updated =
            await restartedSession.AddOrUpdateAsync(
                "nyx voice",
                "Fluent Desktop");
        Assert.Equal(DictionaryMutationOutcome.Updated, updated.Outcome);

        PersistentPersonalDictionary updatedRestart = new(
            new SqlitePersonalDictionaryStore(database.DatabasePath));
        await updatedRestart.InitializeAsync();

        Assert.Equal(
            new PersonalDictionaryEntry("nyx voice", "Fluent Desktop"),
            Assert.Single(updatedRestart.CreateSnapshot()));

        DictionaryMutationResult removed =
            await updatedRestart.RemoveAsync("nyx voice");
        Assert.Equal(DictionaryMutationOutcome.Removed, removed.Outcome);

        PersistentPersonalDictionary deletedRestart = new(
            new SqlitePersonalDictionaryStore(database.DatabasePath));
        await deletedRestart.InitializeAsync();

        Assert.Empty(deletedRestart.CreateSnapshot());
    }

    [Fact]
    public async Task Corrupt_database_enters_empty_session_fallback_unchanged()
    {
        using TemporaryDatabase database = new();
        Directory.CreateDirectory(database.RootPath);
        byte[] corruptBytes = Enumerable.Range(0, 512)
            .Select(index => unchecked((byte)(index * 17 + 11)))
            .ToArray();
        File.WriteAllBytes(database.DatabasePath, corruptBytes);

        PersistentPersonalDictionary dictionary = new(
            new SqlitePersonalDictionaryStore(database.DatabasePath));

        await dictionary.InitializeAsync();

        Assert.Equal(
            DictionaryStorageMode.SessionOnlyFallback,
            dictionary.StorageMode);
        Assert.Empty(dictionary.CreateSnapshot());
        Assert.Equal(corruptBytes, File.ReadAllBytes(database.DatabasePath));
        Assert.Contains(
            "session",
            dictionary.StatusMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Locked_database_enters_fallback_without_losing_saved_data()
    {
        using TemporaryDatabase database = new();
        PersistentPersonalDictionary initialDictionary = new(
            new SqlitePersonalDictionaryStore(database.DatabasePath));
        await initialDictionary.InitializeAsync();
        await initialDictionary.AddOrUpdateAsync("alpha", "Alpha");

        await using (FileStream lockStream = new(
                         database.DatabasePath,
                         FileMode.Open,
                         FileAccess.ReadWrite,
                         FileShare.None))
        {
            PersistentPersonalDictionary lockedDictionary = new(
                new SqlitePersonalDictionaryStore(database.DatabasePath));

            await lockedDictionary.InitializeAsync();

            Assert.Equal(
                DictionaryStorageMode.SessionOnlyFallback,
                lockedDictionary.StorageMode);
            Assert.Empty(lockedDictionary.CreateSnapshot());
        }

        PersistentPersonalDictionary recoveredDictionary = new(
            new SqlitePersonalDictionaryStore(database.DatabasePath));
        await recoveredDictionary.InitializeAsync();

        Assert.Equal(DictionaryStorageMode.Persistent, recoveredDictionary.StorageMode);
        Assert.Equal(
            new PersonalDictionaryEntry("alpha", "Alpha"),
            Assert.Single(recoveredDictionary.CreateSnapshot()));
    }

    private sealed class TemporaryDatabase : IDisposable
    {
        public TemporaryDatabase()
        {
            RootPath = Path.Combine(
                AppContext.BaseDirectory,
                "temporary-databases",
                Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture));
            DatabasePath = Path.Combine(RootPath, "fluent-integration.db");
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
