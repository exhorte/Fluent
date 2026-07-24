namespace Fluent.Audio.Capture;

public interface IAudioRecorder : IDisposable
{
    bool IsRecording { get; }

    void Start();

    Task<RecordedAudio> StopAsync(CancellationToken cancellationToken = default);
}
