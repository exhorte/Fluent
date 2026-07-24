using Fluent.Rewrite.Providers;

namespace Fluent.Rewrite.Validation;

/// <summary>
/// Validates a cloud rewrite candidate against the source before it may replace the local
/// text. Implementations must fail closed; the orchestrator treats any thrown exception as
/// a fallback to the exact local text.
/// </summary>
public interface ICloudRewriteValidator
{
    RewriteValidationResult Validate(string source, string candidate);
}
