namespace Fluent.Core.History;

public static class DictationHistoryLimits
{
    /// <summary>Retention cap: only the newest entries are kept.</summary>
    public const int MaximumEntryCount = 500;

    /// <summary>Maximum number of characters retained for one dictation.</summary>
    public const int MaximumTextLength = 10000;

    public const int MaximumProfileIdLength = 64;
}
