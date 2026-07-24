using System.Text.RegularExpressions;
using Fluent.Rewrite.Observability;
using Fluent.Rewrite.Orchestration;
using Fluent.Rewrite.Profiles;
using Fluent.Rewrite.Providers;
using Fluent.Rewrite.Rewriting;
using Fluent.Rewrite.Validation;

namespace Fluent.Rewrite.Tests;

internal sealed class SpyCloudRewriteClient : ICloudRewriteClient
{
    private readonly Func<CloudRewriteTransportRequest, CloudRewriteTransportResult> _handler;

    public SpyCloudRewriteClient(Func<CloudRewriteTransportRequest, CloudRewriteTransportResult> handler)
    {
        _handler = handler;
    }

    public int CallCount { get; private set; }

    public RewriteProviderId? LastProvider { get; private set; }

    public Task<CloudRewriteTransportResult> SendAsync(
        CloudRewriteTransportRequest request,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastProvider = request.Provider;
        return Task.FromResult(_handler(request));
    }
}

internal sealed class RecordingObservabilitySink : IRewriteObservabilitySink
{
    public List<RewriteTelemetry> Records { get; } = [];

    public void Record(RewriteTelemetry telemetry) => Records.Add(telemetry);
}

public sealed class CloudRewriteOrchestratorTests
{
    private const string Source = "le budget est de 42 euros le 12/03/2026";

    private static RewriteOrchestrator CreateOrchestrator(
        ICloudRewriteClient client,
        IRewriteObservabilitySink? sink = null)
    {
        SafeProfileRewriteService local = new(new ProfileRoutedRewriter(), new RewriteOutputValidator());
        return new RewriteOrchestrator(
            new LocalRewriteProvider(local),
            new CloudRewriteProvider(new GeminiRewriteProvider(client), new DeepSeekRewriteProvider(client)),
            new CloudRewriteValidator(),
            sink);
    }

    private static OrchestrationRewriteRequest Request(RewriteContext context) =>
        new(Source, RewriteProfiles.ProfessionalFrench, context);

    private static RewriteContext FullyGated() =>
        new(true, true, true, RewriteProviderId.Gemini);

    private static async Task<string> LocalTextAsync()
    {
        SafeProfileRewriteService local = new(new ProfileRoutedRewriter(), new RewriteOutputValidator());
        LocalRewriteProvider provider = new(local);
        ProviderRewriteResult result = await provider.RewriteAsync(
            new ProviderRewriteRequest(Source, RewriteProfiles.ProfessionalFrench));
        return result.Text;
    }

    [Fact]
    public async Task Not_authenticated_uses_local_and_never_calls_cloud()
    {
        SpyCloudRewriteClient client = new(_ => CloudRewriteTransportResult.Success("ignored"));
        RewriteOrchestrator orchestrator = CreateOrchestrator(client);

        OrchestrationRewriteResult result = await orchestrator.RewriteAsync(
            Request(new RewriteContext(false, true, true, RewriteProviderId.Gemini)));

        Assert.Equal(0, client.CallCount);
        Assert.Equal(RewriteStatus.LocalApplied, result.Status);
        Assert.Equal(RewriteProviderId.Local, result.ProviderUsed);
        Assert.Equal(await LocalTextAsync(), result.Text);
    }

    [Fact]
    public async Task Authenticated_without_enabling_never_calls_cloud()
    {
        SpyCloudRewriteClient client = new(_ => CloudRewriteTransportResult.Success("ignored"));
        RewriteOrchestrator orchestrator = CreateOrchestrator(client);

        OrchestrationRewriteResult result = await orchestrator.RewriteAsync(
            Request(new RewriteContext(true, false, true, RewriteProviderId.Gemini)));

        Assert.Equal(0, client.CallCount);
        Assert.Equal(RewriteStatus.LocalApplied, result.Status);
    }

