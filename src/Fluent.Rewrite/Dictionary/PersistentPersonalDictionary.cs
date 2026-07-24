using Fluent.Core.Dictionary;

namespace Fluent.Rewrite.Dictionary;

public sealed class PersistentPersonalDictionary
{
    private const string LoadingMessage =
        "Chargement du dictionnaire local…";
    private const string PersistentMessage =
        "Dictionnaire enregistré localement sur cet appareil.";
    private const string LoadingMutationMessage =
        "Le dictionnaire local est encore en cours de chargement.";
    private const string FallbackMessage =
        "Sauvegarde locale indisponible : corrections actives pour cette session uniquement.";
    private const string WriteFailureMessage =
        "La sauvegarde locale a échoué. La correction n'a pas été appliquée ; " +
        "le dictionnaire reste disponible pour cette session uniquement.";

    private readonly IPersonalDictionaryStore _store;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private volatile SessionDictionary _dictionary = new();
    private volatile DictionaryStorageMode _storageMode =
        DictionaryStorageMode.Loading;
    private volatile string _statusMessage = LoadingMessage;

    public PersistentPersonalDictionary(IPersonalDictionaryStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        _store = store;
    }

    public DictionaryStorageMode StorageMode => _storageMode;

    public int Count => _dictionary.Count;

    public string StatusMessage => _statusMessage;

    public IReadOnlyList<PersonalDictionaryEntry> CreateSnapshot()
    {
        return _dictionary.CreateSnapshot();
    }

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            SessionDictionary previousDictionary = _dictionary;
            DictionaryStorageMode previousMode = _storageMode;
            string previousStatusMessage = _statusMessage;

            _storageMode = DictionaryStorageMode.Loading;
            _statusMessage = LoadingMessage;

            try
            {
                IReadOnlyList<PersonalDictionaryStorageEntry> storedEntries =
                    await _store.InitializeAndLoadAsync(cancellationToken)
                        .ConfigureAwait(false);
                SessionDictionary loadedDictionary =
                    HydrateValidatedDictionary(storedEntries);

                _dictionary = loadedDictionary;
                _storageMode = DictionaryStorageMode.Persistent;
                _statusMessage = PersistentMessage;
            }
            catch (OperationCanceledException)
            {
                _dictionary = previousDictionary;
                _storageMode = previousMode;
                _statusMessage = previousStatusMessage;
                throw;
            }
            catch (Exception)
            {
                _dictionary = new SessionDictionary();
                _storageMode = DictionaryStorageMode.SessionOnlyFallback;
                _statusMessage = FallbackMessage;
            }
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async Task<DictionaryMutationResult> AddOrUpdateAsync(
        string? spokenForm,
        string? replacement,
        CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_storageMode == DictionaryStorageMode.Loading)
            {
                return new DictionaryMutationResult(
                    DictionaryMutationOutcome.Rejected,
                    LoadingMutationMessage);
            }

            SessionDictionary currentDictionary = _dictionary;
            SessionDictionary stagedDictionary = Clone(currentDictionary);
            DictionaryMutationResult stagedResult =
                stagedDictionary.AddOrUpdate(spokenForm, replacement);
            if (!stagedResult.Succeeded)
            {
                return stagedResult;
            }

            if (_storageMode == DictionaryStorageMode.SessionOnlyFallback)
            {
                _dictionary = stagedDictionary;
                return WithSessionOnlyMessage(stagedResult);
            }

            PersonalDictionaryEntry normalizedEntry =
                FindEntry(stagedDictionary, spokenForm!);

            try
            {
                await _store.UpsertAsync(
                        new PersonalDictionaryStorageEntry(
                            normalizedEntry.SpokenForm,
                            normalizedEntry.Replacement),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                SwitchToFallbackRetaining(currentDictionary);
                return new DictionaryMutationResult(
                    DictionaryMutationOutcome.Rejected,
                    WriteFailureMessage);
            }

            _dictionary = stagedDictionary;
            return stagedResult;
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async Task<DictionaryMutationResult> RemoveAsync(
        string? spokenForm,
        CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_storageMode == DictionaryStorageMode.Loading)
            {
                return new DictionaryMutationResult(
                    DictionaryMutationOutcome.Rejected,
                    LoadingMutationMessage);
            }

            SessionDictionary currentDictionary = _dictionary;
            SessionDictionary stagedDictionary = Clone(currentDictionary);
            DictionaryMutationResult stagedResult =
                stagedDictionary.Remove(spokenForm);
            if (!stagedResult.Succeeded)
            {
                return stagedResult;
            }

            if (_storageMode == DictionaryStorageMode.SessionOnlyFallback)
            {
                _dictionary = stagedDictionary;
                return WithSessionOnlyMessage(stagedResult);
            }

            try
            {
                bool deleted = await _store.DeleteAsync(
                        spokenForm!.Trim(),
                        cancellationToken)
                    .ConfigureAwait(false);
                if (!deleted)
                {
                    SwitchToFallbackRetaining(currentDictionary);
                    return new DictionaryMutationResult(
                        DictionaryMutationOutcome.Rejected,
                        WriteFailureMessage);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
                SwitchToFallbackRetaining(currentDictionary);
                return new DictionaryMutationResult(
                    DictionaryMutationOutcome.Rejected,
                    WriteFailureMessage);
            }

            _dictionary = stagedDictionary;
            return stagedResult;
        }
        finally
        {
            _operationGate.Release();
        }
    }

