namespace Fluent.App.Auth;

/// <summary>
/// Public desktop configuration for Supabase Auth. The publishable key identifies the
/// Supabase project but is not a privileged credential. This type intentionally reads only
/// process environment variables; the repository .env file is never a desktop input.
/// </summary>
public sealed record SupabasePublicConfiguration(Uri ProjectUrl, string PublishableKey)
{
    public const string ProjectUrlEnvironmentVariable = "FLUENT_SUPABASE_URL";
    public const string PublishableKeyEnvironmentVariable = "FLUENT_SUPABASE_PUBLISHABLE_KEY";

    public static bool TryLoadFromEnvironment(
        out SupabasePublicConfiguration? configuration,
        out string unavailableReason)
    {
        return TryCreate(
            Environment.GetEnvironmentVariable(ProjectUrlEnvironmentVariable),
            Environment.GetEnvironmentVariable(PublishableKeyEnvironmentVariable),
            out configuration,
            out unavailableReason);
    }

    public static bool TryCreate(
        string? projectUrl,
        string? publishableKey,
        out SupabasePublicConfiguration? configuration,
        out string unavailableReason)
    {
        configuration = null;

        if (!Uri.TryCreate(projectUrl, UriKind.Absolute, out Uri? parsed)
            || parsed.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(parsed.UserInfo)
            || !string.IsNullOrEmpty(parsed.Query)
            || !string.IsNullOrEmpty(parsed.Fragment)
            || !parsed.IsDefaultPort
            || !string.Equals(parsed.AbsolutePath, "/", StringComparison.Ordinal)
            || !parsed.Host.EndsWith(".supabase.co", StringComparison.OrdinalIgnoreCase)
            || parsed.Host.Length <= ".supabase.co".Length)
        {
            unavailableReason = "La configuration publique Supabase est absente ou invalide.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(publishableKey))
        {
            unavailableReason = "La clé publique Supabase est absente.";
            return false;
        }

        Uri normalized = new(parsed.GetLeftPart(UriPartial.Authority));
        configuration = new SupabasePublicConfiguration(normalized, publishableKey.Trim());
        unavailableReason = string.Empty;
        return true;
    }

    public Uri BuildAuthUri(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        return new Uri(ProjectUrl, relativePath.TrimStart('/'));
    }
}
