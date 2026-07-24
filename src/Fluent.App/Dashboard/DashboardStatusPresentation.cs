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
    public static DashboardStatusPresentation Create(DashboardStatusInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        input.Validate();

        return new DashboardStatusPresentation(
            $"Profil · {input.ProfileDisplayName}",
            DescribeDictionary(input.DictionaryStorageMode, input.DictionaryEntryCount),
            DescribeAuthentication(input.AuthenticationStatus),
            DescribeCloud(input));
    }

    private static string DescribeDictionary(DictionaryStorageMode storageMode, int entryCount)
    {
        return storageMode switch
        {
            DictionaryStorageMode.Loading => "Dictionnaire · Chargement",
            DictionaryStorageMode.Persistent when entryCount == 0 => "Dictionnaire · Vide (local)",
            DictionaryStorageMode.Persistent when entryCount == 1 => "Dictionnaire · 1 entrée locale",
            DictionaryStorageMode.Persistent => $"Dictionnaire · {entryCount} entrées locales",
            DictionaryStorageMode.SessionOnlyFallback when entryCount == 0 =>
                "Dictionnaire · Vide (secours)",
            DictionaryStorageMode.SessionOnlyFallback when entryCount == 1 =>
                "Dictionnaire · 1 entrée de secours",
            DictionaryStorageMode.SessionOnlyFallback =>
                $"Dictionnaire · {entryCount} entrées de secours",
            _ => "Dictionnaire · Indisponible"
        };
    }

    private static string DescribeAuthentication(AuthenticationStatus status)
    {
        return status switch
        {
            AuthenticationStatus.Unconfigured => "Compte · Non configuré",
            AuthenticationStatus.SignedOut => "Compte · Déconnecté",
            AuthenticationStatus.SigningIn => "Compte · Connexion en cours",
            AuthenticationStatus.Authenticated => "Compte · Connecté",
            AuthenticationStatus.Offline => "Compte · Hors ligne",
            AuthenticationStatus.Expired => "Compte · Session expirée",
            AuthenticationStatus.Cancelled => "Compte · Connexion annulée",
            AuthenticationStatus.Failed => "Compte · Échec de connexion",
            _ => "Compte · État inconnu"
        };
    }

    private static string DescribeCloud(DashboardStatusInput input)
    {
        if (!input.HasConfiguredBackendOrigin)
        {
            return "Cloud · Non configuré";
        }

        if (!input.IsAuthenticated)
        {
            return "Cloud · Local (connexion requise)";
        }

        if (!input.IsCloudEnabled)
        {
            return "Cloud · Désactivé";
        }

        if (!input.HasCloudConsent)
        {
            return "Cloud · Consentement requis";
        }

        return input.SelectedProvider switch
        {
            RewriteProviderId.Gemini => "Cloud · Autorisé localement (Gemini)",
            RewriteProviderId.DeepSeek => "Cloud · Autorisé localement (DeepSeek)",
            _ => "Cloud · Autorisé localement"
        };
    }
}
