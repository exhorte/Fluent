using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Fluent.Backend.Auth;

public enum SupabaseJwtValidationStatus
{
    Valid,
    Invalid,
    Forbidden,
    Unavailable
}

public sealed record SupabaseJwtValidationResult(SupabaseJwtValidationStatus Status, string? UserId)
{
    public static SupabaseJwtValidationResult Valid(string userId) => new(SupabaseJwtValidationStatus.Valid, userId);

    public static SupabaseJwtValidationResult Invalid() => new(SupabaseJwtValidationStatus.Invalid, null);

    public static SupabaseJwtValidationResult Forbidden() => new(SupabaseJwtValidationStatus.Forbidden, null);

    public static SupabaseJwtValidationResult Unavailable() => new(SupabaseJwtValidationStatus.Unavailable, null);
}

public interface ISupabaseJwtValidator
{
    Task<SupabaseJwtValidationResult> ValidateAsync(string? authorizationHeader, CancellationToken cancellationToken);
}

/// <summary>
/// Validates only asymmetric Supabase access tokens. The metadata endpoint and JWKS URI are
/// derived from the server's mandatory trusted issuer configuration and are never taken from a
/// bearer token. A missing or unusable verifier fails closed as service unavailable.
/// </summary>
public sealed class SupabaseJwtValidator : ISupabaseJwtValidator
{
    public const string IssuerEnvironmentVariable = "FLUENT_BACKEND_SUPABASE_ISSUER";
    public const string AudienceEnvironmentVariable = "FLUENT_BACKEND_SUPABASE_AUDIENCE";
    private static readonly string[] AllowedAlgorithms = [SecurityAlgorithms.RsaSha256, SecurityAlgorithms.EcdsaSha256];
    private readonly TrustedSupabaseAuthority? _authority;
    private readonly HttpClient? _jwksHttpClient;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private ConfigurationManager<OpenIdConnectConfiguration>? _configurationManager;
    private long _refreshGeneration;
    private readonly JwtSecurityTokenHandler _tokenHandler = new() { MapInboundClaims = false };

    public SupabaseJwtValidator(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(httpClientFactory);

        if (!TrustedSupabaseAuthority.TryCreate(
                configuration[IssuerEnvironmentVariable],
                configuration[AudienceEnvironmentVariable],
                out TrustedSupabaseAuthority? authority))
        {
            return;
        }

        TrustedSupabaseAuthority trustedAuthority = authority!;
        _authority = trustedAuthority;
        _jwksHttpClient = httpClientFactory.CreateClient("supabase-jwks");
        _configurationManager = CreateConfigurationManager(trustedAuthority, _jwksHttpClient);
    }

    public async Task<SupabaseJwtValidationResult> ValidateAsync(
        string? authorizationHeader,
        CancellationToken cancellationToken)
    {
        if (_authority is null || _configurationManager is null)
        {
            return SupabaseJwtValidationResult.Unavailable();
        }

        if (!TryExtractBearerToken(authorizationHeader, out string? token)
            || token is null
            || !HasAllowedAlgorithm(token))
        {
            return SupabaseJwtValidationResult.Invalid();
        }

        ConfigurationManager<OpenIdConnectConfiguration>? manager = Volatile.Read(ref _configurationManager);
        if (manager is null)
        {
            return SupabaseJwtValidationResult.Unavailable();
        }

        long refreshGeneration = Volatile.Read(ref _refreshGeneration);
        return await ValidateWithConfigurationAsync(
            token!,
            manager,
            refreshOnKeyMiss: true,
            refreshGeneration,
            cancellationToken);
    }

