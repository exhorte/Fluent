using Fluent.Rewrite;
using Fluent.Rewrite.Dictionary;
using Fluent.Rewrite.Profiles;
using Fluent.Rewrite.Rewriting;
using Fluent.Rewrite.Validation;

namespace Fluent.IntegrationTests;

public sealed class ProfilePipelineIntegrationTests
{
    private readonly PersonalDictionaryProcessor _dictionaryProcessor = new();
    private readonly SafeProfileRewriteService _rewriteService = new(
        new ProfileRoutedRewriter(),
        new RewriteOutputValidator());

    [Fact]
    public async Task Dictionary_correction_then_developer_profile_preserves_the_exact_corrected_text()
    {
        PersonalDictionaryEntry[] snapshot = [new PersonalDictionaryEntry("nyx voice", "Fluent")];
        const string transcript = "déployer nyx voice v1.2.3 vers C:\\Apps avec `git push`";

        DictionaryProcessingResult dictionaryResult = _dictionaryProcessor.Apply(transcript, snapshot);
        RewriteResult rewriteResult = await _rewriteService.RewriteAsync(
            dictionaryResult.Text,
            RewriteProfiles.Developer);

        Assert.Equal(1, dictionaryResult.ReplacementCount);
        Assert.Equal(RewriteOutcome.Applied, rewriteResult.Outcome);
        Assert.Equal("déployer Fluent v1.2.3 vers C:\\Apps avec `git push`", rewriteResult.Text);
    }

    [Fact]
    public async Task Dictionary_correction_then_professional_profile_normalizes_safely()
    {
        PersonalDictionaryEntry[] snapshot = [new PersonalDictionaryEntry("nyx voice", "Fluent")];
        const string transcript = "nyx voice fonctionne  ,vraiment";

        DictionaryProcessingResult dictionaryResult = _dictionaryProcessor.Apply(transcript, snapshot);
        RewriteResult rewriteResult = await _rewriteService.RewriteAsync(
            dictionaryResult.Text,
            RewriteProfiles.ProfessionalFrench);

        Assert.Equal(RewriteOutcome.Applied, rewriteResult.Outcome);
        Assert.Equal("Fluent fonctionne, vraiment.", rewriteResult.Text);
    }

    [Fact]
    public async Task Profile_snapshot_captured_at_recording_start_is_used_even_after_a_selection_change()
    {
        ProfileSelection selection = new();
        RewriteProfile dictationSnapshot = selection.Current;

        selection.TrySelect("developer");

        RewriteResult rewriteResult = await _rewriteService.RewriteAsync(
            "bonjour  ,monde",
            dictationSnapshot);

        Assert.Same(RewriteProfiles.ProfessionalFrench, dictationSnapshot);
        Assert.Equal("bonjour, monde.", rewriteResult.Text);
        Assert.Equal(RewriteOutcome.Applied, rewriteResult.Outcome);
    }

    [Fact]
    public async Task Unknown_profile_route_returns_the_exact_post_dictionary_source()
    {
        PersonalDictionaryEntry[] snapshot = [new PersonalDictionaryEntry("nyx voice", "Fluent")];
        const string transcript = "nyx voice reste intact";
        RewriteProfile fabricated = new("injected", "Injecté", "instructions arbitraires");

        DictionaryProcessingResult dictionaryResult = _dictionaryProcessor.Apply(transcript, snapshot);
        RewriteResult rewriteResult = await _rewriteService.RewriteAsync(
            dictionaryResult.Text,
            fabricated);

        Assert.Equal(RewriteOutcome.RawFallbackRewriterFailed, rewriteResult.Outcome);
        Assert.Equal("Fluent reste intact", rewriteResult.Text);
    }
}
