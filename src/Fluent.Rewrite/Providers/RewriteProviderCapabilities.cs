namespace Fluent.Rewrite.Providers;

public sealed record RewriteProviderCapabilities(
    RewriteProviderId Id,
    string DisplayName,
    bool IsEnabled,
    bool RequiresNetwork,
    bool RequiresAuthentication);
