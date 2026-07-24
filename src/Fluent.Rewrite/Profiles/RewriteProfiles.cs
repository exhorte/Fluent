namespace Fluent.Rewrite.Profiles;

public static class RewriteProfiles
{
    public static RewriteProfile ProfessionalFrench { get; } = new(
        "professional-fr",
        "Français professionnel",
        "Corriger uniquement l'espacement et la ponctuation sans ajouter, supprimer, modifier ou réordonner les mots.",
        IsAvailable: true,
        Description: "Normalise localement l'espacement et la ponctuation sans jamais modifier les mots dictés.");

    public static RewriteProfile Developer { get; } = new(
        "developer",
        "Développeur",
        "Restituer exactement le texte source après dictionnaire, sans aucune modification.",
        IsAvailable: true,
        Description: "Conserve le texte exact : code, commandes, chemins, URL, versions, nombres et identifiants restent intacts.");

    public static RewriteProfile SimplifiedFrench { get; } = new(
        "simplified-fr",
        "Français simplifié",
        "Indisponible : aucune simplification sémantique locale sûre n'existe encore.",
        IsAvailable: false,
        Description: "Simplifiera le vocabulaire sans inventer d'information.",
        UnavailableReason: "Indisponible tant qu'aucun moteur sémantique local sûr n'existe.");

    public static RewriteProfile Default => ProfessionalFrench;

    public static IReadOnlyList<RewriteProfile> All { get; } =
    [
        ProfessionalFrench,
        Developer,
        SimplifiedFrench
    ];

    public static bool IsCanonicalAvailable(RewriteProfile? profile)
    {
        if (profile is null || !profile.IsAvailable)
        {
            return false;
        }

        foreach (RewriteProfile candidate in All)
        {
            if (ReferenceEquals(candidate, profile))
            {
                return true;
            }
        }

        return false;
    }

    public static bool TryGetAvailable(string? id, out RewriteProfile profile)
    {
        foreach (RewriteProfile candidate in All)
        {
            if (candidate.IsAvailable && string.Equals(candidate.Id, id, StringComparison.Ordinal))
            {
                profile = candidate;
                return true;
            }
        }

        profile = Default;
        return false;
    }
}
