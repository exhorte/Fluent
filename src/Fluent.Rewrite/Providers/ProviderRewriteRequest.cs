using Fluent.Rewrite.Profiles;

namespace Fluent.Rewrite.Providers;

/// <summary>
/// A provider-agnostic rewrite request. <see cref="Profile"/> is only meaningful
/// for the local provider; cloud providers ignore it.
/// </summary>
public sealed record ProviderRewriteRequest(
    string Text,
    RewriteProfile? Profile = null);
