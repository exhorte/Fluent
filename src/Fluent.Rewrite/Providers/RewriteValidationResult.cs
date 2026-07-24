namespace Fluent.Rewrite.Providers;

public sealed record RewriteValidationResult(bool IsValid, RewriteFailureReason Reason)
{
    public static readonly RewriteValidationResult Valid = new(true, RewriteFailureReason.None);

    public static RewriteValidationResult Invalid(RewriteFailureReason reason) => new(false, reason);
}
