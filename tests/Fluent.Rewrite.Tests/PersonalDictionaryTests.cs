using Fluent.Core.Dictionary;
using Fluent.Rewrite.Dictionary;
using Fluent.Rewrite.Profiles;
using Fluent.Rewrite.Rewriting;
using Fluent.Rewrite.Validation;

namespace Fluent.Rewrite.Tests;

public sealed class SessionDictionaryTests
{
    [Fact]
    public void Add_update_remove_and_case_insensitive_uniqueness_are_deterministic()
    {
        SessionDictionary dictionary = new();

        DictionaryMutationResult added = dictionary.AddOrUpdate(
            "  nyx voice  ",
            "  Fluent  ");
        DictionaryMutationResult updated = dictionary.AddOrUpdate(
            "NYX VOICE",
            "Fluent Desktop");

        Assert.Equal(DictionaryMutationOutcome.Added, added.Outcome);
        Assert.True(added.Succeeded);
        Assert.Equal(DictionaryMutationOutcome.Updated, updated.Outcome);
        Assert.True(updated.Succeeded);
        Assert.Equal(1, dictionary.Count);
        Assert.Equal(
            new PersonalDictionaryEntry("NYX VOICE", "Fluent Desktop"),
            Assert.Single(dictionary.CreateSnapshot()));
        DictionaryMutationResult removed = dictionary.Remove("  nyx voice ");
        DictionaryMutationResult missing = dictionary.Remove("nyx voice");
        Assert.Equal(DictionaryMutationOutcome.Removed, removed.Outcome);
        Assert.True(removed.Succeeded);
        Assert.Equal(DictionaryMutationOutcome.Rejected, missing.Outcome);
        Assert.False(missing.Succeeded);
        Assert.Equal(0, dictionary.Count);
    }

    [Theory]
    [InlineData(null, "Valeur")]
    [InlineData("Source", null)]
    [InlineData("   ", "Valeur")]
    [InlineData("Source", "   ")]
    [InlineData("Source\ninterdite", "Valeur")]
    [InlineData("Source", "Valeur\tinterdite")]
    [InlineData(".", "point")]
    [InlineData("Identique", "Identique")]
    public void Invalid_entries_are_rejected(
        string? spokenForm,
        string? replacement)
    {
        SessionDictionary dictionary = new();

        DictionaryMutationResult result = dictionary.AddOrUpdate(
            spokenForm,
            replacement);

        Assert.Equal(DictionaryMutationOutcome.Rejected, result.Outcome);
        Assert.False(result.Succeeded);
        Assert.NotEmpty(result.Message);
        Assert.Equal(0, dictionary.Count);
    }

    [Theory]
    [InlineData("mot\u200Bcache", "valeur")]
    [InlineData("source", "texte\u202Einversé")]
    [InlineData("source\u2028suite", "valeur")]
    [InlineData("\uD800", "valeur")]
    public void Invisible_directional_separator_and_invalid_unicode_are_rejected(
        string spokenForm,
        string replacement)
    {
        SessionDictionary dictionary = new();

        DictionaryMutationResult result = dictionary.AddOrUpdate(
            spokenForm,
            replacement);

        Assert.Equal(DictionaryMutationOutcome.Rejected, result.Outcome);
        Assert.False(result.Succeeded);
        Assert.Equal(0, dictionary.Count);
    }

    [Fact]
    public void Supplementary_plane_letters_are_valid_dictionary_terms()
    {
        SessionDictionary dictionary = new();

        DictionaryMutationResult result = dictionary.AddOrUpdate("𐐀", "lettre");

        Assert.Equal(DictionaryMutationOutcome.Added, result.Outcome);
    }

    [Fact]
    public void Case_only_correction_is_not_treated_as_a_no_op()
    {
        SessionDictionary dictionary = new();

        DictionaryMutationResult result = dictionary.AddOrUpdate(
            "fluent",
            "Fluent");

        Assert.Equal(DictionaryMutationOutcome.Added, result.Outcome);
    }

