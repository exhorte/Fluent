using Fluent.Rewrite.Profiles;

namespace Fluent.Rewrite.Providers;

/// <summary>
/// Adapts the accepted local <see cref="SafeProfileRewriteService"/> to the provider
/// contract. Local behavior is unchanged: the service already falls back to the exact
/// source transcript on any unsafe or failed rewrite.
/// </summary>
public sealed class LocalRewriteProvider : IRewriteProvider
{
    private readonly SafeProfileRewriteService _service;

    public LocalRewriteProvider(SafeProfileRewriteService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public RewriteProviderCapabilities Capabilities { get; } = new(
        RewriteProviderId.Local,
        "Local",
        IsEnabled: true,
        RequiresNetwork: false,
        RequiresAuthentication: false);

    public async Task<ProviderRewriteResult> RewriteAsync(
        ProviderRewriteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        RewriteProfile profile = request.Profile ?? RewriteProfiles.Default;
        Rewriting.RewriteResult result = await _service.RewriteAsync(
            request.Text,
            profile,
            request.TranscriptionLanguage,
            cancellationToken);

        return ProviderRewriteResult.Success(RewriteProviderId.Local, result.Text);
    }
}