    private async Task<SupabaseJwtValidationResult> ValidateWithConfigurationAsync(
        string token,
        ConfigurationManager<OpenIdConnectConfiguration> manager,
        bool refreshOnKeyMiss,
        long refreshGeneration,
        CancellationToken cancellationToken)
    {
        OpenIdConnectConfiguration openIdConfiguration;
        try
        {
            openIdConfiguration = await manager.GetConfigurationAsync(cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return SupabaseJwtValidationResult.Unavailable();
        }

        if (!IsExpectedConfiguration(openIdConfiguration, _authority!))
        {
            return SupabaseJwtValidationResult.Unavailable();
        }

        JwtSecurityToken parsed;
        try
        {
            parsed = _tokenHandler.ReadJwtToken(token);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return SupabaseJwtValidationResult.Invalid();
        }

        if (string.IsNullOrWhiteSpace(parsed.Header.Kid)
            || !openIdConfiguration.SigningKeys.Any(key =>
                string.Equals(key.KeyId, parsed.Header.Kid, StringComparison.Ordinal)))
        {
            if (!refreshOnKeyMiss)
            {
                return SupabaseJwtValidationResult.Invalid();
            }

            return await RefreshAndValidateAsync(token, refreshGeneration, cancellationToken);
        }

        TokenValidationParameters parameters = new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = openIdConfiguration.SigningKeys,
            RequireSignedTokens = true,
            ValidAlgorithms = AllowedAlgorithms,
            ValidateIssuer = true,
            ValidIssuer = _authority!.Issuer.AbsoluteUri,
            ValidateAudience = true,
            ValidAudience = _authority.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        try
        {
            System.Security.Claims.ClaimsPrincipal principal = _tokenHandler.ValidateToken(token, parameters, out _);
            string? subject = principal.FindFirst("sub")?.Value;
            if (!Guid.TryParse(subject, out Guid userId))
            {
                return SupabaseJwtValidationResult.Invalid();
            }

            if (!string.Equals(principal.FindFirst("role")?.Value, "authenticated", StringComparison.Ordinal))
            {
                return SupabaseJwtValidationResult.Forbidden();
            }

            return SupabaseJwtValidationResult.Valid(userId.ToString("D"));
        }
        catch (SecurityTokenSignatureKeyNotFoundException) when (refreshOnKeyMiss)
        {
            return await RefreshAndValidateAsync(token, refreshGeneration, cancellationToken);
        }
        catch (SecurityTokenException)
        {
            return SupabaseJwtValidationResult.Invalid();
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return SupabaseJwtValidationResult.Invalid();
        }
    }

    private bool HasAllowedAlgorithm(string token)
    {
        try
        {
            JwtSecurityToken parsed = _tokenHandler.ReadJwtToken(token);
            return !string.IsNullOrWhiteSpace(parsed.Header.Alg)
                && AllowedAlgorithms.Contains(parsed.Header.Alg, StringComparer.Ordinal);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<SupabaseJwtValidationResult> RefreshAndValidateAsync(
        string token,
        long observedRefreshGeneration,
        CancellationToken cancellationToken)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (_authority is null || _jwksHttpClient is null)
            {
                return SupabaseJwtValidationResult.Unavailable();
            }

            long currentRefreshGeneration = Volatile.Read(ref _refreshGeneration);
            if (currentRefreshGeneration != observedRefreshGeneration)
            {
                ConfigurationManager<OpenIdConnectConfiguration>? currentManager =
                    Volatile.Read(ref _configurationManager);
                return currentManager is null
                    ? SupabaseJwtValidationResult.Unavailable()
                    : await ValidateWithConfigurationAsync(
                        token,
                        currentManager,
                        refreshOnKeyMiss: false,
                        currentRefreshGeneration,
                        cancellationToken);
            }

            Interlocked.Increment(ref _refreshGeneration);
            try
            {
                // A fresh manager tied to the same pinned authority performs this one
                // security-triggered fetch without accepting a key absent from the refreshed JWKS.
                ConfigurationManager<OpenIdConnectConfiguration> refreshedManager =
                    CreateConfigurationManager(_authority, _jwksHttpClient);
                SupabaseJwtValidationResult result = await ValidateWithConfigurationAsync(
                    token,
                    refreshedManager,
                    refreshOnKeyMiss: false,
                    Volatile.Read(ref _refreshGeneration),
                    cancellationToken);
                if (result.Status != SupabaseJwtValidationStatus.Unavailable)
                {
                    Volatile.Write(ref _configurationManager, refreshedManager);
                }

                return result;
            }
            finally
            {
                Interlocked.Increment(ref _refreshGeneration);
            }
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static ConfigurationManager<OpenIdConnectConfiguration> CreateConfigurationManager(
        TrustedSupabaseAuthority authority,
        HttpClient httpClient)
    {
        HttpDocumentRetriever retriever = new(httpClient) { RequireHttps = true };
        return new ConfigurationManager<OpenIdConnectConfiguration>(
            authority.MetadataAddress.AbsoluteUri,
            new OpenIdConnectConfigurationRetriever(),
            retriever)
        {
            AutomaticRefreshInterval = TimeSpan.FromHours(12),
            RefreshInterval = TimeSpan.FromMinutes(5)
        };
    }

    private static bool TryExtractBearerToken(string? authorizationHeader, out string? token)
    {
        const string Prefix = "Bearer ";
        token = null;
        if (string.IsNullOrWhiteSpace(authorizationHeader)
            || !authorizationHeader.StartsWith(Prefix, StringComparison.Ordinal)
            || authorizationHeader.Length <= Prefix.Length)
        {
            return false;
        }

        token = authorizationHeader[Prefix.Length..].Trim();
        return token.Length > 0 && !token.Contains(' ');
    }

    private static bool IsExpectedConfiguration(
        OpenIdConnectConfiguration configuration,
        TrustedSupabaseAuthority authority)
    {
        return string.Equals(configuration.Issuer, authority.Issuer.AbsoluteUri, StringComparison.Ordinal)
            && Uri.TryCreate(configuration.JwksUri, UriKind.Absolute, out Uri? jwksUri)
            && Uri.Compare(jwksUri, authority.JwksAddress, UriComponents.AbsoluteUri, UriFormat.SafeUnescaped,
                StringComparison.Ordinal) == 0;
    }

    private sealed record TrustedSupabaseAuthority(Uri Issuer, string Audience, Uri MetadataAddress, Uri JwksAddress)
    {
        public static bool TryCreate(
            string? issuerValue,
            string? audienceValue,
            out TrustedSupabaseAuthority? authority)
        {
            authority = null;
            if (!string.Equals(audienceValue, "authenticated", StringComparison.Ordinal)
                || !Uri.TryCreate(issuerValue, UriKind.Absolute, out Uri? issuer)
                || issuer.Scheme != Uri.UriSchemeHttps
                || !issuer.IsDefaultPort
                || !string.IsNullOrEmpty(issuer.UserInfo)
                || !string.IsNullOrEmpty(issuer.Query)
                || !string.IsNullOrEmpty(issuer.Fragment)
                || !issuer.Host.EndsWith(".supabase.co", StringComparison.OrdinalIgnoreCase)
                || issuer.Host.Length <= ".supabase.co".Length
                || !string.Equals(issuer.AbsolutePath.TrimEnd('/'), "/auth/v1", StringComparison.Ordinal))
            {
                return false;
            }

            Uri canonicalIssuer = new(issuer.AbsoluteUri.TrimEnd('/'));
            string audience = audienceValue!;
            authority = new TrustedSupabaseAuthority(
                canonicalIssuer,
                audience,
                new Uri(canonicalIssuer.AbsoluteUri + "/.well-known/openid-configuration"),
                new Uri(canonicalIssuer.AbsoluteUri + "/.well-known/jwks.json"));
            return true;
        }
    }
}
