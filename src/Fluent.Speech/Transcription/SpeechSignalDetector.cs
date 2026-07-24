namespace Fluent.Speech.Transcription;

internal static class SpeechSignalDetector
{
    private const int FrameSize = 320;
    private const int MinimumConsecutiveFrames = 3;
    private const float MinimumPeak = 0.001f;
    private const double MinimumRootMeanSquare = 0.0002;

    public static bool ContainsSpeech(ReadOnlySpan<float> samples)
    {
        int consecutiveSpeechFrames = 0;

        for (int offset = 0; offset < samples.Length; offset += FrameSize)
        {
            ReadOnlySpan<float> frame = samples.Slice(offset, Math.Min(FrameSize, samples.Length - offset));
            double sumOfSquares = 0;
            float peak = 0;

            foreach (float sample in frame)
            {
                float absolute = Math.Abs(sample);
                peak = Math.Max(peak, absolute);
                sumOfSquares += sample * sample;
            }

            double rootMeanSquare = Math.Sqrt(sumOfSquares / frame.Length);
            if (peak >= MinimumPeak && rootMeanSquare >= MinimumRootMeanSquare)
            {
                consecutiveSpeechFrames++;
                if (consecutiveSpeechFrames >= MinimumConsecutiveFrames)
                {
                    return true;
                }
            }
            else
            {
                consecutiveSpeechFrames = 0;
            }
        }

        return false;
    }
}
