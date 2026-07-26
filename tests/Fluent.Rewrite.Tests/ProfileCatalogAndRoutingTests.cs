using Fluent.Rewrite.Profiles;
using Fluent.Rewrite.Rewriting;
using Fluent.Rewrite.Validation;

namespace Fluent.Rewrite.Tests;

public sealed class ProfileCatalogTests
{
    [Fact]
    public void Catalog_contains_three_canonical_profiles_with_truthful_availability()
    {
        Assert.Equal(3, RewriteProfiles.All.Count);

        Assert.Same(RewriteProfiles.ProfessionalFrench, RewriteProfiles.All[0]);
        Assert.Same(RewriteProfiles.Developer, RewriteProfiles.All[1]);
        Assert.Same(RewriteProfiles.SimplifiedFrench, RewriteProfiles.All[2]);

        Assert.True(RewriteProfiles.ProfessionalFrench.IsAvailable);
        Assert.True(RewriteProfiles.Developer.IsAvailable);
        Assert.False(RewriteProfiles.SimplifiedFrench.IsAvailable);
        Assert.NotEmpty(RewriteProfiles.SimplifiedFrench.UnavailableReason);
    }

    [Fact]
    public void Default_profile_is_professional_french()
    {
        Assert.Same(RewriteProfiles.ProfessionalFrench, RewriteProfiles.Default);
        Assert.Equal("professional-fr", RewriteProfiles.Default.Id);
    }

    [Fact]
    public void Fabricated_profile_with_identical_values_is_not_canonical()
    {
        RewriteProfile fabricated = RewriteProfiles.Developer with { };

        Assert.Equal(RewriteProfiles.Developer, fabricated);
        Assert.False(RewriteProfiles.IsCanonicalAvailable(fabricated));
    }

    [Fact]
    public void Canonical_available_profiles_are_recognized()
    {
        Assert.True(RewriteProfiles.IsCanonicalAvailable(RewriteProfiles.ProfessionalFrench));
        Assert.True(RewriteProfiles.IsCanonicalAvailable(RewriteProfiles.Developer));
        Assert.False(RewriteProfiles.IsCanonicalAvailable(RewriteProfiles.SimplifiedFrench));
        Assert.False(RewriteProfiles.IsCanonicalAvailable(null));
    }

    [Theory]
    [InlineData("simplified-fr")]
    [InlineData("unknown")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("PROFESSIONAL-FR")]
    public void TryGetAvailable_rejects_unknown_unavailable_or_non_exact_ids(string? id)
    {
        bool found = RewriteProfiles.TryGetAvailable(id, out RewriteProfile fallback);

        Assert.False(found);
        Assert.Same(RewriteProfiles.Default, fallback);
    }

    [Theory]
    [InlineData("professional-fr")]
    [InlineData("developer")]
    public void TryGetAvailable_returns_the_canonical_instance_for_available_ids(string id)
    {
        bool found = RewriteProfiles.TryGetAvailable(id, out RewriteProfile profile);

        Assert.True(found);
        Assert.Equal(id, profile.Id);
        Assert.True(RewriteProfiles.IsCanonicalAvailable(profile));
    }
}

public sealed class ProfileSelectionTests
{
    [Fact]
    public void Selection_starts_with_professional_french()
    {
        ProfileSelection selection = new();

        Assert.Same(RewriteProfiles.ProfessionalFrench, selection.Current);
    }

    [Fact]
    public void Selecting_developer_succeeds_and_updates_current()
    {
        ProfileSelection selection = new();

        ProfileSelectionResult result = selection.TrySelect("developer");

        Assert.True(result.Succeeded);
        Assert.Same(RewriteProfiles.Developer, result.Selected);
        Assert.Same(RewriteProfiles.Developer, selection.Current);
    }

    [Theory]
    [InlineData("simplified-fr")]
    [InlineData("unknown")]
    [InlineData(null)]
    public void Selecting_unknown_or_unavailable_keeps_current_selection(string? id)
    {
        ProfileSelection selection = new();
        selection.TrySelect("developer");

        ProfileSelectionResult result = selection.TrySelect(id);

        Assert.False(result.Succeeded);
        Assert.Same(RewriteProfiles.Developer, result.Selected);
        Assert.Same(RewriteProfiles.Developer, selection.Current);
    }