    [Fact]
    public void Oversized_entries_are_rejected()
    {
        SessionDictionary dictionary = new();

        DictionaryMutationResult spokenResult = dictionary.AddOrUpdate(
            new string('a', SessionDictionary.MaximumSpokenFormLength + 1),
            "Valeur");
        DictionaryMutationResult replacementResult = dictionary.AddOrUpdate(
            "Source",
            new string('b', SessionDictionary.MaximumReplacementLength + 1));

        Assert.Equal(DictionaryMutationOutcome.Rejected, spokenResult.Outcome);
        Assert.Equal(DictionaryMutationOutcome.Rejected, replacementResult.Outcome);
        Assert.Equal(0, dictionary.Count);
    }

    [Fact]
    public void Capacity_rejects_a_new_entry_but_still_allows_an_update()
    {
        SessionDictionary dictionary = new();
        for (int index = 0; index < SessionDictionary.MaximumEntryCount; index++)
        {
            Assert.True(dictionary.AddOrUpdate($"mot{index}", $"terme{index}").Succeeded);
        }

        DictionaryMutationResult overflow = dictionary.AddOrUpdate(
            "nouveau",
            "remplacement");
        DictionaryMutationResult update = dictionary.AddOrUpdate(
            "MOT0",
            "mise à jour");

        Assert.Equal(DictionaryMutationOutcome.Rejected, overflow.Outcome);
        Assert.Equal(DictionaryMutationOutcome.Updated, update.Outcome);
        Assert.Equal(SessionDictionary.MaximumEntryCount, dictionary.Count);
    }

    [Fact]
    public void Snapshot_is_a_detached_read_only_copy()
    {
        SessionDictionary dictionary = new();
        dictionary.AddOrUpdate("beta", "B");
        IReadOnlyList<PersonalDictionaryEntry> snapshot = dictionary.CreateSnapshot();

        dictionary.AddOrUpdate("alpha", "A");
        dictionary.Remove("beta");

        Assert.Equal(
            new PersonalDictionaryEntry("beta", "B"),
            Assert.Single(snapshot));
        Assert.IsAssignableFrom<IReadOnlyList<PersonalDictionaryEntry>>(snapshot);
        Assert.Equal(
            ["alpha"],
            dictionary.CreateSnapshot().Select(entry => entry.SpokenForm));
    }
}

public sealed class PersistentPersonalDictionaryTests
{
    [Fact]
    public async Task Mutations_before_initialization_are_rejected_without_store_io()
    {
        FakePersonalDictionaryStore store = new();
        PersistentPersonalDictionary dictionary = new(store);

        DictionaryMutationResult addResult =
            await dictionary.AddOrUpdateAsync("alpha", "Alpha");
        DictionaryMutationResult removeResult =
            await dictionary.RemoveAsync("alpha");

        Assert.Equal(DictionaryMutationOutcome.Rejected, addResult.Outcome);
        Assert.Equal(DictionaryMutationOutcome.Rejected, removeResult.Outcome);
        Assert.Equal(DictionaryStorageMode.Loading, dictionary.StorageMode);
        Assert.Empty(dictionary.CreateSnapshot());
        Assert.Equal(0, store.UpsertCallCount);
        Assert.Equal(0, store.DeleteCallCount);
    }

