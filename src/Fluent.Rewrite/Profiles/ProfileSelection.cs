namespace Fluent.Rewrite.Profiles;

/// <summary>
/// Session-only rewrite-profile selection. Nothing is persisted or logged;
/// every launch starts with the canonical default profile.
/// Not thread-safe: read and mutate from the UI thread only.
/// </summary>
public sealed class ProfileSelection
{
    private RewriteProfile _current = RewriteProfiles.Default;

    public RewriteProfile Current => _current;

    public ProfileSelectionResult TrySelect(string? profileId)
    {
        if (!RewriteProfiles.TryGetAvailable(profileId, out RewriteProfile profile))
        {
            return new ProfileSelectionResult(
                false,
                _current,
                "Profil inconnu ou indisponible. La sélection actuelle est conservée.");
        }

        _current = profile;
        return new ProfileSelectionResult(
            true,
            profile,
            $"Profil {profile.DisplayName} sélectionné pour cette session.");
    }
}
