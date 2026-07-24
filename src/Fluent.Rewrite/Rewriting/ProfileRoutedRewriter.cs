using Fluent.Rewrite.Profiles;

namespace Fluent.Rewrite.Rewriting;

/// <summary>
/// Deterministic router: only canonical catalog instances can reach a rewriter.
/// Any unknown, unavailable, or fabricated profile object fails the route, which
/// the safe rewrite service converts into an exact-source fallback.
/// </summary>
public sealed class ProfileRoutedRewriter : ILocalTextRewriter
{
    private readonly ProfessionalFrenchRuleBasedRewriter _professionalFrench = new();
    private readonly DeveloperPassThroughRewriter _developer = new();

    public Task<string> RewriteAsync(
        RewriteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        if (ReferenceEquals(request.Profile, RewriteProfiles.ProfessionalFrench))
        {
            return _professionalFrench.RewriteAsync(request, cancellationToken);
        }

        if (ReferenceEquals(request.Profile, RewriteProfiles.Developer))
        {
            return _developer.RewriteAsync(request, cancellationToken);
        }

        throw new InvalidOperationException(
            $"No local route exists for rewrite profile '{request.Profile.Id}'.");
    }
}
