using Fluent.Rewrite.Profiles;
using Fluent.Rewrite.Rewriting;
using Fluent.Rewrite.Validation;

namespace Fluent.Rewrite.Tests;

public sealed class ProfessionalFrenchRewriteTests
{
    private static readonly RewriteProfile Profile = RewriteProfiles.ProfessionalFrench;

    [Fact]
    public void Professional_profile_is_explicit_and_bounded()
    {
        Assert.Equal("professional-fr", Profile.Id);
        Assert.Equal("Français professionnel", Profile.DisplayName);
        Assert.Contains("sans ajouter", Profile.Instructions, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("  Bonjour   le monde  !  ", "Bonjour le monde !")]
    [InlineData("Bonjour  ,monde", "Bonjour, monde.")]
    [InlineData("Bonjour.", "Bonjour.")]
    [InlineData("Contacter jean.dupont@example.com", "Contacter jean.dupont@example.com.")]
    [InlineData("Ouvrir example.com", "Ouvrir example.com.")]
    [InlineData("Utiliser System.Console", "Utiliser System.Console.")]
    [InlineData("Utiliser Namespace::Type.", "Utiliser Namespace::Type.")]
    [InlineData("Ouvrir localhost:5000.", "Ouvrir localhost:5000.")]
    [InlineData("Contacter mailto:jean@example.com.", "Contacter mailto:jean@example.com.")]
    public async Task Rule_based_rewriter_normalizes_only_spacing_and_punctuation(
        string source,
        string expected)
    {
        ProfessionalFrenchRuleBasedRewriter rewriter = new();

        string result = await rewriter.RewriteAsync(new RewriteRequest(source, Profile));

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Rule_based_rewriter_propagates_cancellation()
    {
        ProfessionalFrenchRuleBasedRewriter rewriter = new();
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => rewriter.RewriteAsync(
                new RewriteRequest("Bonjour", Profile),
                cancellation.Token));
    }
}

public sealed class RewriteOutputValidatorTests
{
    private readonly RewriteOutputValidator _validator = new();

    [Fact]
    public void Punctuation_only_change_is_valid()
    {
        const string source =
            "Déployer Fluent v1.2.3 vers C:\\Apps\\Fluent avec https://example.com/api et `git status`";
        const string candidate =
            "Déployer Fluent v1.2.3 vers C:\\Apps\\Fluent avec https://example.com/api et `git status` !";

        Assert.True(_validator.IsValid(source, candidate));
    }

    [Theory]
    [InlineData("Version v1.2.3.", "Version v1.2.4.")]
    [InlineData("Budget 42 euros.", "Budget 43 euros.")]
    [InlineData("Voir https://example.com/a.", "Voir https://example.com/b.")]
    [InlineData("Ouvrir C:\\Apps\\Fluent.", "Ouvrir C:\\Apps\\NyxFlow.")]
    [InlineData("Lancer `git status`.", "Lancer `git push`.")]
    [InlineData("Corriger issue_123.", "Corriger issue_124.")]
    [InlineData("Contacter jean.dupont@example.com.", "Contacter jean.dupont@example.net.")]
    [InlineData("Ouvrir example.com.", "Ouvrir example.net.")]
    [InlineData("Utiliser System.Console.", "Utiliser System.Diagnostics.")]
    [InlineData("Utiliser Namespace::Type.", "Utiliser Namespace : : Type.")]
    [InlineData("Ouvrir localhost:5000.", "Ouvrir localhost : 5000.")]
    [InlineData("Contacter mailto:jean@example.com.", "Contacter mailto : jean@example.com.")]
    public void Sensitive_token_change_is_rejected(string source, string candidate)
    {
        Assert.False(_validator.IsValid(source, candidate));
    }

