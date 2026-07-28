namespace Fluent.Core.Settings;

/// <summary>
/// Local, versioned, reversible application preferences. No secret and no Cloud
/// consent is ever stored here — those remain session-only by design.
/// </summary>
public sealed record AppPreferences(
    string? PreferredProfileId,
    string Language,
    string TranscriptionLanguageId)
{
    public static AppPreferences Default { get; } =
        new(null,
            AppSettingsLimits.DefaultLanguage,
            AppSettingsLimits.DefaultTranscriptionLanguage);
}
