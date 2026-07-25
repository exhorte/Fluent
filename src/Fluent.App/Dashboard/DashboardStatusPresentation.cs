using Fluent.App.Auth;
using Fluent.Rewrite.Dictionary;
using Fluent.Rewrite.Providers;

namespace Fluent.App.Dashboard;

/// <summary>
/// Formats only ephemeral, already-available dashboard state. It never reads storage,
/// contacts a service, or receives user content, account identity, or credentials.
/// </summary>
public sealed record DashboardStatusInput(
    string ProfileDisplayName,
    int DictionaryEntryCount,
    DictionaryStorageMode DictionaryStorageMode,
    AuthenticationStatus AuthenticationStatus,
    bool IsAuthenticated,
    bool HasConfiguredBackendOrigin,
    bool IsCloudEnabled,
    bool HasCloudConsent,
    RewriteProviderId SelectedProvider)
{
    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ProfileDisplayName);

        if (DictionaryEntryCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(DictionaryEntryCount),
                DictionaryEntryCount,
                "Le nombre d’entrées du dictionnaire ne peut pas être négatif.");
        }
    }
}

public sealed record DashboardStatusPresentation(
    string ProfileSummary,
    string DictionarySummary,
    string AuthenticationSummary,
    string CloudSummary);

public static class DashboardStatusPresenter
{
    public static DashboardStatusPresentation Create(
        DashboardStatusInput input,
        string language = "fr")
    {
        ArgumentNullException.ThrowIfNull(input);
        input.Validate();
        bool fr = language != "en";

        string profile = fr ? "Profil" : "Profile";
        return new DashboardStatusPresentation(
            $"{profile} · {input.ProfileDisplayName}",
            DescribeDictionary(input.DictionaryStorageMode, input.DictionaryEntryCount, fr),
            DescribeAuthentication(input.AuthenticationStatus, fr),
            DescribeCloud(input, fr));
    }

    private static string DescribeDictionary(
        DictionaryStorageMode storageMode,
        int entryCount,
        bool fr)
    {
        return storageMode switch
        {
            DictionaryStorageMode.Loading =>
                fr ? "Dictionnaire · Chargement" : "Dictionary · Loading",
            DictionaryStorageMode.Persistent when entryCount == 0 =>
                fr ? "Dictionnaire · Vide (local)" : "Dictionary · Empty (local)",
            DictionaryStorageMode.Persistent when entryCount == 1 =>
                fr ? "Dictionnaire · 1 entrée locale" : "Dictionary · 1 local entry",
            DictionaryStorageMode.Persistent =>
                fr ? $"Dictionnaire · {entryCount} entrées locales"
                   : $"Dictionary · {entryCount} local entries",
            DictionaryStorageMode.SessionOnlyFallback when entryCount == 0 =>
                fr ? "Dictionnaire · Vide (secours)" : "Dictionary · Empty (fallback)",
            DictionaryStorageMode.SessionOnlyFallback when entryCount == 1 =>
                fr ? "Dictionnaire · 1 entrée de secours" : "Dictionary · 1 fallback entry",
            DictionaryStorageMode.SessionOnlyFallback =>
                fr ? $"Dictionnaire · {entryCount} entrées de secours"
                   : $"Dictionary · {entryCount} fallback entries",
            _ => fr ? "Dictionnaire · Indisponible" : "Dictionary · Unavailable"
        };
    }

    private static string DescribeAuthentication(AuthenticationStatus status, bool fr)
    {
        return status switch
        {
            AuthenticationStatus.Unconfigured => fr ? "Compte · Non configuré" : "Account · Not configured",
            AuthenticationStatus.SignedOut => fr ? "Compte · Déconnecté" : "Account · Signed out",
            AuthenticationStatus.SigningIn => fr ? "Compte · Connexion en cours" : "Account · Signing in",
            AuthenticationStatus.Authenticated => fr ? "Compte · Connecté" : "Account · Signed in",
            AuthenticationStatus.Offline => fr ? "Compte · Hors ligne" : "Account · Offline",
            AuthenticationStatus.Expired => fr ? "Compte · Session expirée" : "Account · Session expired",
            AuthenticationStatus.Cancelled => fr ? "Compte · Connexion annulée" : "Account · Sign-in cancelled",
            AuthenticationStatus.Failed => fr ? "Compte · Échec de connexion" : "Account · Sign-in failed",
            _ => fr ? "Compte · État inconnu" : "Account · Unknown state"
        };
    }

    private static string DescribeCloud(DashboardStatusInput input, bool fr)
    {
        if (!input.HasConfiguredBackendOrigin)
        {
            return fr ? "Cloud · Non configuré" : "Cloud · Not configured";
        }

        if (!input.IsAuthenticated)
        {
            return fr ? "Cloud · Local (connexion requise)" : "Cloud · Local (sign-in required)";
        }

        if (!input.IsCloudEnabled)
        {
            return fr ? "Cloud · Désactivé" : "Cloud · Disabled";
        }

        if (!input.HasCloudConsent)
        {
            return fr ? "Cloud · Consentement requis" : "Cloud · Consent required";
        }

        return input.SelectedProvider switch
        {
            RewriteProviderId.Gemini =>
                fr ? "Cloud · Autorisé localement (Gemini)" : "Cloud · Locally authorized (Gemini)",
            RewriteProviderId.DeepSeek =>
                fr ? "Cloud · Autorisé localement (DeepSeek)" : "Cloud · Locally authorized (DeepSeek)",
            _ => fr ? "Cloud · Autorisé localement" : "Cloud · Locally authorized"
        };
    }
}