    [Fact]
    public async Task Authenticated_and_enabled_without_consent_never_calls_cloud()
    {
        SpyCloudRewriteClient client = new(_ => CloudRewriteTransportResult.Success("ignored"));
        RewriteOrchestrator orchestrator = CreateOrchestrator(client);

        OrchestrationRewriteResult result = await orchestrator.RewriteAsync(
            Request(new RewriteContext(true, true, false, RewriteProviderId.Gemini)));

        Assert.Equal(0, client.CallCount);
        Assert.Equal(RewriteStatus.LocalApplied, result.Status);
    }

    [Fact]
    public async Task Fully_gated_explicit_deepseek_rewrite_is_applied()
    {
        const string candidate = "Le budget atteint 42 euros le 12/03/2026.";
        SpyCloudRewriteClient client = new(_ => CloudRewriteTransportResult.Success(candidate));
        RewriteOrchestrator orchestrator = CreateOrchestrator(client);

        OrchestrationRewriteResult result = await orchestrator.RewriteAsync(
            Request(new RewriteContext(true, true, true, RewriteProviderId.DeepSeek)));

        Assert.Equal(1, client.CallCount);
        Assert.Equal(RewriteProviderId.DeepSeek, client.LastProvider);
        Assert.Equal(RewriteProviderId.DeepSeek, result.ProviderUsed);
        Assert.Equal(RewriteStatus.CloudApplied, result.Status);
    }

    [Fact]
    public void DeepSeek_capabilities_are_enabled_but_require_the_existing_cloud_gate()
    {
        DeepSeekRewriteProvider provider = new(new SpyCloudRewriteClient(_ => CloudRewriteTransportResult.Success("unused")));

        Assert.True(provider.Capabilities.IsEnabled);
        Assert.Equal(RewriteProviderId.DeepSeek, provider.Capabilities.Id);
    }

    [Fact]
    public async Task Fully_gated_gemini_rewrite_is_applied()
    {
        const string candidate = "Le budget atteint 42 euros le 12/03/2026.";
        SpyCloudRewriteClient client = new(_ => CloudRewriteTransportResult.Success(candidate));
        RewriteOrchestrator orchestrator = CreateOrchestrator(client);

        OrchestrationRewriteResult result = await orchestrator.RewriteAsync(Request(FullyGated()));

        Assert.Equal(1, client.CallCount);
        Assert.Equal(RewriteProviderId.Gemini, client.LastProvider);
        Assert.Equal(RewriteStatus.CloudApplied, result.Status);
        Assert.Equal(candidate, result.Text);
    }

    [Theory]
    [InlineData(RewriteFailureReason.Timeout)]
    [InlineData(RewriteFailureReason.TransportError)]
    [InlineData(RewriteFailureReason.InvalidResponse)]
    public async Task Cloud_failure_falls_back_to_exact_local_text(RewriteFailureReason reason)
    {
        SpyCloudRewriteClient client = new(_ => CloudRewriteTransportResult.Failure(reason));
        RewriteOrchestrator orchestrator = CreateOrchestrator(client);

        OrchestrationRewriteResult result = await orchestrator.RewriteAsync(Request(FullyGated()));

        Assert.Equal(RewriteStatus.LocalFallback, result.Status);
        Assert.True(result.FallbackUsed);
        Assert.Equal(reason, result.FailureReason);
        Assert.Equal(await LocalTextAsync(), result.Text);
    }

    [Fact]
    public async Task Cloud_transport_exception_falls_back_to_exact_local_text()
    {
        RewriteOrchestrator orchestrator = CreateOrchestrator(new ThrowingCloudClient());

        OrchestrationRewriteResult result = await orchestrator.RewriteAsync(Request(FullyGated()));

        Assert.Equal(RewriteStatus.LocalFallback, result.Status);
        Assert.Equal(RewriteFailureReason.TransportError, result.FailureReason);
        Assert.Equal(await LocalTextAsync(), result.Text);
    }

    [Fact]
    public async Task Cloud_empty_response_falls_back_to_exact_local_text()
    {
        SpyCloudRewriteClient client = new(_ => CloudRewriteTransportResult.Success("   "));
        RewriteOrchestrator orchestrator = CreateOrchestrator(client);

        OrchestrationRewriteResult result = await orchestrator.RewriteAsync(Request(FullyGated()));

        Assert.Equal(RewriteStatus.LocalFallback, result.Status);
        Assert.Equal(await LocalTextAsync(), result.Text);
    }

