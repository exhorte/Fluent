namespace Fluent.Core.History;

/// <summary>
/// A single recorded dictation. Contains only the produced text and its
/// metadata. No audio is ever stored (P-003).
/// </summary>
public sealed record DictationHistoryEntry(
    Guid Id,
    DateTimeOffset CreatedUtc,
    string Text,
    string? ProfileId);