    [Fact]
    public async Task Valid_rows_are_normalized_and_hydrated_in_persistent_mode()
    {
        FakePersonalDictionaryStore store = new()
        {
            StoredEntries =
            [
                new("  nyx voice  ", "  Fluent  "),
                new("alpha", "Alpha corrigé")
            ]
        };
        PersistentPersonalDictionary dictionary = new(store);

        await dictionary.InitializeAsync();

        Assert.Equal(DictionaryStorageMode.Persistent, dictionary.StorageMode);
        Assert.Equal(2, dictionary.Count);
        Assert.Equal(
            [
                new PersonalDictionaryEntry("alpha", "Alpha corrigé"),
                new PersonalDictionaryEntry("nyx voice", "Fluent")
            ],
            dictionary.CreateSnapshot());
        Assert.Contains(
            "localement",
            dictionary.StatusMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Invalid_row_uses_an_empty_session_only_fallback()
    {
        FakePersonalDictionaryStore store = new()
        {
            StoredEntries =
            [
                new(".", "point")
            ]
        };
        PersistentPersonalDictionary dictionary = new(store);

        await dictionary.InitializeAsync();

        AssertEmptyFallback(dictionary);
    }

    [Fact]
    public async Task Normalized_case_insensitive_duplicate_uses_empty_fallback()
    {
        FakePersonalDictionaryStore store = new()
        {
            StoredEntries =
            [
                new(" Nyx Voice ", "Fluent"),
                new("NYX VOICE", "Fluent Desktop")
            ]
        };
        PersistentPersonalDictionary dictionary = new(store);

        await dictionary.InitializeAsync();

        AssertEmptyFallback(dictionary);
    }

    [Fact]
    public async Task Over_capacity_storage_uses_empty_fallback()
    {
        FakePersonalDictionaryStore store = new()
        {
            StoredEntries = Enumerable
                .Range(0, SessionDictionary.MaximumEntryCount + 1)
                .Select(index => new PersonalDictionaryStorageEntry(
                    $"mot{index}",
                    $"terme{index}"))
                .ToArray()
        };
        PersistentPersonalDictionary dictionary = new(store);

        await dictionary.InitializeAsync();

        AssertEmptyFallback(dictionary);
    }

    [Fact]
    public async Task Upsert_is_persisted_before_the_staged_snapshot_is_published()
    {
        TaskCompletionSource persistenceEntered = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource allowPersistence = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        FakePersonalDictionaryStore store = new()
        {
            UpsertHandler = async (_, cancellationToken) =>
            {
                persistenceEntered.SetResult();
                await allowPersistence.Task.WaitAsync(cancellationToken);
            }
        };
        PersistentPersonalDictionary dictionary = new(store);
        await dictionary.InitializeAsync();

        Task<DictionaryMutationResult> mutation = dictionary.AddOrUpdateAsync(
            "nyx voice",
            "Fluent");
        await persistenceEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.False(mutation.IsCompleted);
        Assert.Equal(0, dictionary.Count);
        Assert.Empty(dictionary.CreateSnapshot());

        allowPersistence.SetResult();
        DictionaryMutationResult result = await mutation;

        Assert.Equal(DictionaryMutationOutcome.Added, result.Outcome);
        Assert.Equal(1, dictionary.Count);
        Assert.Equal(
            new PersonalDictionaryEntry("nyx voice", "Fluent"),
            Assert.Single(dictionary.CreateSnapshot()));
    }

    [Fact]
    public async Task Concurrent_mutations_are_serialized_in_call_order()
    {
        TaskCompletionSource firstPersistenceEntered = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFirstPersistence = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        int activeStoreCalls = 0;
        int maximumActiveStoreCalls = 0;

        FakePersonalDictionaryStore store = new()
        {
            UpsertHandler = async (entry, cancellationToken) =>
            {
                int active = Interlocked.Increment(ref activeStoreCalls);
                InterlockedExtensions.Max(
                    ref maximumActiveStoreCalls,
                    active);
                try
                {
                    if (entry.SpokenForm == "alpha")
                    {
                        firstPersistenceEntered.SetResult();
                        await releaseFirstPersistence.Task.WaitAsync(
                            cancellationToken);
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref activeStoreCalls);
                }
            }
        };
        PersistentPersonalDictionary dictionary = new(store);
        await dictionary.InitializeAsync();

        Task<DictionaryMutationResult> first =
            dictionary.AddOrUpdateAsync("alpha", "Alpha");
        await firstPersistenceEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Task<DictionaryMutationResult> second =
            dictionary.AddOrUpdateAsync("beta", "Beta");

        Assert.Equal(1, store.UpsertCallCount);

        releaseFirstPersistence.SetResult();
        DictionaryMutationResult[] results =
            await Task.WhenAll(first, second);

        Assert.All(results, result => Assert.True(result.Succeeded));
        Assert.Equal(1, maximumActiveStoreCalls);
        Assert.Equal(
            ["alpha", "beta"],
            dictionary.CreateSnapshot().Select(entry => entry.SpokenForm));
    }

    [Fact]
    public async Task Write_failure_keeps_prior_snapshot_and_enters_safe_fallback()
    {
        FakePersonalDictionaryStore store = new()
        {
            StoredEntries =
            [
                new("alpha", "Alpha")
            ],
            UpsertHandler = (_, _) =>
                throw new InvalidOperationException("SQLITE_SECRET_DETAILS")
        };
        PersistentPersonalDictionary dictionary = new(store);
        await dictionary.InitializeAsync();
        IReadOnlyList<PersonalDictionaryEntry> priorSnapshot =
            dictionary.CreateSnapshot();

        DictionaryMutationResult result = await dictionary.AddOrUpdateAsync(
            "beta",
            "Beta");

        Assert.Equal(DictionaryMutationOutcome.Rejected, result.Outcome);
        Assert.Equal(
            DictionaryStorageMode.SessionOnlyFallback,
            dictionary.StorageMode);
        Assert.Equal(priorSnapshot, dictionary.CreateSnapshot());
        Assert.False(dictionary.StatusMessage.Contains(
            "SQLITE_SECRET_DETAILS",
            StringComparison.Ordinal));
        Assert.Contains(
            "session",
            result.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Delete_failure_keeps_the_entry_and_enters_fallback()
    {
        FakePersonalDictionaryStore store = new()
        {
            StoredEntries =
            [
                new("alpha", "Alpha")
            ],
            DeleteHandler = (_, _) => Task.FromResult(false)
        };
        PersistentPersonalDictionary dictionary = new(store);
        await dictionary.InitializeAsync();

        DictionaryMutationResult result =
            await dictionary.RemoveAsync("alpha");

        Assert.Equal(DictionaryMutationOutcome.Rejected, result.Outcome);
        Assert.Equal(
            DictionaryStorageMode.SessionOnlyFallback,
            dictionary.StorageMode);
        Assert.Equal(
            new PersonalDictionaryEntry("alpha", "Alpha"),
            Assert.Single(dictionary.CreateSnapshot()));
    }

    [Fact]
    public async Task Fallback_crud_is_session_only_and_never_retries_the_store()
    {
        FakePersonalDictionaryStore store = new()
        {
            InitializeHandler = _ =>
                throw new InvalidOperationException("database unavailable")
        };
        PersistentPersonalDictionary dictionary = new(store);
        await dictionary.InitializeAsync();

        DictionaryMutationResult added = await dictionary.AddOrUpdateAsync(
            "alpha",
            "Alpha");
        DictionaryMutationResult updated = await dictionary.AddOrUpdateAsync(
            "ALPHA",
            "Alpha deux");
        DictionaryMutationResult removed =
            await dictionary.RemoveAsync("alpha");

        Assert.Equal(DictionaryMutationOutcome.Added, added.Outcome);
        Assert.Equal(DictionaryMutationOutcome.Updated, updated.Outcome);
        Assert.Equal(DictionaryMutationOutcome.Removed, removed.Outcome);
        Assert.All(
            new[] { added, updated, removed },
            result => Assert.Contains(
                "session uniquement",
                result.Message,
                StringComparison.OrdinalIgnoreCase));
        Assert.Empty(dictionary.CreateSnapshot());
        Assert.Equal(0, store.UpsertCallCount);
        Assert.Equal(0, store.DeleteCallCount);
    }

    [Fact]
    public async Task Initialization_cancellation_propagates_without_fallback()
    {
        FakePersonalDictionaryStore store = new()
        {
            InitializeHandler = cancellationToken =>
                Task.FromCanceled<IReadOnlyList<PersonalDictionaryStorageEntry>>(
                    cancellationToken)
        };
        PersistentPersonalDictionary dictionary = new(store);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => dictionary.InitializeAsync(cancellation.Token));

        Assert.Equal(DictionaryStorageMode.Loading, dictionary.StorageMode);
        Assert.Empty(dictionary.CreateSnapshot());
    }

    [Fact]
    public async Task In_progress_initialization_cancellation_restores_loading_state()
    {
        TaskCompletionSource initializationEntered = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        FakePersonalDictionaryStore store = new()
        {
            InitializeHandler = async cancellationToken =>
            {
                initializationEntered.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return [];
            }
        };
        PersistentPersonalDictionary dictionary = new(store);
        using CancellationTokenSource cancellation = new();

        Task initialization = dictionary.InitializeAsync(cancellation.Token);
        await initializationEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => initialization);
        Assert.Equal(DictionaryStorageMode.Loading, dictionary.StorageMode);
        Assert.Empty(dictionary.CreateSnapshot());
    }

    [Fact]
    public async Task Write_cancellation_propagates_and_retains_persistent_snapshot()
    {
        FakePersonalDictionaryStore store = new()
        {
            StoredEntries =
            [
                new("alpha", "Alpha")
            ],
            UpsertHandler = (_, cancellationToken) =>
                Task.FromCanceled(cancellationToken)
        };
        PersistentPersonalDictionary dictionary = new(store);
        await dictionary.InitializeAsync();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => dictionary.AddOrUpdateAsync(
                "beta",
                "Beta",
                cancellation.Token));

        Assert.Equal(DictionaryStorageMode.Persistent, dictionary.StorageMode);
        Assert.Equal(
            new PersonalDictionaryEntry("alpha", "Alpha"),
            Assert.Single(dictionary.CreateSnapshot()));
    }

    [Fact]
    public async Task In_progress_write_cancellation_retains_persistent_snapshot()
    {
        TaskCompletionSource writeEntered = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        FakePersonalDictionaryStore store = new()
        {
            StoredEntries =
            [
                new("alpha", "Alpha")
            ],
            UpsertHandler = async (_, cancellationToken) =>
            {
                writeEntered.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
        };
        PersistentPersonalDictionary dictionary = new(store);
        await dictionary.InitializeAsync();
        using CancellationTokenSource cancellation = new();

        Task<DictionaryMutationResult> mutation = dictionary.AddOrUpdateAsync(
            "beta",
            "Beta",
            cancellation.Token);
        await writeEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => mutation);
        Assert.Equal(DictionaryStorageMode.Persistent, dictionary.StorageMode);
        Assert.Equal(
            new PersonalDictionaryEntry("alpha", "Alpha"),
            Assert.Single(dictionary.CreateSnapshot()));
    }

    [Fact]
    public async Task In_progress_delete_cancellation_retains_persistent_snapshot()
    {
        TaskCompletionSource deleteEntered = new(
            TaskCreationOptions.RunContinuationsAsynchronously);
        FakePersonalDictionaryStore store = new()
        {
            StoredEntries =
            [
                new("alpha", "Alpha")
            ],
            DeleteHandler = async (_, cancellationToken) =>
            {
                deleteEntered.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return true;
            }
        };
        PersistentPersonalDictionary dictionary = new(store);
        await dictionary.InitializeAsync();
        using CancellationTokenSource cancellation = new();

        Task<DictionaryMutationResult> mutation = dictionary.RemoveAsync(
            "alpha",
            cancellation.Token);
        await deleteEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => mutation);
        Assert.Equal(DictionaryStorageMode.Persistent, dictionary.StorageMode);
        Assert.Equal(
            new PersonalDictionaryEntry("alpha", "Alpha"),
            Assert.Single(dictionary.CreateSnapshot()));
    }

