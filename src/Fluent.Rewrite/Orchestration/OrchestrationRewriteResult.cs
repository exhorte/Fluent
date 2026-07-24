using Fluent.Rewrite.Providers;

namespace Fluent.Rewrite.Orchestration;

public sealed record OrchestrationRewriteResult(
    string Text,
    RewriteProviderId ProviderUsed,
    RewriteStatus Status,
    RewriteFailureReason FailureReason,
    bool FallbackUsed,
    TimeSpan Duration);
