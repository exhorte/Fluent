namespace Fluent.Core.History;

public enum DictationHistoryCaptureOutcome
{
    /// <summary>History is disabled; nothing is recorded (opt-in default).</summary>
    SkippedDisabled,

    /// <summary>The dictated text was empty or whitespace.</summary>
    SkippedEmpty,

    /// <summary>The dictated text exceeded the retained length limit.</summary>
    SkippedTooLong,

    /// <summary>An entry was produced and should be persisted.</summary>
    Recorded
}

public sealed record DictationHistoryCaptureDecision(
    DictationHistoryCaptureOutcome Outcome,
    DictationHistoryEntry? Entry)
{
    public bool ShouldRecord => Outcome == DictationHistoryCaptureOutcome.Recorded;
}

/// <summary>
/// Pure decision for whether a completed dictation is recorded in local
/// history. No I/O; deterministic given its inputs so it is fully testable.
/// Honours the opt-in preference and never invents or truncates content.
/// </summary>
public static class DictationHistoryCapturePolicy
{
    public static DictationHistoryCaptureDecision Decide(
        DictationHistoryPreferences preferences,
        string? dictatedText,
        string? profileId,
        Guid id,
        DateTimeOffset createdUtc)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        if (!preferences.IsEnabled)
        {
            return new DictationHistoryCaptureDecision(
                DictationHistoryCaptureOutcome.SkippedDisabled,
                null);
        }

        if (string.IsNullOrWhiteSpace(dictatedText))
        {
            return new DictationHistoryCaptureDecision(
                DictationHistoryCaptureOutcome.SkippedEmpty,
                null);
        }

        string trimmedText = dictatedText.Trim();
        if (trimmedText.Length > DictationHistoryLimits.MaximumTextLength)
        {
            return new DictationHistoryCaptureDecision(
                DictationHistoryCaptureOutcome.SkippedTooLong,
                null);
        }

        string? normalizedProfileId = NormalizeProfileId(profileId);

        return new DictationHistoryCaptureDecision(
            DictationHistoryCaptureOutcome.Recorded,
            new DictationHistoryEntry(
                id,
                createdUtc,
                trimmedText,
                normalizedProfileId));
    }

    private static string? NormalizeProfileId(string? profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId))
        {
            return null;
        }

        string trimmed = profileId.Trim();
        return trimmed.Length > DictationHistoryLimits.MaximumProfileIdLength
            ? trimmed[..DictationHistoryLimits.MaximumProfileIdLength]
            : trimmed;
    }
}
