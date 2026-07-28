namespace Fluent.Core.Settings;

public static class AppSettingsLimits
{
    public const int MaximumProfileIdLength = 64;

    public const string DefaultLanguage = "en";

    public static IReadOnlyList<string> SupportedLanguages { get; } = ["en", "fr"];

    /// <summary>
    /// Returns a supported language code (<c>en</c> or <c>fr</c>), falling back to
    /// the default when the value is unknown.
    /// </summary>
    public static string NormalizeLanguage(string? language)
    {
        string? normalized = language?.Trim().ToLowerInvariant();
        return normalized == "fr" ? "fr" : DefaultLanguage;
    }

    // ── Transcription language mode ─────────────────────────────────────

    public const string DefaultTranscriptionLanguage = "auto";

    public static IReadOnlyList<string> SupportedTranscriptionLanguageModes { get; } =
        ["auto", "fr", "en"];

    /// <summary>
    /// Normalizes a transcription language mode id.
    /// Accepts "auto", "fr", "en". Falls back to "auto".
    /// </summary>
    public static string NormalizeTranscriptionLanguage(string? language)
    {
        string? normalized = language?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "fr" => "fr",
            "en" => "en",
            "auto" => "auto",
            _ => DefaultTranscriptionLanguage
        };
    }
}
