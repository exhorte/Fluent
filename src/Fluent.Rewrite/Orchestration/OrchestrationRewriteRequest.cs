using Fluent.Rewrite.Profiles;

namespace Fluent.Rewrite.Orchestration;

public sealed record OrchestrationRewriteRequest(
    string Text,
    RewriteProfile Profile,
    RewriteContext Context);
