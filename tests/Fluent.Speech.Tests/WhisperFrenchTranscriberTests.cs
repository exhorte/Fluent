using Fluent.Speech.Transcription;

namespace Fluent.Speech.Tests;

public sealed class WhisperFrenchTranscriberTests
{
    [Fact]
    public async Task TranscribeAsync_rejects_calls_after_disposal_without_loading_a_model()
    {
        WhisperFrenchTranscriber transcriber = new(Path.Combine(Path.GetTempPath(), $"unused-{Guid.NewGuid():N}.bin"));
        transcriber.Dispose();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => transcriber.TranscribeAsync([], "fr"));
    }

    [Fact]
    public async Task TranscribeAsync_rejects_silence_without_creating_or_loading_a_model()
    {
        string root = Path.Combine(Path.GetTempPath(), $"fluent-silence-{Guid.NewGuid():N}");
        string modelPath = Path.Combine(root, "model.bin");
        using WhisperFrenchTranscriber transcriber = new(modelPath);

        string result = await transcriber.TranscribeAsync(new float[16_000], "fr");

        Assert.Equal(string.Empty, result);
        Assert.False(Directory.Exists(root));
    }

    [Fact]
    public async Task TranscribeAsync_accepts_english_language_code()
    {
        string root = Path.Combine(Path.GetTempPath(), $"fluent-en-{Guid.NewGuid():N}");
        string modelPath = Path.Combine(root, "model.bin");
        using WhisperFrenchTranscriber transcriber = new(modelPath);

        string result = await transcriber.TranscribeAsync(new float[16_000], "en");

        Assert.Equal(string.Empty, result);
    }
}
