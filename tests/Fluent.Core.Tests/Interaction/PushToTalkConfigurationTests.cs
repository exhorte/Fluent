using Fluent.Core.Interaction;

namespace Fluent.Core.Tests.Interaction;

public sealed class PushToTalkConfigurationTests
{
    [Fact]
    public void MinimumHoldDuration_is_200ms()
    {
        Assert.Equal(200, PushToTalkConfiguration.MinimumHoldDuration.TotalMilliseconds);
    }

    [Fact]
    public void MinimumRecordingDuration_is_250ms()
    {
        Assert.Equal(250, PushToTalkConfiguration.MinimumRecordingDuration.TotalMilliseconds);
    }

    [Fact]
    public void MaximumRecordingDuration_is_5_minutes()
    {
        Assert.Equal(5, PushToTalkConfiguration.MaximumRecordingDuration.TotalMinutes);
    }

    [Fact]
    public void StopProcessingDelay_is_50ms()
    {
        Assert.Equal(50, PushToTalkConfiguration.StopProcessingDelay.TotalMilliseconds);
    }
}