    /// <summary>
    /// Applies an import plan's entries (adds and updates) through the same
    /// validated, gated, persisted upsert path. Returns how many were applied.
    /// </summary>
    public async Task<int> ApplyImportAsync(
        IReadOnlyList<PersonalDictionaryEntry> entries,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entries);

        int applied = 0;
        foreach (PersonalDictionaryEntry entry in entries)
        {
            DictionaryMutationResult result = await AddOrUpdateAsync(
                    entry.SpokenForm,
                    entry.Replacement,
                    cancellationToken)
                .ConfigureAwait(false);
            if (result.Succeeded)
            {
                applied++;
            }
        }

        return applied;
    }

    private static SessionDictionary HydrateValidatedDictionary(
        IReadOnlyList<PersonalDictionaryStorageEntry>? storedEntries)
    {
        if (storedEntries is null ||
            storedEntries.Count > SessionDictionary.MaximumEntryCount)
        {
            throw new InvalidOperationException(
                "The stored dictionary is invalid.");
        }

        SessionDictionary dictionary = new();
        HashSet<string> spokenForms = new(StringComparer.OrdinalIgnoreCase);

        foreach (PersonalDictionaryStorageEntry? storedEntry in storedEntries)
        {
            if (storedEntry is null ||
                !PersonalDictionaryValidation.TryNormalize(
                    storedEntry.SpokenForm,
                    storedEntry.Replacement,
                    out PersonalDictionaryEntry? normalizedEntry,
                    out _))
            {
                throw new InvalidOperationException(
                    "The stored dictionary is invalid.");
            }

            if (!spokenForms.Add(normalizedEntry!.SpokenForm))
            {
                throw new InvalidOperationException(
                    "The stored dictionary contains duplicate entries.");
            }

            DictionaryMutationResult result = dictionary.AddOrUpdate(
                normalizedEntry.SpokenForm,
                normalizedEntry.Replacement);
            if (result.Outcome != DictionaryMutationOutcome.Added)
            {
                throw new InvalidOperationException(
                    "The stored dictionary is invalid.");
            }
        }

        return dictionary;
    }

    private static SessionDictionary Clone(SessionDictionary source)
    {
        PersonalDictionaryStorageEntry[] entries = source.CreateSnapshot()
            .Select(entry => new PersonalDictionaryStorageEntry(
                entry.SpokenForm,
                entry.Replacement))
            .ToArray();
        return HydrateValidatedDictionary(entries);
    }

    private static PersonalDictionaryEntry FindEntry(
        SessionDictionary dictionary,
        string spokenForm)
    {
        string normalizedSpokenForm = spokenForm.Trim();
        return dictionary.CreateSnapshot().Single(
            entry => string.Equals(
                entry.SpokenForm,
                normalizedSpokenForm,
                StringComparison.OrdinalIgnoreCase));
    }

    private static DictionaryMutationResult WithSessionOnlyMessage(
        DictionaryMutationResult result)
    {
        string message = result.Outcome switch
        {
            DictionaryMutationOutcome.Added =>
                "Correction ajoutée pour cette session uniquement.",
            DictionaryMutationOutcome.Updated =>
                "Correction mise à jour pour cette session uniquement.",
            DictionaryMutationOutcome.Removed =>
                "Correction supprimée pour cette session uniquement.",
            _ => result.Message
        };
        return result with { Message = message };
    }

    private void SwitchToFallbackRetaining(
        SessionDictionary currentDictionary)
    {
        _dictionary = currentDictionary;
        _storageMode = DictionaryStorageMode.SessionOnlyFallback;
        _statusMessage = FallbackMessage;
    }
}
