using Fluent.Rewrite.Profiles;
using Fluent.Rewrite.Rewriting;
using Fluent.Rewrite.Validation;

namespace Fluent.Rewrite;

public sealed class SafeProfileRewriteService
{
    private readonly ILocalTextRewriter _rewriter;
    private readonly RewriteOutputValidator _validator;

    public SafeProfileRewriteService(
        ILocalTextRewriter rewriter,
        RewriteOutputValidator validator)
    {
        _rewriter = rewriter ?? throw new ArgumentNullException(nameof(rewriter));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    public async Task<RewriteResult> RewriteAsync(
        string transcript,
        RewriteProfile profile,
        string transcriptionLanguage = "fr",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transcript);
        ArgumentNullException.ThrowIfNull(profile);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            string candidate = await _rewriter.RewriteAsync(
                new RewriteRequest(transcript, profile, transcriptionLanguage),
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(candidate))
            {
                return new RewriteResult(transcript, RewriteOutcome.RawFallbackEmpty);
            }

            return _validator.IsValid(transcript, candidate)
                ? new RewriteResult(candidate, RewriteOutcome.Applied)
                : new RewriteResult(transcript, RewriteOutcome.RawFallbackValidationFailed);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return new RewriteResult(transcript, RewriteOutcome.RawFallbackRewriterFailed);
        }
    }
}
