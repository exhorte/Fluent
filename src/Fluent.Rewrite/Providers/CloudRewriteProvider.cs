namespace Fluent.Rewrite.Providers;

/// <summary>
/// Groups the cloud providers and resolves only enabled, catalog-known cloud providers.
/// A disabled provider (DeepSeek in this phase) is never resolved, so it is never called.
/// </summary>
public sealed class CloudRewriteProvider
{
    private readonly IReadOnlyDictionary<RewriteProviderId, IRewriteProvider> _providers;

    public CloudRewriteProvider(
        GeminiRewriteProvider gemini,
        DeepSeekRewriteProvider deepSeek)
    {
        ArgumentNullException.ThrowIfNull(gemini);
        ArgumentNullException.ThrowIfNull(deepSeek);

        _providers = new Dictionary<RewriteProviderId, IRewriteProvider>
        {
            [RewriteProviderId.Gemini] = gemini,
            [RewriteProviderId.DeepSeek] = deepSeek
        };
    }

    public bool TryResolveEnabled(RewriteProviderId id, out IRewriteProvider provider)
    {
        if (_providers.TryGetValue(id, out IRewriteProvider? candidate) && candidate.Capabilities.IsEnabled)
        {
            provider = candidate;
            return true;
        }

        provider = null!;
        return false;
    }
}
