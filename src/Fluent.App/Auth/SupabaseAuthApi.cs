using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Fluent.App.Auth;

public sealed record AuthenticatedUser(string Id, string? Email, string? DisplayName);

public sealed record SupabaseSessionPayload(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    AuthenticatedUser User);

public enum SupabaseAuthFailureKind
{
    Rejected,
    Unavailable
}

public sealed class SupabaseAuthException : Exception
{
    public SupabaseAuthException(SupabaseAuthFailureKind kind)
        : base(kind.ToString())
    {
        Kind = kind;
    }

    public SupabaseAuthFailureKind Kind { get; }
}

public interface ISupabaseAuthApi
{
    Task<SupabaseSessionPayload> ExchangeCodeAsync(
        SupabasePublicConfiguration configuration,
        string authorizationCode,
        string verifier,
        CancellationToken cancellationToken);

    Task<SupabaseSessionPayload> RefreshAsync(
        SupabasePublicConfiguration configuration,
        string refreshToken,
        CancellationToken cancellationToken);
}

/// <summary>
/// Minimal transport to Supabase Auth. It carries only the project's public key and a user
/// refresh token during refresh; no provider or OAuth confidential-client secret exists here.
/// </summary>
internal sealed class SupabaseAuthApi : ISupabaseAuthApi
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);
    private readonly HttpClient _httpClient;

    public SupabaseAuthApi(HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public Task<SupabaseSessionPayload> ExchangeCodeAsync(
        SupabasePublicConfiguration configuration,
        string authorizationCode,
        string verifier,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(authorizationCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(verifier);

        return SendSessionRequestAsync(
            configuration,
            "auth/v1/token?grant_type=pkce",
            new CodeExchangeRequest(authorizationCode, verifier),
            cancellationToken);
    }

    public Task<SupabaseSessionPayload> RefreshAsync(
        SupabasePublicConfiguration configuration,
        string refreshToken,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        return SendSessionRequestAsync(
            configuration,
            "auth/v1/token?grant_type=refresh_token",
            new RefreshRequest(refreshToken),
            cancellationToken);
    }

    private async Task<SupabaseSessionPayload> SendSessionRequestAsync(
        SupabasePublicConfiguration configuration,
        string endpoint,
        object body,
        CancellationToken cancellationToken)
    {
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(RequestTimeout);

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Post, configuration.BuildAuthUri(endpoint))
            {
                Content = JsonContent.Create(body)
            };
            request.Headers.TryAddWithoutValidation("apikey", configuration.PublishableKey);

            using HttpResponseMessage response = await _httpClient.SendAsync(request, timeout.Token);
            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                throw new SupabaseAuthException(SupabaseAuthFailureKind.Rejected);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new SupabaseAuthException(SupabaseAuthFailureKind.Unavailable);
            }

            SessionResponse? payload = await response.Content.ReadFromJsonAsync<SessionResponse>(timeout.Token);
            if (payload is null
                || string.IsNullOrWhiteSpace(payload.AccessToken)
                || string.IsNullOrWhiteSpace(payload.RefreshToken)
                || string.IsNullOrWhiteSpace(payload.User?.Id))
            {
                throw new SupabaseAuthException(SupabaseAuthFailureKind.Rejected);
            }

            DateTimeOffset expiresAt = payload.ExpiresAt is long unixSeconds
                ? DateTimeOffset.FromUnixTimeSeconds(unixSeconds)
                : DateTimeOffset.UtcNow.AddSeconds(payload.ExpiresIn.GetValueOrDefault());
            if (expiresAt <= DateTimeOffset.UtcNow)
            {
                throw new SupabaseAuthException(SupabaseAuthFailureKind.Rejected);
            }

            string? displayName = payload.User.UserMetadata?.FullName ?? payload.User.UserMetadata?.Name;
            return new SupabaseSessionPayload(
                payload.AccessToken,
                payload.RefreshToken,
                expiresAt,
                new AuthenticatedUser(payload.User.Id, payload.User.Email, displayName));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw new SupabaseAuthException(SupabaseAuthFailureKind.Unavailable);
        }
        catch (HttpRequestException)
        {
            throw new SupabaseAuthException(SupabaseAuthFailureKind.Unavailable);
        }
    }

    private sealed record CodeExchangeRequest(
        [property: JsonPropertyName("auth_code")] string AuthorizationCode,
        [property: JsonPropertyName("code_verifier")] string Verifier);

    private sealed record RefreshRequest(
        [property: JsonPropertyName("refresh_token")] string RefreshToken);

    private sealed record SessionResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_at")] long? ExpiresAt,
        [property: JsonPropertyName("expires_in")] long? ExpiresIn,
        [property: JsonPropertyName("user")] UserResponse? User);

    private sealed record UserResponse(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("email")] string? Email,
        [property: JsonPropertyName("user_metadata")] UserMetadataResponse? UserMetadata);

    private sealed record UserMetadataResponse(
        [property: JsonPropertyName("full_name")] string? FullName,
        [property: JsonPropertyName("name")] string? Name);
}