    [Fact]
    public void Snapshot_captured_before_a_change_is_not_affected_by_the_change()
    {
        ProfileSelection selection = new();
        RewriteProfile snapshot = selection.Current;

        selection.TrySelect("developer");

        Assert.Same(RewriteProfiles.ProfessionalFrench, snapshot);
        Assert.Same(RewriteProfiles.Developer, selection.Current);
    }
}

public sealed class ProfileRoutingTests
{
    private readonly ProfileRoutedRewriter _router = new();

    [Fact]
    public async Task Professional_route_normalizes_spacing_and_punctuation()
    {
        string result = await _router.RewriteAsync(
            new RewriteRequest("Bonjour  ,monde", RewriteProfiles.ProfessionalFrench));

        Assert.Equal("Bonjour, monde.", result);
    }

    [Theory]
    [InlineData("bonjour tout le monde")]
    [InlineData("  espace  initial et final  ")]
    [InlineData("ligne un\r\nligne deux\nligne trois")]
    [InlineData("git commit -m \"fix: retry\" && dotnet build Fluent.sln -c Release")]
    [InlineData("Ouvrir C:\\Apps\\Fluent\\bin et https://example.com/api?x=1&y=2")]
    [InlineData("Version v1.2.3, budget 42,50 euros, issue_123, Namespace::Type, localhost:5000")]
    [InlineData("Héllo àéîôù œuf — em-dash… «guillemets» ‘quotes’ 汉字 ✦")]
    [InlineData("contacter jean.dupont@example.com au sujet de mailto:x@y.z")]
    public async Task Developer_route_returns_the_exact_source_text(string source)
    {
        string result = await _router.RewriteAsync(
            new RewriteRequest(source, RewriteProfiles.Developer));

        Assert.Equal(source, result);
    }

    [Fact]
    public async Task Unavailable_or_fabricated_profile_route_throws()
    {
        RewriteProfile fabricated = RewriteProfiles.Developer with { };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _router.RewriteAsync(new RewriteRequest("Bonjour", fabricated)));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _router.RewriteAsync(new RewriteRequest("Bonjour", RewriteProfiles.SimplifiedFrench)));
    }

    [Fact]
    public async Task Routing_propagates_cancellation()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _router.RewriteAsync(
                new RewriteRequest("Bonjour", RewriteProfiles.Developer),
                cancellation.Token));
    }
}

public sealed class ProfileRoutedSafeServiceTests
{
    private readonly SafeProfileRewriteService _service = new(
        new ProfileRoutedRewriter(),
        new RewriteOutputValidator());

    [Fact]
    public async Task Professional_profile_applies_safe_normalization()
    {
        RewriteResult result = await _service.RewriteAsync(
            "Bonjour  ,monde",
            RewriteProfiles.ProfessionalFrench);

        Assert.Equal(RewriteOutcome.Applied, result.Outcome);
        Assert.Equal("Bonjour, monde.", result.Text);
    }

    [Fact]
    public async Task Developer_profile_applies_exact_source_output()
    {
        const string source = "dotnet test Fluent.sln -c Release --filter FullyQualifiedName~Profile";

        RewriteResult result = await _service.RewriteAsync(source, RewriteProfiles.Developer);

        Assert.Equal(RewriteOutcome.Applied, result.Outcome);
        Assert.Equal(source, result.Text);
    }

    [Fact]
    public async Task Fabricated_profile_falls_back_to_the_exact_source()
    {
        const string source = "Texte exact à préserver v1.2.3";
        RewriteProfile fabricated = RewriteProfiles.ProfessionalFrench with { };

        RewriteResult result = await _service.RewriteAsync(source, fabricated);

        Assert.Equal(RewriteOutcome.RawFallbackRewriterFailed, result.Outcome);
        Assert.Equal(source, result.Text);
    }

    [Fact]
    public async Task Unavailable_simplified_profile_falls_back_to_the_exact_source()
    {
        const string source = "Texte exact à préserver";

        RewriteResult result = await _service.RewriteAsync(source, RewriteProfiles.SimplifiedFrench);

        Assert.Equal(RewriteOutcome.RawFallbackRewriterFailed, result.Outcome);
        Assert.Equal(source, result.Text);
    }

    [Fact]
    public async Task Developer_profile_propagates_cancellation()
    {
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _service.RewriteAsync("Bonjour", RewriteProfiles.Developer, cancellationToken: cancellation.Token));
    }
}
