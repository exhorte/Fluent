namespace Fluent.Rewrite.Providers;

/// <summary>
/// Desktop-side Gemini provider. It never calls Gemini directly and never knows the
/// model; it forwards the text to the Fluent backend through <see cref="ICloudRewriteClient"/>.
/// </summary>
public sealed class GeminiRewriteProvider : IRewriteProvider
{
    private readonly ICloudRewriteClient _client;

    public GeminiRewriteProvider(ICloudRewriteClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public RewriteProviderCapabilities Capabilities { get; } = new(
        RewriteProviderId.Gemini,
        "Gemini",
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
            new CloudRewriteTransportRequest(request.Text, RewriteProviderId.Gemini),
            cancellationToken);

        if (transport.Succeeded && !string.IsNullOrWhiteSpace(transport.Text))
        {
            return ProviderRewriteResult.Success(RewriteProviderId.Gemini, transport.Text);
        }

        RewriteFailureReason reason = transport.FailureReason == RewriteFailureReason.None
            ? RewriteFailureReason.EmptyResponse
            : transport.FailureReason;
        return ProviderRewriteResult.Failure(RewriteProviderId.Gemini, reason);
    }
}
