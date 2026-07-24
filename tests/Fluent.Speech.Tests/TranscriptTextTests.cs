using Fluent.Speech.Transcription;

namespace Fluent.Speech.Tests;

public sealed class TranscriptTextTests
{
    [Fact]
    public void Combine_preserves_segment_text_and_trims_outer_whitespace()
    {
        string result = TranscriptText.Combine(["  Bonjour", " le monde.  "]);

        Assert.Equal("Bonjour le monde.", result);
    }

    [Fact]
    public void Combine_returns_empty_text_when_there_are_no_segments()
    {
        Assert.Equal(string.Empty, TranscriptText.Combine([]));
    }
}
