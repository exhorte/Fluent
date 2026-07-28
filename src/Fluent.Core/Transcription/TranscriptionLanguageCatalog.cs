namespace Fluent.Core.Transcription;

/// <summary>
/// Conversions between modes, languages, Whisper codes, and persisted values.
/// Auto is never a concrete language — it is always resolved before transcription.
/// </summary>
public static class TranscriptionLanguageCatalog
{
    public static string ToWhisperCode(this TranscriptionLanguage language) => language switch
    {
        TranscriptionLanguage.French => "fr",
        TranscriptionLanguage.English => "en",
        _ => "fr"
    };

    public static string PersistMode(TranscriptionLanguageMode mode) => mode switch
    {
        TranscriptionLanguageMode.Auto => "auto",
        TranscriptionLanguageMode.French => "fr",
        TranscriptionLanguageMode.English => "en",
        _ => "auto"
    };

    public static TranscriptionLanguageMode ParseModeOrDefault(string? persisted) => persisted?.Trim().ToLowerInvariant() switch
    {
        "fr" => TranscriptionLanguageMode.French,
        "en" => TranscriptionLanguageMode.English,
        "auto" => TranscriptionLanguageMode.Auto,
        _ => TranscriptionLanguageMode.Auto
    };

    public static TranscriptionLanguage ResolveFromMode(
        TranscriptionLanguageMode mode) => mode switch
    {
        TranscriptionLanguageMode.French => TranscriptionLanguage.French,
        TranscriptionLanguageMode.English => TranscriptionLanguage.English,
        _ => TranscriptionLanguage.French // Auto not resolved yet — caller must detect
    };

    public static TranscriptionLanguage? ParseConcreteLanguageOrNull(string? code) => code?.Trim().ToLowerInvariant() switch
    {
        "fr" => TranscriptionLanguage.French,
        "en" => TranscriptionLanguage.English,
        _ => null
    };

    public static bool IsSupportedDetectionResult(string? whisperCode) =>
        whisperCode == "fr" || whisperCode == "en";
}
