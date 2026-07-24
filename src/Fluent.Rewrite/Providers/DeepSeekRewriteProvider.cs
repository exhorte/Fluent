namespace Fluent.Rewrite.Providers;

/// <summary>
/// Desktop-side DeepSeek provider. Like Gemini, it knows neither a provider endpoint,
/// model nor credential: it can only call the Fluent backend. The backend itself remains
/// default-deny until its server-only configuration is valid.
/// </summary>
public sealed class DeepSeekRewriteProvider : IRewriteProvider
{
    private readonly ICloudRewriteClient _client;

    public DeepSeekRewriteProvider(ICloudRewriteClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public RewriteProviderCapabilities Capabilities { get; } = new(
        RewriteProviderId.DeepSeek,
        "DeepSeek V4 Pro",
        IsEnabled: true,
        RequiresNetwork: true,
        RequiresAuthentication: true);

    public async Task<ProviderRewriteResult> RewriteAsync(
        ProviderRewriteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        CloudRewriteTransportResult transport = await _client.SendAsync(
            new CloudRewriteTransportRequest(request.Text, RewriteProviderId.DeepSeek),
            cancellationToken);

        if (transport.Succeeded && !string.IsNullOrWhiteSpace(transport.Text))
        {
            return ProviderRewriteResult.Success(RewriteProviderId.DeepSeek, transport.Text);
        }

        RewriteFailureReason reason = transport.FailureReason == RewriteFailureReason.None
            ? RewriteFailureReason.EmptyResponse
            : transport.FailureReason;
        return ProviderRewriteResult.Failure(RewriteProviderId.DeepSeek, reason);
    }
}
