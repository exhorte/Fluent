namespace Fluent.Rewrite.Providers;

public interface IRewriteProvider
{
    RewriteProviderCapabilities Capabilities { get; }

    Task<ProviderRewriteResult> RewriteAsync(
        ProviderRewriteRequest request,
        CancellationToken cancellationToken = default);
}
