using Whisper.net;
using Whisper.net.Ggml;

namespace Fluent.Speech.Transcription;

public sealed class WhisperFrenchTranscriber : ISpeechTranscriber
{
    private const string ModelEnvironmentVariable = "FLUENT_WHISPER_MODEL_PATH";
    private const string DefaultModelFileName = "ggml-base-q8_0.bin";
    private const int TranscriptionThreadCount = 8;
    private static readonly float[] WarmupSamples = new float[16_000];

    private readonly SemaphoreSlim _modelGate = new(1, 1);
    private readonly SemaphoreSlim _transcriptionGate = new(1, 1);
    private readonly object _lifecycleSync = new();
    private readonly string _modelPath;
    private WhisperFactory? _factory;
    private WhisperProcessor? _processor;
    private int _activeOperations;
    private bool _disposeRequested;

    public WhisperFrenchTranscriber()
        : this(ResolveModelPath())
    {
    }

    internal WhisperFrenchTranscriber(string modelPath)
    {
        _modelPath = modelPath;
    }

    public async Task PrepareAsync(
        IProgress<SpeechTranscriptionStage>? progress = null,
        CancellationToken cancellationToken = default)
    {
        EnterOperation();
        try
        {
            _ = await GetProcessorAsync(progress, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ExitOperation();
        }
    }

    public async Task<string> TranscribeFrenchAsync(
        float[] samples,
        IProgress<SpeechTranscriptionStage>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(samples);
        ThrowIfDisposalRequested();

        if (samples.Length == 0)
        {
            return string.Empty;
        }

        if (!SpeechSignalDetector.ContainsSpeech(samples))
        {
            return string.Empty;
        }

        EnterOperation();
        bool gateAcquired = false;
        try
        {
            await _transcriptionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            gateAcquired = true;
            WhisperProcessor processor = await GetProcessorAsync(progress, cancellationToken).ConfigureAwait(false);
            progress?.Report(SpeechTranscriptionStage.Transcribing);

            List<string> segments = [];
            await foreach (SegmentData segment in processor.ProcessAsync(samples, cancellationToken).ConfigureAwait(false))
            {
                segments.Add(segment.Text);
            }

            return TranscriptText.Combine(segments);
        }
        finally
        {
            if (gateAcquired)
            {
                _transcriptionGate.Release();
            }

            ExitOperation();
        }
    }

    public void Dispose()
    {
        WhisperFactory? factoryToDispose = null;
        WhisperProcessor? processorToDispose = null;
        lock (_lifecycleSync)
        {
            if (_disposeRequested)
            {
                return;
            }

            _disposeRequested = true;
            if (_activeOperations == 0)
            {
                processorToDispose = _processor;
                _processor = null;
                factoryToDispose = _factory;
                _factory = null;
            }
        }

        DisposeResources(processorToDispose, factoryToDispose);
    }

    private async Task<WhisperProcessor> GetProcessorAsync(
        IProgress<SpeechTranscriptionStage>? progress,
        CancellationToken cancellationToken)
    {
        if (_processor is not null)
        {
            return _processor;
        }

        await _modelGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_processor is not null)
            {
                return _processor;
            }

            if (_factory is null)
            {
                progress?.Report(SpeechTranscriptionStage.PreparingModel);
                await EnsureModelAsync(progress, cancellationToken).ConfigureAwait(false);
                progress?.Report(SpeechTranscriptionStage.LoadingModel);
                _factory = await Task.Run(
                    () => WhisperFactory.FromPath(_modelPath),
                    cancellationToken).ConfigureAwait(false);
            }

            WhisperProcessor processor = _factory.CreateBuilder()
                .WithLanguage("fr")
                .WithThreads(TranscriptionThreadCount)
                .Build();

            progress?.Report(SpeechTranscriptionStage.WarmingModel);
            try
            {
                _ = await Task.Run(
                    () => processor.DetectLanguage(WarmupSamples),
                    cancellationToken).ConfigureAwait(false);
                _processor = processor;
                return _processor;
            }
            catch
            {
                processor.Dispose();
                throw;
            }
        }
        finally
        {
            _modelGate.Release();
        }
    }

    private async Task EnsureModelAsync(
        IProgress<SpeechTranscriptionStage>? progress,
        CancellationToken cancellationToken)
    {
        if (File.Exists(_modelPath))
        {
            return;
        }

        string? directory = Path.GetDirectoryName(_modelPath);
        if (string.IsNullOrWhiteSpace(directory))
        {
            throw new InvalidOperationException("The Whisper model path has no parent directory.");
        }

        Directory.CreateDirectory(directory);
        string temporaryPath = $"{_modelPath}.{Guid.NewGuid():N}.tmp";
        progress?.Report(SpeechTranscriptionStage.DownloadingModel);

        try
        {
            using Stream source = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(
                GgmlType.Base,
                QuantizationType.Q8_0,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            await using FileStream destination = new(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81_920,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
            await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
            destination.Close();
            File.Move(temporaryPath, _modelPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static string ResolveModelPath()
    {
        string? configuredPath = Environment.GetEnvironmentVariable(ModelEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Fluent",
            "Models",
            DefaultModelFileName);
    }

    private void EnterOperation()
    {
        lock (_lifecycleSync)
        {
            ObjectDisposedException.ThrowIf(_disposeRequested, this);
            _activeOperations++;
        }
    }

    private void ThrowIfDisposalRequested()
    {
        lock (_lifecycleSync)
        {
            ObjectDisposedException.ThrowIf(_disposeRequested, this);
        }
    }

    private void ExitOperation()
    {
        WhisperFactory? factoryToDispose = null;
        WhisperProcessor? processorToDispose = null;
        lock (_lifecycleSync)
        {
            _activeOperations--;
            if (_disposeRequested && _activeOperations == 0)
            {
                processorToDispose = _processor;
                _processor = null;
                factoryToDispose = _factory;
                _factory = null;
            }
        }

        DisposeResources(processorToDispose, factoryToDispose);
    }

    private static void DisposeResources(WhisperProcessor? processor, WhisperFactory? factory)
    {
        try
        {
            processor?.Dispose();
        }
        finally
        {
            factory?.Dispose();
        }
    }
}