    [Theory]
    [InlineData("Bonjour monde.", "Bonjour grand monde.")]
    [InlineData("Bonjour grand monde.", "Bonjour monde.")]
    [InlineData("Bonjour grand monde.", "Grand bonjour monde.")]
    [InlineData("Fluent Fluent fonctionne.", "Fluent fonctionne.")]
    [InlineData("Fluent fonctionne.", "fluent fonctionne.")]
    public void Lexical_addition_removal_reorder_duplicate_or_case_change_is_rejected(
        string source,
        string candidate)
    {
        Assert.False(_validator.IsValid(source, candidate));
    }
}

public sealed class SafeProfileRewriteServiceTests
{
    private static readonly RewriteProfile Profile = RewriteProfiles.ProfessionalFrench;

    [Fact]
    public async Task Valid_candidate_is_applied()
    {
        SafeProfileRewriteService service = CreateService(
            (_, _) => Task.FromResult("Bonjour, monde !"));

        RewriteResult result = await service.RewriteAsync("Bonjour monde", Profile);

        Assert.Equal(RewriteOutcome.Applied, result.Outcome);
        Assert.Equal("Bonjour, monde !", result.Text);
        Assert.True(result.WasApplied);
    }

    [Fact]
    public async Task Empty_candidate_returns_exact_source()
    {
        string source = new("  Bonjour monde  ".ToCharArray());
        SafeProfileRewriteService service = CreateService(
            (_, _) => Task.FromResult("   "));

        RewriteResult result = await service.RewriteAsync(source, Profile);

        Assert.Equal(RewriteOutcome.RawFallbackEmpty, result.Outcome);
        Assert.Same(source, result.Text);
    }

    [Fact]
    public async Task Invalid_candidate_returns_exact_source()
    {
        string source = new("Budget 42 euros".ToCharArray());
        SafeProfileRewriteService service = CreateService(
            (_, _) => Task.FromResult("Budget 43 euros."));

        RewriteResult result = await service.RewriteAsync(source, Profile);

        Assert.Equal(RewriteOutcome.RawFallbackValidationFailed, result.Outcome);
        Assert.Same(source, result.Text);
    }

    [Fact]
    public async Task Rewriter_exception_returns_exact_source()
    {
        string source = new("Bonjour monde".ToCharArray());
        SafeProfileRewriteService service = CreateService(
            (_, _) => Task.FromException<string>(new InvalidOperationException("local failure")));

        RewriteResult result = await service.RewriteAsync(source, Profile);

        Assert.Equal(RewriteOutcome.RawFallbackRewriterFailed, result.Outcome);
        Assert.Same(source, result.Text);
    }

    [Fact]
    public async Task Cancellation_is_propagated()
    {
        SafeProfileRewriteService service = CreateService(
            (_, _) => Task.FromException<string>(new OperationCanceledException()));

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.RewriteAsync("Bonjour", Profile));
    }

    [Fact]
    public async Task Cancellation_after_rewriter_completion_is_propagated()
    {
        using CancellationTokenSource cancellation = new();
        SafeProfileRewriteService service = CreateService(
            (_, _) =>
            {
                cancellation.Cancel();
                return Task.FromResult("Bonjour.");
            });

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => service.RewriteAsync("Bonjour", Profile, cancellationToken: cancellation.Token));
    }

    [Fact]
    public async Task Rule_based_url_mutation_fails_closed_to_exact_source()
    {
        string source = new("Ouvrir https://example.com".ToCharArray());
        SafeProfileRewriteService service = new(
            new ProfessionalFrenchRuleBasedRewriter(),
            new RewriteOutputValidator());

        RewriteResult result = await service.RewriteAsync(source, Profile);

        Assert.Equal(RewriteOutcome.RawFallbackValidationFailed, result.Outcome);
        Assert.Same(source, result.Text);
    }

    private static SafeProfileRewriteService CreateService(
        Func<RewriteRequest, CancellationToken, Task<string>> rewrite)
    {
        return new SafeProfileRewriteService(
            new StubRewriter(rewrite),
            new RewriteOutputValidator());
    }

    private sealed class StubRewriter(
        Func<RewriteRequest, CancellationToken, Task<string>> rewrite)
        : ILocalTextRewriter
    {
        public Task<string> RewriteAsync(
            RewriteRequest request,
            CancellationToken cancellationToken = default)
        {
            return rewrite(request, cancellationToken);
        }
    }
}
