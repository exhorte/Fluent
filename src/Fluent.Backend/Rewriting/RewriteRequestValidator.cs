namespace Fluent.Backend.Rewriting;

public sealed record RewriteRequestValidationResult(bool IsValid, string? Error);

public static class RewriteRequestValidator
{
    public const int MaxTextLength = 8000;

    public static RewriteRequestValidationResult Validate(ServerRewriteRequest? request)
    {
        if (request is null)
        {
            return new RewriteRequestValidationResult(false, "missing_body");
        }

        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return new RewriteRequestValidationResult(false, "missing_text");
        }

        if (request.Text.Length > MaxTextLength)
        {
            return new RewriteRequestValidationResult(false, "text_too_long");
        }

        if (string.IsNullOrWhiteSpace(request.Provider))
        {
            return new RewriteRequestValidationResult(false, "missing_provider");
        }

        return new RewriteRequestValidationResult(true, null);
    }
}
