namespace Fluent.Rewrite.Dictionary;

public sealed class SessionDictionary
{
    public const int MaximumEntryCount = PersonalDictionaryValidation.MaximumEntryCount;
    public const int MaximumSpokenFormLength = PersonalDictionaryValidation.MaximumSpokenFormLength;
    public const int MaximumReplacementLength = PersonalDictionaryValidation.MaximumReplacementLength;

    private readonly Lock _syncRoot = new();
    private readonly Dictionary<string, PersonalDictionaryEntry> _entries =
        new(StringComparer.OrdinalIgnoreCase);

    public int Count
    {
        get
        {
            lock (_syncRoot)
            {
                return _entries.Count;
            }
        }
    }

    public DictionaryMutationResult AddOrUpdate(
        string? spokenForm,
        string? replacement)
    {
        if (!PersonalDictionaryValidation.TryNormalize(
                spokenForm,
                replacement,
                out PersonalDictionaryEntry? entry,
                out string message))
        {
            return new DictionaryMutationResult(
                DictionaryMutationOutcome.Rejected,
                message);
        }

        lock (_syncRoot)
        {
            bool isUpdate = _entries.ContainsKey(entry!.SpokenForm);
            if (!isUpdate && _entries.Count >= MaximumEntryCount)
            {
                return new DictionaryMutationResult(
                    DictionaryMutationOutcome.Rejected,
                    $"Le dictionnaire de session est limité à {MaximumEntryCount} corrections.");
            }

            _entries[entry.SpokenForm] = entry;
            return isUpdate
                ? new DictionaryMutationResult(
                    DictionaryMutationOutcome.Updated,
                    "Correction mise à jour.")
                : new DictionaryMutationResult(
                    DictionaryMutationOutcome.Added,
                    "Correction ajoutée.");
        }
    }

    public DictionaryMutationResult Remove(string? spokenForm)
    {
        if (string.IsNullOrWhiteSpace(spokenForm))
        {
            return new DictionaryMutationResult(
                DictionaryMutationOutcome.Rejected,
                "La forme prononcée est obligatoire.");
        }

        string normalizedSpokenForm = spokenForm.Trim();
        lock (_syncRoot)
        {
            return _entries.Remove(normalizedSpokenForm)
                ? new DictionaryMutationResult(
                    DictionaryMutationOutcome.Removed,
                    "Correction supprimée.")
                : new DictionaryMutationResult(
                    DictionaryMutationOutcome.Rejected,
                    "Cette correction n'existe plus dans la session.");
        }
    }

    public IReadOnlyList<PersonalDictionaryEntry> CreateSnapshot()
    {
        lock (_syncRoot)
        {
            PersonalDictionaryEntry[] entries = _entries.Values
                .OrderBy(entry => entry.SpokenForm, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.SpokenForm, StringComparer.Ordinal)
                .ToArray();
            return Array.AsReadOnly(entries);
        }
    }
}
