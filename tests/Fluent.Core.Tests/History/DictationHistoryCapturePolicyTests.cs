using Fluent.Core.History;

namespace Fluent.Core.Tests.History;

public sealed class DictationHistoryCapturePolicyTests
{
    private static readonly Guid SampleId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly DateTimeOffset SampleTime =
        new(2026, 7, 23, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Disabled_history_records_nothing()
    {
        DictationHistoryCaptureDecision decision = DictationHistoryCapturePolicy.Decide(
            DictationHistoryPreferences.Disabled,
            "Bonjour le monde",
            "professional",
            SampleId,
            SampleTime);

        Assert.Equal(DictationHistoryCaptureOutcome.SkippedDisabled, decision.Outcome);
        Assert.False(decision.ShouldRecord);
        Assert.Null(decision.Entry);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n ")]
    public void Empty_text_is_not_recorded_when_enabled(string? text)
    {
        DictationHistoryCaptureDecision decision = DictationHistoryCapturePolicy.Decide(
            new DictationHistoryPreferences(true),
            text,
            null,
            SampleId,
            SampleTime);

        Assert.Equal(DictationHistoryCaptureOutcome.SkippedEmpty, decision.Outcome);
        Assert.Null(decision.Entry);
    }

    [Fact]
    public void Text_over_the_limit_is_skipped()
    {
        string tooLong = new('a', DictationHistoryLimits.MaximumTextLength + 1);

        DictationHistoryCaptureDecision decision = DictationHistoryCapturePolicy.Decide(
            new DictationHistoryPreferences(true),
            tooLong,
            null,
            SampleId,
            SampleTime);

        Assert.Equal(DictationHistoryCaptureOutcome.SkippedTooLong, decision.Outcome);
        Assert.Null(decision.Entry);
    }

    [Fact]
    public void Enabled_history_records_trimmed_entry()
    {
        DictationHistoryCaptureDecision decision = DictationHistoryCapturePolicy.Decide(
            new DictationHistoryPreferences(true),
            "  Bonjour le monde  ",
            "  professional  ",
            SampleId,
            SampleTime);

        Assert.True(decision.ShouldRecord);
        DictationHistoryEntry entry = Assert.IsType<DictationHistoryEntry>(decision.Entry);
        Assert.Equal(SampleId, entry.Id);
        Assert.Equal(SampleTime, entry.CreatedUtc);
        Assert.Equal("Bonjour le monde", entry.Text);
        Assert.Equal("professional", entry.ProfileId);
    }

    [Fact]
    public void Whitespace_profile_becomes_null()
    {
        DictationHistoryCaptureDecision decision = DictationHistoryCapturePolicy.Decide(
            new DictationHistoryPreferences(true),
            "texte",
            "   ",
            SampleId,
            SampleTime);

        Assert.True(decision.ShouldRecord);
        Assert.Null(decision.Entry!.ProfileId);
    }

    [Fact]
    public void Oversized_profile_id_is_truncated()
    {
        string longProfile = new('p', DictationHistoryLimits.MaximumProfileIdLength + 50);

        DictationHistoryCaptureDecision decision = DictationHistoryCapturePolicy.Decide(
            new DictationHistoryPreferences(true),
            "texte",
            longProfile,
            SampleId,
            SampleTime);

        Assert.True(decision.ShouldRecord);
        Assert.Equal(
            DictationHistoryLimits.MaximumProfileIdLength,
            decision.Entry!.ProfileId!.Length);
    }
}