    [Fact]
    public async Task Snapshots_are_detached_across_successful_persistent_mutations()
    {
        FakePersonalDictionaryStore store = new()
        {
            StoredEntries =
            [
                new("alpha", "Alpha")
            ]
        };
        PersistentPersonalDictionary dictionary = new(store);
        await dictionary.InitializeAsync();
        IReadOnlyList<PersonalDictionaryEntry> snapshot =
            dictionary.CreateSnapshot();

        await dictionary.AddOrUpdateAsync("beta", "Beta");
        await dictionary.RemoveAsync("alpha");

        Assert.Equal(
            new PersonalDictionaryEntry("alpha", "Alpha"),
            Assert.Single(snapshot));
        Assert.Equal(
            new PersonalDictionaryEntry("beta", "Beta"),
            Assert.Single(dictionary.CreateSnapshot()));
    }

    private static void AssertEmptyFallback(
        PersistentPersonalDictionary dictionary)
    {
        Assert.Equal(
            DictionaryStorageMode.SessionOnlyFallback,
            dictionary.StorageMode);
        Assert.Equal(0, dictionary.Count);
        Assert.Empty(dictionary.CreateSnapshot());
        Assert.Contains(
            "session",
            dictionary.StatusMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FakePersonalDictionaryStore :
        IPersonalDictionaryStore
    {
        public IReadOnlyList<PersonalDictionaryStorageEntry> StoredEntries
        {
            get;
            init;
        } = [];

        public Func<
            CancellationToken,
            Task<IReadOnlyList<PersonalDictionaryStorageEntry>>>?
            InitializeHandler { get; init; }

        public Func<
            PersonalDictionaryStorageEntry,
            CancellationToken,
            Task>? UpsertHandler { get; init; }

        public Func<string, CancellationToken, Task<bool>>?
            DeleteHandler { get; init; }

        public int UpsertCallCount { get; private set; }

        public int DeleteCallCount { get; private set; }

        public Task<IReadOnlyList<PersonalDictionaryStorageEntry>>
            InitializeAndLoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return InitializeHandler is null
                ? Task.FromResult(StoredEntries)
                : InitializeHandler(cancellationToken);
        }

        public Task UpsertAsync(
            PersonalDictionaryStorageEntry entry,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            UpsertCallCount++;
            return UpsertHandler is null
                ? Task.CompletedTask
                : UpsertHandler(entry, cancellationToken);
        }

        public Task<bool> DeleteAsync(
            string spokenForm,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeleteCallCount++;
            return DeleteHandler is null
                ? Task.FromResult(true)
                : DeleteHandler(spokenForm, cancellationToken);
        }
    }

    private static class InterlockedExtensions
    {
        public static void Max(ref int location, int value)
        {
            int current = Volatile.Read(ref location);
            while (value > current)
            {
                int original = Interlocked.CompareExchange(
                    ref location,
                    value,
                    current);
                if (original == current)
                {
                    return;
                }

                current = original;
            }
        }
    }
}

public sealed class PersonalDictionaryProcessorTests
{
    private readonly PersonalDictionaryProcessor _processor = new();