    [Fact]
    public async Task Cloud_conversational_response_falls_back_to_exact_local_text()
    {
        SpyCloudRewriteClient client = new(_ =>
            CloudRewriteTransportResult.Success("Voici le texte reformule : 42 euros le 12/03/2026"));
        RewriteOrchestrator orchestrator = CreateOrchestrator(client);

        OrchestrationRewriteResult result = await orchestrator.RewriteAsync(Request(FullyGated()));

        Assert.Equal(RewriteStatus.LocalFallback, result.Status);
        Assert.Equal(RewriteFailureReason.ConversationalResponse, result.FailureReason);
        Assert.Equal(await LocalTextAsync(), result.Text);
    }

    [Fact]
    public async Task Cloud_altering_a_number_falls_back_to_exact_local_text()
    {
        SpyCloudRewriteClient client = new(_ =>
            CloudRewriteTransportResult.Success("Le budget est de 43 euros le 12/03/2026."));
        RewriteOrchestrator orchestrator = CreateOrchestrator(client);

        OrchestrationRewriteResult result = await orchestrator.RewriteAsync(Request(FullyGated()));

        Assert.Equal(RewriteStatus.LocalFallback, result.Status);
        Assert.Equal(RewriteFailureReason.InvalidResponse, result.FailureReason);
        Assert.Equal(await LocalTextAsync(), result.Text);
    }

    [Fact]
    public async Task Telemetry_records_provider_and_fallback_only()
    {
        RecordingObservabilitySink sink = new();
        SpyCloudRewriteClient client = new(_ => CloudRewriteTransportResult.Failure(RewriteFailureReason.Timeout));
        RewriteOrchestrator orchestrator = CreateOrchestrator(client, sink);

        await orchestrator.RewriteAsync(Request(FullyGated()));

        RewriteTelemetry telemetry = Assert.Single(sink.Records);
        Assert.Equal(RewriteProviderId.Gemini, telemetry.Provider);
        Assert.True(telemetry.FallbackUsed);
        Assert.Equal(RewriteFailureReason.Timeout, telemetry.FallbackReason);
    }

    [Fact]
    public void Telemetry_type_carries_no_free_text_field()
    {
        Assert.DoesNotContain(
            typeof(RewriteTelemetry).GetProperties(),
            property => property.PropertyType == typeof(string));
    }

    [Fact]
    public async Task Cancellation_is_propagated()
    {
        SpyCloudRewriteClient client = new(_ => CloudRewriteTransportResult.Success("x"));
        RewriteOrchestrator orchestrator = CreateOrchestrator(client);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => orchestrator.RewriteAsync(Request(FullyGated()), cancellation.Token));
    }

    [Fact]
    public async Task Validator_failure_still_degrades_to_the_exact_local_text()
    {
        SpyCloudRewriteClient client = new(_ => CloudRewriteTransportResult.Success("candidat"));
        SafeProfileRewriteService local = new(new ProfileRoutedRewriter(), new RewriteOutputValidator());
        RewriteOrchestrator orchestrator = new(
            new LocalRewriteProvider(local),
            new CloudRewriteProvider(new GeminiRewriteProvider(client), new DeepSeekRewriteProvider(client)),
            new ThrowingCloudRewriteValidator());

        OrchestrationRewriteResult result = await orchestrator.RewriteAsync(Request(FullyGated()));

        Assert.Equal(RewriteStatus.LocalFallback, result.Status);
        Assert.True(result.FallbackUsed);
        Assert.Equal(await LocalTextAsync(), result.Text);
    }

    private sealed class ThrowingCloudRewriteValidator : ICloudRewriteValidator
    {
        public RewriteValidationResult Validate(string source, string candidate) =>
            throw new RegexMatchTimeoutException(candidate, "pattern", TimeSpan.FromMilliseconds(250));
    }

    private sealed class ThrowingCloudClient : ICloudRewriteClient
    {
        public Task<CloudRewriteTransportResult> SendAsync(
            CloudRewriteTransportRequest request,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("transport exploded");
    }
}

