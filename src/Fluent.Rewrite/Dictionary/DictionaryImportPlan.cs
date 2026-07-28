namespace Fluent.Rewrite.Dictionary;

public enum DictionaryImportItemStatus
{
    Added,
    Updated,
    SkippedConflict,
    RejectedInvalid,
    SkippedDuplicateInFile,
    SkippedCapacity
}

/// <summary>One imported line and the decision taken for it.</summary>
public sealed record DictionaryImportItem(
    string SpokenForm,
    string Replacement,
    DictionaryImportItemStatus Status,
    string Detail);

/// <summary>
/// The result of planning an import: whether the content parsed, the entries to
/// upsert (adds and updates), and a per-item audit. Pure data; applying it is a
/// separate step.
/// </summary>
public sealed record DictionaryImportPlan(
    bool Parsed,
    string? ParseError,
    IReadOnlyList<PersonalDictionaryEntry> EntriesToUpsert,
    IReadOnlyList<DictionaryImportItem> Items)
{
    public int AddedCount => Count(DictionaryImportItemStatus.Added);

    public int UpdatedCount => Count(DictionaryImportItemStatus.Updated);

    public int SkippedConflictCount => Count(DictionaryImportItemStatus.SkippedConflict);

    public int RejectedCount => Count(DictionaryImportItemStatus.RejectedInvalid);

    public int SkippedDuplicateCount => Count(DictionaryImportItemStatus.SkippedDuplicateInFile);

    public int SkippedCapacityCount => Count(DictionaryImportItemStatus.SkippedCapacity);

    private int Count(DictionaryImportItemStatus status)
    {
        int total = 0;
        foreach (DictionaryImportItem item in Items)
        {
            if (item.Status == status)
            {
                total++;
            }
        }

        return total;
    }
}
