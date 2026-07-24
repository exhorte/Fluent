namespace Fluent.Speech.Transcription;

public interface ISpeechTranscriber : IDisposable
{
    Task PrepareAsync(
        IProgress<SpeechTranscriptionStage>? progress = null,
        CancellationToken cancellationToken = default);

    Task<string> TranscribeFrenchAsync(
        float[] samples,
        IProgress<SpeechTranscriptionStage>? progress = null,
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
