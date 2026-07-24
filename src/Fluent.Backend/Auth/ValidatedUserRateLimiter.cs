using System.Threading.RateLimiting;

namespace Fluent.Backend.Auth;

public interface IValidatedUserRateLimiter : IDisposable
{
    ValueTask<bool> TryAcquireAsync(string userId, CancellationToken cancellationToken);
}

/// <summary>
/// A no-queue quota partitioned solely by a JWT subject already verified by SupabaseJwtValidator.
/// It intentionally accepts no raw bearer value, request header or remote address.
/// </summary>
public sealed class ValidatedUserRateLimiter : IValidatedUserRateLimiter
{
    private readonly PartitionedRateLimiter<string> _limiter = PartitionedRateLimiter.Create<string, string>(
        userId => RateLimitPartition.GetFixedWindowLimiter(
            userId,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    public async ValueTask<bool> TryAcquireAsync(string userId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        using RateLimitLease lease = await _limiter.AcquireAsync(userId, 1, cancellationToken);
        return lease.IsAcquired;
    }

    public void Dispose() => _limiter.Dispose();
}
