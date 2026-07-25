namespace Fluent.Core.Settings;

/// <summary>
/// Local persistence boundary for reversible application preferences.
/// Implementations live outside <c>Fluent.Core</c>. No secret is stored.
/// </summary>
public interface IAppSettingsStore
{
    /// <summary>Ensures the store exists and returns the current preferences.</summary>
    Task<AppPreferences> InitializeAndLoadAsync(
        CancellationToken cancellationToken);

    /// <summary>
    /// Persists the preferred rewrite-profile identifier, or clears it when null.
    /// </summary>
    Task SetPreferredProfileAsync(
        string? profileId,
        CancellationToken cancellationToken);

    /// <summary>Persists the application language (normalised to a supported code).</summary>
    Task SetLanguageAsync(
        string language,
        CancellationToken cancellationToken);
}
