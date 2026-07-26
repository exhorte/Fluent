namespace Fluent.Speech.Transcription;

public interface ISpeechTranscriber : IDisposable
{
    Task PrepareAsync(
        IProgress<SpeechTranscriptionStage>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>Transcribes PCM float samples using the given Whisper language code (e.g. "fr", "en").</summary>
    Task<string> TranscribeAsync(
        float[] samples,
        string languageCode,
        IProgress<SpeechTranscriptionStage>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects the language of the given audio samples, limited to "fr" and "en".
    /// Returns (languageCode, probability). Returns (null, 0) on failure.
    /// </summary>
    Task<(string? LanguageCode, float Probability)> DetectLanguageAsync(
        float[] samples,
        CancellationToken cancellationToken = default);
}

public enum SpeechTranscriptionStage
{
    PreparingModel,
    DownloadingModel,
    LoadingModel,
    WarmingModel,
    Transcribing
}