    [Fact]
    public void Longest_phrase_wins_and_replacements_do_not_cascade()
    {
        PersonalDictionaryEntry[] snapshot =
        [
            new("new", "nouveau"),
            new("new york", "Paris"),
            new("Paris", "Lyon")
        ];

        DictionaryProcessingResult result = _processor.Apply(
            "new york puis new",
            snapshot);

        Assert.Equal(DictionaryProcessingOutcome.Applied, result.Outcome);
        Assert.Equal("Paris puis nouveau", result.Text);
        Assert.Equal(2, result.ReplacementCount);
    }

    [Theory]
    [InlineData("chat chateau achat chat.", "félin chateau achat félin.", 2, "chat", "félin")]
    [InlineData("FLUENT et fluent", "Fluent et Fluent", 2, "fluent", "Fluent")]
    [InlineData("ÉCOLE et école", "classe et classe", 2, "école", "classe")]
    public void Matching_respects_boundaries_case_and_accents(
        string source,
        string expected,
        int expectedCount,
        string spoken,
        string replacement)
    {
        PersonalDictionaryEntry entry = new(spoken, replacement);

        DictionaryProcessingResult result = _processor.Apply(source, [entry]);

        Assert.Equal(expected, result.Text);
        Assert.Equal(expectedCount, result.ReplacementCount);
    }