public sealed class CloudRewriteValidatorTests
{
    private readonly CloudRewriteValidator _validator = new();

    [Fact]
    public void Faithful_rephrase_is_accepted()
    {
        RewriteValidationResult result = _validator.Validate(
            "le budget est de 42 euros le 12/03/2026",
            "Le budget atteint 42 euros le 12/03/2026.");

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public void Empty_candidate_is_rejected(string candidate)
    {
        RewriteValidationResult result = _validator.Validate("texte source", candidate);

        Assert.False(result.IsValid);
        Assert.Equal(RewriteFailureReason.EmptyResponse, result.Reason);
    }

    [Theory]
    [InlineData("Voici la reformulation demandee.")]
    [InlineData("Sure, here is the rewritten text.")]
    [InlineData("As an AI language model, I cannot help.")]
    [InlineData("Certainly, the text follows.")]
    public void Conversational_candidate_is_rejected(string candidate)
    {
        RewriteValidationResult result = _validator.Validate("texte source simple", candidate);

        Assert.False(result.IsValid);
        Assert.Equal(RewriteFailureReason.ConversationalResponse, result.Reason);
    }

    [Theory]
    [InlineData("Version v1.2.3 deployee", "Version v1.2.4 deployee")]
    [InlineData("Ouvrir https://example.com/a", "Ouvrir https://example.com/b")]
    [InlineData("Chemin C:\\Apps\\Fluent", "Chemin C:\\Apps\\NyxFlow")]
    [InlineData("Lancer `git status`", "Lancer `git push`")]
    [InlineData("Rendez-vous le 12/03/2026", "Rendez-vous le 13/03/2026")]
    [InlineData("Contacter jean.dupont@example.com", "Contacter jean.dupont@example.net")]
    [InlineData("Budget 42 euros", "Budget 43 euros")]
    public void Sensitive_token_mutation_is_rejected(string source, string candidate)
    {
        RewriteValidationResult result = _validator.Validate(source, candidate);

        Assert.False(result.IsValid);
        Assert.Equal(RewriteFailureReason.InvalidResponse, result.Reason);
    }

    [Theory]
    [InlineData("Contacter mailto:jean@example.com", "Contacter mailto:paul@example.com")]
    [InlineData("Utiliser std::vector ici", "Utiliser std::list ici")]
    [InlineData("Ouvrir ssh://serveur/chemin", "Ouvrir ssh://autre/chemin")]
    [InlineData("Copier \\\\serveur\\partage\\fichier", "Copier \\\\serveur\\autre\\fichier")]
    [InlineData("Le produit Fluent evolue", "Le produit NyxFlow evolue")]
    public void Scheme_unc_and_proper_noun_mutation_is_rejected(string source, string candidate)
    {
        RewriteValidationResult result = _validator.Validate(source, candidate);

        Assert.False(result.IsValid);
        Assert.Equal(RewriteFailureReason.InvalidResponse, result.Reason);
    }

    [Fact]
    public void Pathological_slash_run_completes_quickly_without_backtracking_blowup()
    {
        string source = "chemin /" + string.Join("/", Enumerable.Repeat("aaaa", 400));
        string candidate = source + " suite";

        System.Diagnostics.Stopwatch timer = System.Diagnostics.Stopwatch.StartNew();
        RewriteValidationResult result = _validator.Validate(source, candidate);
        timer.Stop();

        Assert.True(result.IsValid);
        Assert.True(
            timer.Elapsed < TimeSpan.FromSeconds(2),
            $"Validation took {timer.Elapsed.TotalSeconds:F2}s, indicating catastrophic backtracking.");
    }

    [Fact]
    public void Dropping_a_sensitive_token_is_rejected()
    {
        RewriteValidationResult result = _validator.Validate(
            "Livraison le 12/03/2026 pour 42 euros",
            "Livraison prevue pour 42 euros");

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Over_long_candidate_is_rejected()
    {
        string candidate = new string((char)97, 500);

        RewriteValidationResult result = _validator.Validate("court", candidate);

        Assert.False(result.IsValid);
        Assert.Equal(RewriteFailureReason.InvalidResponse, result.Reason);
    }
}
