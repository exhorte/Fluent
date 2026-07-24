using Fluent.Rewrite.Profiles;

namespace Fluent.Rewrite.Rewriting;

public sealed record RewriteRequest(
    string Text,
    RewriteProfile Profile);
