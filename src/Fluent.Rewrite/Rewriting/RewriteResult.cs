namespace Fluent.Rewrite.Rewriting;

public sealed record RewriteResult(
    string Text,
    RewriteOutcome Outcome)
{
    public bool WasApplied => Outcome == RewriteOutcome.Applied;
}
