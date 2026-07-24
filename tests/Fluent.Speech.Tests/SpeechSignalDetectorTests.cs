using Fluent.Speech.Transcription;

namespace Fluent.Speech.Tests;

public sealed class SpeechSignalDetectorTests
{
    [Fact]
    public void ContainsSpeech_rejects_silence()
    {
        Assert.False(SpeechSignalDetector.ContainsSpeech(new float[16_000]));
    }

    [Fact]
    public void ContainsSpeech_rejects_very_low_level_noise()
    {
        Assert.False(SpeechSignalDetector.ContainsSpeech(CreateSignal(16_000, 0.0001f)));
    }

    [Fact]
    public void ContainsSpeech_rejects_an_isolated_click()
    {
        float[] samples = new float[16_000];
        samples[1_000] = 0.8f;

        Assert.False(SpeechSignalDetector.ContainsSpeech(samples));
    }

    [Fact]
    public void ContainsSpeech_accepts_sustained_voice_level_signal()
    {
        Assert.True(SpeechSignalDetector.ContainsSpeech(CreateSignal(1_280, 0.02f)));
    }

    private static float[] CreateSignal(int length, float amplitude)
    {
        float[] samples = new float[length];
        Array.Fill(samples, amplitude);
        return samples;
    }
}
