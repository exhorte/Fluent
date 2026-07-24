using System.Buffers.Binary;
using Fluent.Audio.Capture;

namespace Fluent.Audio.Tests;

public sealed class InMemoryMicrophoneRecorderTests
{
    [Fact]
    public async Task StopAsync_returns_normalized_samples_without_writing_a_file()
    {
        FakeMicrophoneCapture capture = new(CreatePcmBytes(short.MinValue, 0, short.MaxValue));
        using InMemoryMicrophoneRecorder recorder = new(() => capture);

        recorder.Start();
        RecordedAudio audio = await recorder.StopAsync();

        Assert.False(recorder.IsRecording);
        Assert.Equal(InMemoryMicrophoneRecorder.SampleRate, audio.SampleRate);
        Assert.Equal(3, audio.Samples.Length);
        Assert.Equal(-1f, audio.Samples[0]);
        Assert.Equal(0f, audio.Samples[1]);
        Assert.InRange(audio.Samples[2], 0.9999f, 1f);
    }

    [Fact]
    public void Start_rejects_a_second_active_recording()
    {
        FakeMicrophoneCapture capture = new([]);
        using InMemoryMicrophoneRecorder recorder = new(() => capture);
        recorder.Start();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(recorder.Start);

        Assert.Contains("already active", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StopAsync_propagates_capture_failure_and_resets_state()
    {
        InvalidOperationException expected = new("Device disconnected.");
        FakeMicrophoneCapture capture = new([], expected);
        using InMemoryMicrophoneRecorder recorder = new(() => capture);
        recorder.Start();

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => recorder.StopAsync());

        Assert.Same(expected, actual);
        Assert.False(recorder.IsRecording);
    }

    [Fact]
    public void Start_wraps_device_failure_and_resets_state()
    {
        FakeMicrophoneCapture capture = new([], startException: new InvalidOperationException("No device."));
        using InMemoryMicrophoneRecorder recorder = new(() => capture);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(recorder.Start);

        Assert.Contains("could not be started", exception.Message, StringComparison.Ordinal);
        Assert.False(recorder.IsRecording);
    }

    [Fact]
    public async Task StopAsync_completes_with_an_error_when_capture_disposal_fails()
    {
        FakeMicrophoneCapture capture = new(
            [],
            disposeException: new InvalidOperationException("COM cleanup failed."));
        using InMemoryMicrophoneRecorder recorder = new(() => capture);
        recorder.Start();

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => recorder.StopAsync().WaitAsync(TimeSpan.FromSeconds(1)));

        Assert.Contains("could not be released", exception.Message, StringComparison.Ordinal);
        Assert.False(recorder.IsRecording);
    }

    private static byte[] CreatePcmBytes(params short[] samples)
    {
        byte[] bytes = new byte[samples.Length * sizeof(short)];
        for (int index = 0; index < samples.Length; index++)
        {
            BinaryPrimitives.WriteInt16LittleEndian(bytes.AsSpan(index * sizeof(short), sizeof(short)), samples[index]);
        }

        return bytes;
    }

    private sealed class FakeMicrophoneCapture(
        byte[] data,
        Exception? stopException = null,
        Exception? startException = null,
        Exception? disposeException = null) : IMicrophoneCapture
    {
        public event EventHandler<MicrophoneDataAvailableEventArgs>? DataAvailable;

        public event EventHandler<MicrophoneRecordingStoppedEventArgs>? RecordingStopped;

        public void StartRecording()
        {
            if (startException is not null)
            {
                throw startException;
            }

            if (data.Length > 0)
            {
                DataAvailable?.Invoke(this, new MicrophoneDataAvailableEventArgs(data, data.Length));
            }
        }

        public void StopRecording()
        {
            RecordingStopped?.Invoke(this, new MicrophoneRecordingStoppedEventArgs(stopException));
        }

        public void Dispose()
        {
            if (disposeException is not null)
            {
                throw disposeException;
            }
        }
    }
}
