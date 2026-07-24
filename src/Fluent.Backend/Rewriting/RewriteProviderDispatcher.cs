namespace Fluent.Backend.Rewriting;

/// <summary>
/// Resolves a requested provider only when it exists and is enabled. A disabled provider
/// (DeepSeek in this phase) is never returned, so it is never dispatched.
/// </summary>
public sealed class RewriteProviderDispatcher
{
    private readonly IReadOnlyDictionary<string, IServerRewriteProvider> _providers;

    public RewriteProviderDispatcher(IEnumerable<IServerRewriteProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        _providers = providers.ToDictionary(
            provider => provider.Id,
            StringComparer.OrdinalIgnoreCase);
    }

    public bool TryResolve(string? providerId, out IServerRewriteProvider provider)
    {
        if (!string.IsNullOrWhiteSpace(providerId)
            && _providers.TryGetValue(providerId, out IServerRewriteProvider? candidate)
            && candidate.IsEnabled)
        {
            provider = candidate;
            return true;
        }

        provider = null!;
        return false;
    }
}
