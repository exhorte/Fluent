namespace Fluent.App.Cloud;

/// <summary>
/// Public Desktop configuration for the Fluent backend origin. It deliberately accepts only
/// a process environment variable and never loads a repository .env file. The origin is not
/// a provider endpoint or credential; provider keys remain server-side.
/// </summary>
public sealed record FluentBackendPublicConfiguration(Uri Origin)
{
    public const string BackendUrlEnvironmentVariable = "FLUENT_BACKEND_URL";

    public static bool TryLoadFromEnvironment(
        out FluentBackendPublicConfiguration? configuration,
        out string unavailableReason)
    {
        return TryCreate(
            Environment.GetEnvironmentVariable(BackendUrlEnvironmentVariable),
            out configuration,
            out unavailableReason);
    }

    public static bool TryCreate(
        string? backendUrl,
        out FluentBackendPublicConfiguration? configuration,
        out string unavailableReason)
    {
        configuration = null;

        if (!Uri.TryCreate(backendUrl, UriKind.Absolute, out Uri? parsed)
            || parsed.Scheme != Uri.UriSchemeHttps
            || string.IsNullOrWhiteSpace(parsed.Host)
            || !string.IsNullOrEmpty(parsed.UserInfo)
            || !string.IsNullOrEmpty(parsed.Query)
            || !string.IsNullOrEmpty(parsed.Fragment)
            || !parsed.IsDefaultPort
            || !string.Equals(parsed.AbsolutePath, "/", StringComparison.Ordinal))
        {
            unavailableReason = "Le backend Cloud n’est pas configuré ou son origine publique est invalide.";
            return false;
        }

        Uri normalized = new(parsed.GetLeftPart(UriPartial.Authority));
        configuration = new FluentBackendPublicConfiguration(normalized);
        unavailableReason = string.Empty;
        return true;
    }
}