    [Fact]
    public void Protected_technical_tokens_are_never_replaced()
    {
        const string source =
            "`secret` https://secret.example/api secret@example.com " +
            "C:\\secret\\file.txt \"C:\\secret folder\\file.txt\" " +
            "/usr/secret/bin v1.2.3 " +
            "package.secret Namespace::secret ticket:secret secret";
        PersonalDictionaryEntry[] snapshot =
        [
            new("secret", "public"),
            new("v1", "version")
        ];

        DictionaryProcessingResult result = _processor.Apply(source, snapshot);

        Assert.Equal(
            "`secret` https://secret.example/api secret@example.com " +
            "C:\\secret\\file.txt \"C:\\secret folder\\file.txt\" " +
            "/usr/secret/bin v1.2.3 " +
            "package.secret Namespace::secret ticket:secret public",
            result.Text);
        Assert.Equal(1, result.ReplacementCount);
    }

    [Theory]
    [InlineData("example.com/secret secret", "secret", "public", "example.com/secret public")]
    [InlineData("élise@example.fr élise", "élise", "Marie", "élise@example.fr Marie")]
    [InlineData("docs/secret/file.txt secret", "secret", "public", "docs/secret/file.txt public")]
    [InlineData("docs\\secret\\file.txt secret", "secret", "public", "docs\\secret\\file.txt public")]
    [InlineData("v1.2.3-beta beta", "beta", "stable", "v1.2.3-beta stable")]
    [InlineData("package.2.secret secret", "secret", "public", "package.2.secret public")]
    [InlineData("build.2026 build", "build", "compile", "build.2026 compile")]
    [InlineData("12:30 12", "12", "13", "12:30 13")]
    [InlineData("123:ABC 123", "123", "456", "123:ABC 456")]
    [InlineData("Namespace::secret secret", "secret", "public", "Namespace::secret public")]
    public void Mixed_structured_tokens_are_protected_as_a_whole(
        string source,
        string spokenForm,
        string replacement,
        string expected)
    {
        DictionaryProcessingResult result = _processor.Apply(
            source,
            [new(spokenForm, replacement)]);

        Assert.Equal(DictionaryProcessingOutcome.Applied, result.Outcome);
        Assert.Equal(expected, result.Text);
        Assert.Equal(1, result.ReplacementCount);
    }

