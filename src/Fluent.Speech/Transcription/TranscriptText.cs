using System.Text;

namespace Fluent.Speech.Transcription;

internal static class TranscriptText
{
    public static string Combine(IEnumerable<string> segments)
    {
        StringBuilder text = new();
        foreach (string segment in segments)
        {
            text.Append(segment);
        }

        return text.ToString().Trim();
    }
}
