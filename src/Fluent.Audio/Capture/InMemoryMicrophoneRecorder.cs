namespace Fluent.Audio.Capture;

public sealed class InMemoryMicrophoneRecorder : IAudioRecorder
{
    public const int SampleRate = 16_000;

    private readonly object _sync = new();
    private readonly Func<IMicrophoneCapture> _captureFactory;
    private IMicrophoneCapture? _capture;
    private MemoryStream? _buffer;
    private TaskCompletionSource<RecordedAudio>? _completion;
    private bool _stopRequested;
    private bool _disposed;

    public InMemoryMicrophoneRecorder()
        : this(static () => new WasapiMicrophoneCapture())
    {
    }

    internal InMemoryMicrophoneRecorder(Func<IMicrophoneCapture> captureFactory)
    {
        _captureFactory = captureFactory;
    }

    public bool IsRecording
    {
        get
        {
            lock (_sync)
            {
                return _capture is not null;
            }
        }
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        IMicrophoneCapture capture;
        lock (_sync)
        {
            if (_capture is not null)
            {
                throw new InvalidOperationException("A microphone recording is already active.");
            }

            capture = _captureFactory();
            capture.DataAvailable += OnDataAvailable;
            capture.RecordingStopped += OnRecordingStopped;
            _capture = capture;
            _buffer = new MemoryStream();
            _completion = new TaskCompletionSource<RecordedAudio>(TaskCreationOptions.RunContinuationsAsynchronously);
            _stopRequested = false;
        }

        try
        {
            capture.StartRecording();
        }
        catch (Exception ex)
        {
            CleanupFailedStart(capture);
            throw new InvalidOperationException("The default microphone could not be started.", ex);
        }
    }

    public async Task<RecordedAudio> StopAsync(CancellationToken cancellationToken = default)
    {
        IMicrophoneCapture capture;
        Task<RecordedAudio> completionTask;
        bool shouldRequestStop;

        lock (_sync)
        {
            capture = _capture ?? throw new InvalidOperationException("No microphone recording is active.");
            completionTask = _completion!.Task;
            shouldRequestStop = !_stopRequested;
            _stopRequested = true;
        }

        if (shouldRequestStop)
        {
            try
            {
                capture.StopRecording();
            }
            catch (Exception ex)
            {
                CompleteCapture(capture, new InvalidOperationException("The microphone recording could not be stopped.", ex));
            }
        }

        return await completionTask.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        IMicrophoneCapture? capture;
        MemoryStream? buffer;
        TaskCompletionSource<RecordedAudio>? completion;

        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            capture = _capture;
            buffer = _buffer;
            completion = _completion;
            ResetState();
        }

        if (capture is not null)
        {
            _ = ReleaseCapture(capture);
        }

        buffer?.Dispose();
        completion?.TrySetCanceled();
    }

    private void OnDataAvailable(object? sender, MicrophoneDataAvailableEventArgs args)
    {
        lock (_sync)
        {
            if (ReferenceEquals(sender, _capture))
            {
                _buffer?.Write(args.Buffer, 0, args.BytesRecorded);
            }
        }
    }

    private void OnRecordingStopped(object? sender, MicrophoneRecordingStoppedEventArgs args)
    {
        if (sender is IMicrophoneCapture capture)
        {
            CompleteCapture(capture, args.Exception);
        }
    }

    private void CompleteCapture(IMicrophoneCapture capture, Exception? exception)
    {
        MemoryStream? buffer;
        TaskCompletionSource<RecordedAudio>? completion;

        lock (_sync)
        {
            if (!ReferenceEquals(capture, _capture))
            {
                return;
            }

            buffer = _buffer;
            completion = _completion;
            ResetState();
        }

        Exception? cleanupException = ReleaseCapture(capture);
        exception ??= cleanupException;

        if (exception is not null)
        {
            buffer?.Dispose();
            completion?.TrySetException(exception);
            return;
        }

        byte[] pcmBytes = buffer?.ToArray() ?? [];
        buffer?.Dispose();
        completion?.TrySetResult(new RecordedAudio(Pcm16SampleConverter.Convert(pcmBytes), SampleRate));
    }

    private void CleanupFailedStart(IMicrophoneCapture capture)
    {
        MemoryStream? buffer;
        TaskCompletionSource<RecordedAudio>? completion;

        lock (_sync)
        {
            if (!ReferenceEquals(capture, _capture))
            {
                return;
            }

            buffer = _buffer;
            completion = _completion;
            ResetState();
        }

        _ = ReleaseCapture(capture);
        buffer?.Dispose();
        completion?.TrySetCanceled();
    }

    private Exception? ReleaseCapture(IMicrophoneCapture capture)
    {
        Exception? failure = null;

        try
        {
            capture.DataAvailable -= OnDataAvailable;
            capture.RecordingStopped -= OnRecordingStopped;
        }
        catch (Exception ex)
        {
            failure = ex;
        }

        try
        {
            capture.Dispose();
        }
        catch (Exception ex)
        {
            failure ??= ex;
        }

        return failure is null
            ? null
            : new InvalidOperationException("The microphone capture resources could not be released.", failure);
    }

    private void ResetState()
    {
        _capture = null;
        _buffer = null;
        _completion = null;
        _stopRequested = false;
    }
}