    [Fact]
    public void Supplementary_plane_letter_is_a_word_boundary_character()
    {
        DictionaryProcessingResult result = _processor.Apply(
            "𐐀chat chat",
            [new("chat", "félin")]);

        Assert.Equal("𐐀chat félin", result.Text);
        Assert.Equal(1, result.ReplacementCount);
    }

    [Fact]
    public void Match_that_would_cross_a_protected_span_is_skipped()
    {
        const string source = "ouvrir `secret` maintenant";

        DictionaryProcessingResult result = _processor.Apply(
            source,
            [new("ouvrir `secret`", "remplacé")]);

        Assert.Equal(DictionaryProcessingOutcome.Unchanged, result.Outcome);
        Assert.Same(source, result.Text);
    }

    [Fact]
    public void Invalid_snapshot_returns_the_exact_source()
    {
        string source = new("texte source".ToCharArray());
        PersonalDictionaryEntry[] invalidSnapshot =
        [
            new("doublon", "premier"),
            new("DOUBLON", "second")
        ];

        DictionaryProcessingResult result = _processor.Apply(
            source,
            invalidSnapshot);

        Assert.Equal(
            DictionaryProcessingOutcome.RawFallbackInvalid,
            result.Outcome);
        Assert.Equal(0, result.ReplacementCount);
        Assert.Same(source, result.Text);
    }

    [Fact]
    public void Timeout_returns_the_exact_source()
    {
        string source = new("texte source".ToCharArray());
        PersonalDictionaryProcessor processor = new(TimeSpan.Zero);

        DictionaryProcessingResult result = processor.Apply(
            source,
            [new("texte", "contenu")]);

        Assert.Equal(
            DictionaryProcessingOutcome.RawFallbackTimeout,
            result.Outcome);
        Assert.Equal(0, result.ReplacementCount);
        Assert.Same(source, result.Text);
    }

    [Fact]
    public void Empty_snapshot_returns_the_unchanged_source()
    {
        string source = new("texte source".ToCharArray());

        DictionaryProcessingResult result = _processor.Apply(source, []);

        Assert.Equal(DictionaryProcessingOutcome.Unchanged, result.Outcome);
        Assert.Same(source, result.Text);
    }
}

public sealed class DictionaryRewritePipelineTests
{
    [Fact]
    public async Task Professional_rewrite_receives_dictionary_corrected_text()
    {
        SessionDictionary dictionary = new();
        dictionary.AddOrUpdate("nyx voice", "Fluent");
        PersonalDictionaryProcessor processor = new();
        DictionaryProcessingResult dictionaryResult = processor.Apply(
            "lancer nyx voice",
            dictionary.CreateSnapshot());
        CapturingRewriter rewriter = new();
        SafeProfileRewriteService rewriteService = new(
            rewriter,
            new RewriteOutputValidator());

        RewriteResult rewriteResult = await rewriteService.RewriteAsync(
            dictionaryResult.Text,
            RewriteProfiles.ProfessionalFrench);

        Assert.Equal("lancer Fluent", rewriter.ReceivedText);
        Assert.Equal("lancer Fluent.", rewriteResult.Text);
        Assert.Equal(RewriteOutcome.Applied, rewriteResult.Outcome);
    }

    private sealed class CapturingRewriter : ILocalTextRewriter
    {
        public string? ReceivedText { get; private set; }

        public Task<string> RewriteAsync(
            RewriteRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReceivedText = request.Text;
            return Task.FromResult(request.Text + ".");
        }
    }
}
