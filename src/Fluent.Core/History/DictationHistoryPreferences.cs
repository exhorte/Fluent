namespace Fluent.Core.History;

/// <summary>
/// Local, opt-in preferences for dictation history. Disabled by default:
/// nothing is recorded until the user explicitly turns history on.
/// </summary>
public sealed record DictationHistoryPreferences(bool IsEnabled)
{
    public static DictationHistoryPreferences Disabled { get; } = new(false);
}
