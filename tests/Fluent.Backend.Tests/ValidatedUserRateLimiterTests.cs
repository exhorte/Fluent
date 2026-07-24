using Fluent.Backend.Auth;

namespace Fluent.Backend.Tests;

public sealed class ValidatedUserRateLimiterTests
{
    [Fact]
    public async Task Quota_is_partitioned_only_by_validated_user_id()
    {
        using ValidatedUserRateLimiter limiter = new();
        const string firstUser = "11111111-1111-1111-1111-111111111111";
        const string secondUser = "22222222-2222-2222-2222-222222222222";

        for (int attempt = 0; attempt < 30; attempt++)
        {
            Assert.True(await limiter.TryAcquireAsync(firstUser, CancellationToken.None));
        }

        Assert.False(await limiter.TryAcquireAsync(firstUser, CancellationToken.None));
        Assert.True(await limiter.TryAcquireAsync(secondUser, CancellationToken.None));
    }
}
