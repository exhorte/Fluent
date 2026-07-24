namespace Fluent.Rewrite.Providers;

public sealed record ProviderRewriteResult(
    RewriteProviderId Provider,
    bool Succeeded,
    string Text,
    RewriteFailureReason FailureReason)
{
    public static ProviderRewriteResult Success(RewriteProviderId provider, string text) =>
        new(provider, true, text, RewriteFailureReason.None);

    public static ProviderRewriteResult Failure(RewriteProviderId provider, RewriteFailureReason reason) =>
        new(provider, false, string.Empty, reason);
}
