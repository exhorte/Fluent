namespace Fluent.Rewrite.Rewriting;

public enum RewriteOutcome
{
    Applied,
    RawFallbackEmpty,
    RawFallbackValidationFailed,
    RawFallbackRewriterFailed
}
