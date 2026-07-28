using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fluent.Rewrite.Dictionary;

/// <summary>
/// Safe, local import/export of the personal dictionary. Export produces a
/// deterministic JSON document; import parses that document, validates every
/// entry with the existing rules, resolves conflicts explicitly, and bounds the
/// result to the dictionary capacity. Imported content is always treated as
/// data — it is never executed and never bypasses validation.
/// </summary>
public static class DictionaryExchange
{
    private static readonly JsonSerializerOptions ExportOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly JsonSerializerOptions ParseOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static string Export(IReadOnlyList<PersonalDictionaryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        ExchangeItem[] items = entries
            .OrderBy(entry => entry.SpokenForm, StringComparer.OrdinalIgnoreCase)
            .Select(entry => new ExchangeItem
            {
                SpokenForm = entry.SpokenForm,
                Replacement = entry.Replacement
            })
            .ToArray();

        return JsonSerializer.Serialize(items, ExportOptions);
    }

    public static DictionaryImportPlan Plan(
        string content,
        IReadOnlyList<PersonalDictionaryEntry> existing,
        DictionaryConflictPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(existing);

        ExchangeItem[]? rawItems;
        try
        {
            rawItems = JsonSerializer.Deserialize<ExchangeItem[]>(content, ParseOptions);
        }
        catch (JsonException)
        {
            return new DictionaryImportPlan(
                false,
                "Le fichier d’import n’est pas un dictionnaire Fluent valide.",
                [],
                []);
        }

        if (rawItems is null)
        {
            return new DictionaryImportPlan(
                false,
                "Le fichier d’import est vide ou invalide.",
                [],
                []);
        }

        Dictionary<string, string> existingMap = new(StringComparer.OrdinalIgnoreCase);
        foreach (PersonalDictionaryEntry entry in existing)
        {
            existingMap[entry.SpokenForm] = entry.Replacement;
        }

        int runningCount = existingMap.Count;
        HashSet<string> seenInFile = new(StringComparer.OrdinalIgnoreCase);
        List<PersonalDictionaryEntry> toUpsert = [];
        List<DictionaryImportItem> audit = [];

        foreach (ExchangeItem raw in rawItems)
        {
            if (!PersonalDictionaryValidation.TryNormalize(
                    raw.SpokenForm,
                    raw.Replacement,
                    out PersonalDictionaryEntry? normalized,
                    out string message))
            {
                audit.Add(new DictionaryImportItem(
                    raw.SpokenForm ?? string.Empty,
                    raw.Replacement ?? string.Empty,
                    DictionaryImportItemStatus.RejectedInvalid,
                    message));
                continue;
            }

            PersonalDictionaryEntry entry = normalized!;

            if (!seenInFile.Add(entry.SpokenForm))
            {
                audit.Add(new DictionaryImportItem(
                    entry.SpokenForm,
                    entry.Replacement,
                    DictionaryImportItemStatus.SkippedDuplicateInFile,
                    "Doublon dans le fichier importé ; première occurrence conservée."));
                continue;
            }

            if (existingMap.ContainsKey(entry.SpokenForm))
            {
                if (policy == DictionaryConflictPolicy.SkipExisting)
                {
                    audit.Add(new DictionaryImportItem(
                        entry.SpokenForm,
                        entry.Replacement,
                        DictionaryImportItemStatus.SkippedConflict,
                        "Existe déjà ; entrée locale conservée."));
                    continue;
                }

                toUpsert.Add(entry);
                audit.Add(new DictionaryImportItem(
                    entry.SpokenForm,
                    entry.Replacement,
                    DictionaryImportItemStatus.Updated,
                    "Entrée existante mise à jour."));
                continue;
            }

            if (runningCount >= PersonalDictionaryValidation.MaximumEntryCount)
            {
                audit.Add(new DictionaryImportItem(
                    entry.SpokenForm,
                    entry.Replacement,
                    DictionaryImportItemStatus.SkippedCapacity,
                    $"Capacité maximale du dictionnaire atteinte ({PersonalDictionaryValidation.MaximumEntryCount})."));
                continue;
            }

            toUpsert.Add(entry);
            runningCount++;
            audit.Add(new DictionaryImportItem(
                entry.SpokenForm,
                entry.Replacement,
                DictionaryImportItemStatus.Added,
                "Ajouté."));
        }

        return new DictionaryImportPlan(true, null, toUpsert, audit);
    }

    private sealed class ExchangeItem
    {
        [JsonPropertyName("spokenForm")]
        public string? SpokenForm { get; set; }

        [JsonPropertyName("replacement")]
        public string? Replacement { get; set; }
    }
}
