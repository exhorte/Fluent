namespace Fluent.Rewrite.Providers;

public sealed record CloudRewriteTransportRequest(string Text, RewriteProviderId Provider);

public sealed record CloudRewriteTransportResult(
    bool Succeeded,
    string? Text,
    RewriteFailureReason FailureReason)
{
    public static CloudRewriteTransportResult Success(string text) =>
        new(true, text, RewriteFailureReason.None);

    public static CloudRewriteTransportResult Failure(RewriteFailureReason reason) =>
        new(false, null, reason);
}

/// <summary>
/// Transport seam to the Fluent backend. The Desktop implementation must call only
/// the Fluent backend, never a provider directly, and must hold no provider secret.
/// </summary>
public interface ICloudRewriteClient
{
    Task<CloudRewriteTransportResult> SendAsync(
        CloudRewriteTransportRequest request,
        CancellationToken cancellationToken = default);
}
