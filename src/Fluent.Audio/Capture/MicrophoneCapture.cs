using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Fluent.Audio.Capture;

internal interface IMicrophoneCapture : IDisposable
{
    event EventHandler<MicrophoneDataAvailableEventArgs>? DataAvailable;

    event EventHandler<MicrophoneRecordingStoppedEventArgs>? RecordingStopped;

    void StartRecording();

    void StopRecording();
}

internal sealed class MicrophoneDataAvailableEventArgs(byte[] buffer, int bytesRecorded) : EventArgs
{
    public byte[] Buffer { get; } = buffer;

    public int BytesRecorded { get; } = bytesRecorded;
}

internal sealed class MicrophoneRecordingStoppedEventArgs(Exception? exception) : EventArgs
{
    public Exception? Exception { get; } = exception;
}

internal sealed class WasapiMicrophoneCapture : IMicrophoneCapture
{
    private readonly WasapiCapture _capture = new()
    {
        WaveFormat = new WaveFormat(InMemoryMicrophoneRecorder.SampleRate, 16, 1)
    };

    public WasapiMicrophoneCapture()
    {
        _capture.DataAvailable += OnDataAvailable;
        _capture.RecordingStopped += OnRecordingStopped;
    }

    public event EventHandler<MicrophoneDataAvailableEventArgs>? DataAvailable;

    public event EventHandler<MicrophoneRecordingStoppedEventArgs>? RecordingStopped;

    public void StartRecording() => _capture.StartRecording();

    public void StopRecording() => _capture.StopRecording();

    public void Dispose()
    {
        _capture.DataAvailable -= OnDataAvailable;
        _capture.RecordingStopped -= OnRecordingStopped;
        _capture.Dispose();
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs args)
    {
        DataAvailable?.Invoke(this, new MicrophoneDataAvailableEventArgs(args.Buffer, args.BytesRecorded));
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs args)
    {
        RecordingStopped?.Invoke(this, new MicrophoneRecordingStoppedEventArgs(args.Exception));
    }
}
