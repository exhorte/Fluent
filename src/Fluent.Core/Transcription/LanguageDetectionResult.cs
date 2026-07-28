namespace Fluent.Core.Transcription;

public enum LanguageFallbackReason
{
    None,
    LowConfidence,
    LowMargin,
    TooShort,
    NoSpeech,
    UnsupportedDetectedLanguage,
    DetectionUnavailable,
    DetectionError,
    Cancelled,
    ModelIncompatible
}

public sealed record LanguageDetectionResult(
    TranscriptionLanguage ResolvedLanguage,
    double? Confidence,
    double? Margin,
    bool UsedFallback,
    LanguageFallbackReason? FallbackReason,
    TimeSpan DetectionDuration,
    TranscriptionLanguage? DetectedLanguage = null);
