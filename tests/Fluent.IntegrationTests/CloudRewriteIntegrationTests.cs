using System.Text.RegularExpressions;
using Fluent.App.Cloud;
using Fluent.Rewrite;
using Fluent.Rewrite.Dictionary;
using Fluent.Rewrite.Orchestration;
using Fluent.Rewrite.Profiles;
using Fluent.Rewrite.Providers;
using Fluent.Rewrite.Rewriting;
using Fluent.Rewrite.Validation;

namespace Fluent.IntegrationTests;

public sealed class CloudRewritePipelineIntegrationTests
{
    private readonly PersonalDictionaryProcessor _dictionaryProcessor = new();

    private static RewriteOrchestrator CreateOrchestrator(ICloudRewriteClient client)
    {
        SafeProfileRewriteService local = new(new ProfileRoutedRewriter(), new RewriteOutputValidator());
        return new RewriteOrchestrator(
            new LocalRewriteProvider(local),
            new CloudRewriteProvider(new GeminiRewriteProvider(client), new DeepSeekRewriteProvider(client)),
            new CloudRewriteValidator());
    }

    [Fact]
    public async Task Default_context_keeps_the_accepted_local_pipeline_result()
    {
        PersonalDictionaryEntry[] snapshot = [new PersonalDictionaryEntry("nyx voice", "Fluent")];
        const string transcript = "nyx voice fonctionne  ,vraiment";
        NeverCalledCloudClient client = new();
        RewriteOrchestrator orchestrator = CreateOrchestrator(client);

        DictionaryProcessingResult dictionaryResult = _dictionaryProcessor.Apply(transcript, snapshot);
        OrchestrationRewriteResult result = await orchestrator.RewriteAsync(
            new OrchestrationRewriteRequest(
                dictionaryResult.Text,
                RewriteProfiles.ProfessionalFrench,
                RewriteContext.LocalOnly));

        Assert.Equal(0, client.CallCount);
        Assert.Equal(RewriteProviderId.Local, result.ProviderUsed);
        Assert.Equal("Fluent fonctionne, vraiment.", result.Text);
    }

    [Fact]
    public async Task Developer_profile_still_returns_exact_text_under_the_orchestrator()
    {
        const string transcript = "dotnet build Fluent.sln -c Release";
        NeverCalledCloudClient client = new();
        RewriteOrchestrator orchestrator = CreateOrchestrator(client);

        OrchestrationRewriteResult result = await orchestrator.RewriteAsync(
            new OrchestrationRewriteRequest(
                transcript,
                RewriteProfiles.Developer,
                RewriteContext.LocalOnly));

        Assert.Equal(transcript, result.Text);
        Assert.Equal(0, client.CallCount);
    }

    private sealed class NeverCalledCloudClient : ICloudRewriteClient
    {
        public int CallCount { get; private set; }

        public Task<CloudRewriteTransportResult> SendAsync(
            CloudRewriteTransportRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(CloudRewriteTransportResult.Success("should never be used"));
        }
    }
}

public sealed class CloudRewriteSettingsTests
{
    [Fact]
    public void Default_provider_is_gemini_and_cloud_starts_disabled_without_consent()
    {
        CloudRewriteSettings settings = new();

        Assert.Equal(RewriteProviderId.Gemini, settings.SelectedProvider);
        Assert.False(settings.CloudRewriteEnabled);
        Assert.False(settings.CloudConsentGranted);
    }

    [Fact]
    public void Explicit_deepseek_selection_is_session_only_and_never_enables_cloud_or_consent()
    {
        CloudRewriteSettings settings = new();

        bool selected = settings.TrySelectProvider(RewriteProviderId.DeepSeek);

        Assert.True(selected);
        Assert.Equal(RewriteProviderId.DeepSeek, settings.SelectedProvider);
        Assert.False(settings.CloudRewriteEnabled);
        Assert.False(settings.CloudConsentGranted);
    }

    [Fact]
    public void Local_or_repeated_provider_selection_is_rejected()
    {
        CloudRewriteSettings settings = new();

        Assert.False(settings.TrySelectProvider(RewriteProviderId.Local));
        Assert.False(settings.TrySelectProvider(RewriteProviderId.Gemini));
        Assert.Equal(RewriteProviderId.Gemini, settings.SelectedProvider);
    }
}

public sealed class CloudSecretScanTests
{
    private static readonly string[] ScannedDirectories = ["src", "tests"];

    private static readonly Regex[] SecretPatterns =
    [
        new(@"GEMINI_API_KEY\s*[=:]\s*[""'][^""'\s]+[""']", RegexOptions.IgnoreCase),
        new(@"DEEPSEEK_API_KEY\s*[=:]\s*[""'][^""'\s]+[""']", RegexOptions.IgnoreCase),
        new(@"AIza[0-9A-Za-z_\-]{20,}"),
        new(@"sk-[A-Za-z0-9]{20,}")
    ];

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Fluent.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory!.FullName;
    }

    private static IEnumerable<string> SourceFiles()
    {
        string root = RepositoryRoot();
        foreach (string relative in ScannedDirectories)
        {
            string directory = Path.Combine(root, relative);
            if (!Directory.Exists(directory))
            {
                continue;
            }

            foreach (string file in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                string extension = Path.GetExtension(file);
                if (extension is ".cs" or ".csproj" or ".json" or ".xaml" or ".config" or ".props")
                {
                    yield return file;
                }
            }
        }
    }

    [Fact]
    public void No_provider_api_key_literal_exists_in_the_repository()
    {
        List<string> offenders = [];

        foreach (string file in SourceFiles())
        {
            string content = File.ReadAllText(file);
            if (SecretPatterns.Any(pattern => pattern.IsMatch(content)))
            {
                offenders.Add(file);
            }
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public void Desktop_projects_never_reference_a_provider_endpoint()
    {
        string root = RepositoryRoot();
        string[] desktopProjects =
        [
            Path.Combine(root, "src", "Fluent.App"),
            Path.Combine(root, "src", "Fluent.Cloud"),
            Path.Combine(root, "src", "Fluent.Rewrite")
        ];

        List<string> offenders = [];

        foreach (string project in desktopProjects.Where(Directory.Exists))
        {
            foreach (string file in Directory.EnumerateFiles(project, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                string content = File.ReadAllText(file);
                if (content.Contains("generativelanguage.googleapis.com", StringComparison.OrdinalIgnoreCase)
                    || content.Contains("api.deepseek.com", StringComparison.OrdinalIgnoreCase))
                {
                    offenders.Add(file);
                }
            }
        }

        Assert.Empty(offenders);
    }
}
