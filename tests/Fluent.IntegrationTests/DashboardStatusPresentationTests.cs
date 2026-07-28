using Fluent.App.Auth;
using Fluent.App.Dashboard;
using Fluent.Rewrite.Dictionary;
using Fluent.Rewrite.Providers;

namespace Fluent.IntegrationTests;

public sealed class DashboardStatusPresentationTests
{
    [Theory]
    [InlineData(DictionaryStorageMode.Loading, 0, "Dictionnaire · Chargement")]
    [InlineData(DictionaryStorageMode.Persistent, 0, "Dictionnaire · Vide (local)")]
    [InlineData(DictionaryStorageMode.Persistent, 1, "Dictionnaire · 1 entrée locale")]
    [InlineData(DictionaryStorageMode.Persistent, 2, "Dictionnaire · 2 entrées locales")]
    [InlineData(DictionaryStorageMode.SessionOnlyFallback, 0, "Dictionnaire · Vide (secours)")]
    [InlineData(DictionaryStorageMode.SessionOnlyFallback, 1, "Dictionnaire · 1 entrée de secours")]
    [InlineData(DictionaryStorageMode.SessionOnlyFallback, 2, "Dictionnaire · 2 entrées de secours")]
    public void Dictionary_summary_distinguishes_loading_empty_persistent_and_fallback(
        DictionaryStorageMode storageMode,
        int entryCount,
        string expected)
    {
        DashboardStatusPresentation presentation = DashboardStatusPresenter.Create(
            CreateInput(dictionaryStorageMode: storageMode, dictionaryEntryCount: entryCount));

        Assert.Equal(expected, presentation.DictionarySummary);
    }

    [Theory]
    [InlineData(AuthenticationStatus.Unconfigured, "Compte · Non configuré")]
    [InlineData(AuthenticationStatus.SignedOut, "Compte · Déconnecté")]
    [InlineData(AuthenticationStatus.SigningIn, "Compte · Connexion en cours")]
    [InlineData(AuthenticationStatus.Authenticated, "Compte · Connecté")]
    [InlineData(AuthenticationStatus.Offline, "Compte · Hors ligne")]
    [InlineData(AuthenticationStatus.Expired, "Compte · Session expirée")]
    [InlineData(AuthenticationStatus.Cancelled, "Compte · Connexion annulée")]
    [InlineData(AuthenticationStatus.Failed, "Compte · Échec de connexion")]
    public void Authentication_summary_distinguishes_each_existing_session_state(
        AuthenticationStatus status,
        string expected)
    {
        DashboardStatusPresentation presentation = DashboardStatusPresenter.Create(
            CreateInput(authenticationStatus: status));

        Assert.Equal(expected, presentation.AuthenticationSummary);
    }

    [Theory]
    [InlineData(false, true, true, true, RewriteProviderId.Gemini, "Cloud · Non configuré")]
    [InlineData(true, false, true, true, RewriteProviderId.Gemini, "Cloud · Local (connexion requise)")]
    [InlineData(true, true, false, true, RewriteProviderId.Gemini, "Cloud · Désactivé")]
    [InlineData(true, true, true, false, RewriteProviderId.Gemini, "Cloud · Consentement requis")]
    [InlineData(true, true, true, true, RewriteProviderId.Gemini, "Cloud · Autorisé localement (Gemini)")]
    [InlineData(true, true, true, true, RewriteProviderId.DeepSeek, "Cloud · Autorisé localement (DeepSeek)")]
    [InlineData(true, true, true, true, RewriteProviderId.Local, "Cloud · Autorisé localement")]
    public void Cloud_summary_uses_local_permission_precedence_without_claiming_reachability(
        bool hasConfiguredBackendOrigin,
        bool isAuthenticated,
        bool isCloudEnabled,
        bool hasCloudConsent,
        RewriteProviderId selectedProvider,
        string expected)
    {
        DashboardStatusPresentation presentation = DashboardStatusPresenter.Create(
            CreateInput(
                hasConfiguredBackendOrigin: hasConfiguredBackendOrigin,
                isAuthenticated: isAuthenticated,
                isCloudEnabled: isCloudEnabled,
                hasCloudConsent: hasCloudConsent,
                selectedProvider: selectedProvider));

        Assert.Equal(expected, presentation.CloudSummary);
        Assert.DoesNotContain("disponible", presentation.CloudSummary, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("connecté", presentation.CloudSummary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Profile_summary_uses_only_the_current_local_profile_name()
    {
        DashboardStatusPresentation presentation = DashboardStatusPresenter.Create(
            CreateInput(profileDisplayName: "Développeur"));

        Assert.Equal("Profil · Développeur", presentation.ProfileSummary);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Empty_profile_name_is_rejected(string profileDisplayName)
    {
        Assert.Throws<ArgumentException>(() => DashboardStatusPresenter.Create(
            CreateInput(profileDisplayName: profileDisplayName)));
    }

    [Fact]
    public void Negative_dictionary_entry_count_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DashboardStatusPresenter.Create(
            CreateInput(dictionaryEntryCount: -1)));
    }

    [Fact]
    public void Presentation_mapper_has_no_storage_network_or_identity_access()
    {
        string source = File.ReadAllText(SourcePath("src", "Fluent.App", "Dashboard", "DashboardStatusPresentation.cs"));

        string[] forbidden =
        [
            "File.",
            "Http",
            "AccessToken",
            "AuthenticatedUser",
            "GetEnvironmentVariable",
            "Sqlite",
            "Registry",
            "Clipboard"
        ];

        foreach (string token in forbidden)
        {
            Assert.DoesNotContain(token, source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Overview_uses_the_pure_mapper_and_exposes_only_status_summaries()
    {
        string codeBehind = File.ReadAllText(SourcePath("src", "Fluent.App", "MainWindow.xaml.cs"));
        string markup = File.ReadAllText(SourcePath("src", "Fluent.App", "MainWindow.xaml"));

        Assert.Contains("DashboardStatusPresenter.Create", codeBehind, StringComparison.Ordinal);
        Assert.Contains("AuthenticationSummaryText", markup, StringComparison.Ordinal);
        Assert.Contains("CloudSummaryText", markup, StringComparison.Ordinal);
        Assert.Contains("Historique · local opt-in", markup, StringComparison.Ordinal);
    }

    private static DashboardStatusInput CreateInput(
        string profileDisplayName = "Français professionnel",
        int dictionaryEntryCount = 0,
        DictionaryStorageMode dictionaryStorageMode = DictionaryStorageMode.Loading,
        AuthenticationStatus authenticationStatus = AuthenticationStatus.SignedOut,
        bool isAuthenticated = false,
        bool hasConfiguredBackendOrigin = false,
        bool isCloudEnabled = false,
        bool hasCloudConsent = false,
        RewriteProviderId selectedProvider = RewriteProviderId.Gemini)
    {
        return new DashboardStatusInput(
            profileDisplayName,
            dictionaryEntryCount,
            dictionaryStorageMode,
            authenticationStatus,
            isAuthenticated,
            hasConfiguredBackendOrigin,
            isCloudEnabled,
            hasCloudConsent,
            selectedProvider);
    }

    private static string SourcePath(params string[] segments)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Fluent.sln")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return Path.Combine([directory!.FullName, .. segments]);
    }
}
