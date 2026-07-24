namespace Fluent.Core.History;

/// <summary>
/// Local persistence boundary for dictation history. Implementations live
/// outside <c>Fluent.Core</c>. All data is local; no audio or secret is stored.
/// </summary>
public interface IDictationHistoryStore
{
    /// <summary>
    /// Ensures the store exists and returns the current preferences and the
    /// retained entries (newest first, bounded by the retention cap).
    /// </summary>
    Task<DictationHistorySnapshot> InitializeAndLoadAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Appends one entry and prunes older entries beyond the retention cap.
    /// </summary>
    Task AppendAsync(
        DictationHistoryEntry entry,
        CancellationToken cancellationToken);

    /// <summary>Deletes a single entry by identifier.</summary>
    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken);

    /// <summary>Deletes every entry and returns the number removed.</summary>
    Task<int> ClearAsync(
        CancellationToken cancellationToken);

    /// <summary>Persists the opt-in preference.</summary>
    Task SetEnabledAsync(
        bool enabled,
        CancellationToken cancellationToken);
}
