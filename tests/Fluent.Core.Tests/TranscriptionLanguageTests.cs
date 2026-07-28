using Fluent.Core.Settings;
using Fluent.Core.Transcription;

namespace Fluent.Core.Tests;

public sealed class TranscriptionLanguageTests
{
    // ── Mode defaults ───────────────────────────────────────────
    [Fact]
    public void Default_is_auto()
    {
        Assert.Equal("auto", AppSettingsLimits.DefaultTranscriptionLanguage);
    }

    [Fact]
    public void ParseModeOrDefault_returns_auto_for_null()
    {
        Assert.Equal(
            TranscriptionLanguageMode.Auto,
            TranscriptionLanguageCatalog.ParseModeOrDefault(null));
    }

    [Fact]
    public void ParseModeOrDefault_returns_auto_for_unknown()
    {
        Assert.Equal(
            TranscriptionLanguageMode.Auto,
            TranscriptionLanguageCatalog.ParseModeOrDefault("de"));
    }

    [Fact]
    public void ParseModeOrDefault_returns_french_for_fr()
    {
        Assert.Equal(
            TranscriptionLanguageMode.French,
            TranscriptionLanguageCatalog.ParseModeOrDefault("fr"));
    }

    [Fact]
    public void ParseModeOrDefault_returns_english_for_en()
    {
        Assert.Equal(
            TranscriptionLanguageMode.English,
            TranscriptionLanguageCatalog.ParseModeOrDefault("en"));
    }

    [Fact]
    public void NormalizeTranscriptionLanguage_returns_auto_for_invalid()
    {
        Assert.Equal("auto", AppSettingsLimits.NormalizeTranscriptionLanguage(null));
    }

    [Fact]
    public void NormalizeTranscriptionLanguage_returns_auto_for_unknown()
    {
        Assert.Equal("auto", AppSettingsLimits.NormalizeTranscriptionLanguage("de"));
    }

    // ── Persistence ────────────────────────────────────────────
    [Fact]
    public void PersistMode_returns_auto_for_auto()
    {
        Assert.Equal("auto", TranscriptionLanguageCatalog.PersistMode(TranscriptionLanguageMode.Auto));
    }

    [Fact]
    public void PersistMode_returns_fr_for_french()
    {
        Assert.Equal("fr", TranscriptionLanguageCatalog.PersistMode(TranscriptionLanguageMode.French));
    }

    [Fact]
    public void PersistMode_returns_en_for_english()
    {
        Assert.Equal("en", TranscriptionLanguageCatalog.PersistMode(TranscriptionLanguageMode.English));
    }

    // ── Whisper codes ──────────────────────────────────────────
    [Fact]
    public void French_has_whisper_code_fr()
    {
        Assert.Equal("fr", TranscriptionLanguage.French.ToWhisperCode());
    }

    [Fact]
    public void English_has_whisper_code_en()
    {
        Assert.Equal("en", TranscriptionLanguage.English.ToWhisperCode());
    }

    // ── Concrete language parsing ──────────────────────────────
    [Fact]
    public void ParseConcreteLanguageOrNull_returns_null_for_auto()
    {
        Assert.Null(TranscriptionLanguageCatalog.ParseConcreteLanguageOrNull("auto"));
    }

    [Fact]
    public void ParseConcreteLanguageOrNull_returns_french_for_fr()
    {
        Assert.Equal(
            TranscriptionLanguage.French,
            TranscriptionLanguageCatalog.ParseConcreteLanguageOrNull("fr"));
    }

    [Fact]
    public void ParseConcreteLanguageOrNull_returns_english_for_en()
    {
        Assert.Equal(
            TranscriptionLanguage.English,
            TranscriptionLanguageCatalog.ParseConcreteLanguageOrNull("en"));
    }

    [Fact]
    public void ParseConcreteLanguageOrNull_returns_null_for_unknown()
    {
        Assert.Null(TranscriptionLanguageCatalog.ParseConcreteLanguageOrNull("de"));
    }

    // ── Resolve from mode ──────────────────────────────────────
    [Fact]
    public void ResolveFromMode_french_returns_french()
    {
        Assert.Equal(
            TranscriptionLanguage.French,
            TranscriptionLanguageCatalog.ResolveFromMode(TranscriptionLanguageMode.French));
    }

    [Fact]
    public void ResolveFromMode_english_returns_english()
    {
        Assert.Equal(
            TranscriptionLanguage.English,
            TranscriptionLanguageCatalog.ResolveFromMode(TranscriptionLanguageMode.English));
    }

    [Fact]
    public void ResolveFromMode_auto_returns_french_as_default()
    {
        Assert.Equal(
            TranscriptionLanguage.French,
            TranscriptionLanguageCatalog.ResolveFromMode(TranscriptionLanguageMode.Auto));
    }

    // ── Supported detection result ────────────────────────────
    [Fact]
    public void IsSupportedDetectionResult_accepts_fr()
    {
        Assert.True(TranscriptionLanguageCatalog.IsSupportedDetectionResult("fr"));
    }

    [Fact]
    public void IsSupportedDetectionResult_accepts_en()
    {
        Assert.True(TranscriptionLanguageCatalog.IsSupportedDetectionResult("en"));
    }

    [Fact]
    public void IsSupportedDetectionResult_rejects_de()
    {
        Assert.False(TranscriptionLanguageCatalog.IsSupportedDetectionResult("de"));
    }

    // ── Default preferences ────────────────────────────────────
    [Fact]
    public void AppPreferences_default_uses_auto()
    {
        Assert.Equal("auto", AppPreferences.Default.TranscriptionLanguageId);
    }

    [Fact]
    public void SupportedModes_include_auto_fr_en()
    {
        Assert.Contains("auto", AppSettingsLimits.SupportedTranscriptionLanguageModes);
        Assert.Contains("fr", AppSettingsLimits.SupportedTranscriptionLanguageModes);
        Assert.Contains("en", AppSettingsLimits.SupportedTranscriptionLanguageModes);
    }
}
