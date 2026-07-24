namespace Fluent.Audio.Capture;

public sealed record RecordedAudio(float[] Samples, int SampleRate)
{
    public TimeSpan Duration => SampleRate <= 0
        ? TimeSpan.Zero
        : TimeSpan.FromSeconds((double)Samples.Length / SampleRate);
}
