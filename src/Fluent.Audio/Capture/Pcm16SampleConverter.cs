using System.Buffers.Binary;

namespace Fluent.Audio.Capture;

internal static class Pcm16SampleConverter
{
    public static float[] Convert(ReadOnlySpan<byte> pcmBytes)
    {
        int sampleCount = pcmBytes.Length / sizeof(short);
        float[] samples = new float[sampleCount];

        for (int index = 0; index < sampleCount; index++)
        {
            short value = BinaryPrimitives.ReadInt16LittleEndian(pcmBytes.Slice(index * sizeof(short), sizeof(short)));
            samples[index] = value / 32768f;
        }

        return samples;
    }
}
