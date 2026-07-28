namespace Fluent.Core.History;

/// <summary>
/// Immutable point-in-time view of the local history store: the opt-in
/// preference plus the retained entries, newest first.
/// </summary>
public sealed record DictationHistorySnapshot(
    DictationHistoryPreferences Preferences,
    IReadOnlyList<DictationHistoryEntry> Entries);
