using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Fluent.Rewrite.Providers;

namespace Fluent.Cloud;

/// <summary>
/// Desktop transport to the Fluent backend rewrite endpoint. It never calls a provider
/// directly and holds no provider secret; the exact model and provider key stay server-side.
/// Any transport failure maps to a fail-closed reason so the orchestrator uses local text.
/// </summary>
public sealed class BackendCloudRewriteClient : ICloudRewriteClient
{
    private readonly HttpClient _httpClient;
    private readonly Func<CloudBackendOptions> _optionsProvider;

    /// <summary>
    /// Options are resolved per call so a future authentication phase can supply a backend
    /// address and a fresh access token without recreating this client.
    /// </summary>
    public BackendCloudRewriteClient(HttpClient httpClient, Func<CloudBackendOptions> optionsProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _optionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
    }

    public BackendCloudRewriteClient(HttpClient httpClient, CloudBackendOptions options)
        : this(httpClient, () => options ?? throw new ArgumentNullException(nameof(options)))
    {
    }

    public async Task<CloudRewriteTransportResult> SendAsync(
        CloudRewriteTransportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        CloudBackendOptions options = _optionsProvider();
        if (options.BaseAddress is null)
        {
            return CloudRewriteTransportResult.Failure(RewriteFailureReason.TransportError);
        }

        using CancellationTokenSource timeoutSource = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(options.Timeout);

        try
        {
            Uri endpoint = new(options.BaseAddress, "v1/rewrite");
            using HttpRequestMessage message = new(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(new BackendRewriteRequest(
                    request.Text,
                    request.Provider.ToString().ToLowerInvariant()))
            };

            if (!string.IsNullOrWhiteSpace(options.AccessToken))
            {
                message.Headers.TryAddWithoutValidation("Authorization", $"Bearer {options.AccessToken}");
            }

            using HttpResponseMessage response = await _httpClient.SendAsync(message, timeoutSource.Token);
            if (!response.IsSuccessStatusCode)
            {
                return CloudRewriteTransportResult.Failure(RewriteFailureReason.TransportError);
            }

            BackendRewriteResponse? payload = await response.Content
                .ReadFromJsonAsync<BackendRewriteResponse>(timeoutSource.Token);

            if (payload is null || string.IsNullOrWhiteSpace(payload.Text))
            {
                return CloudRewriteTransportResult.Failure(RewriteFailureReason.InvalidResponse);
            }

            return CloudRewriteTransportResult.Success(payload.Text);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return CloudRewriteTransportResult.Failure(RewriteFailureReason.Timeout);
        }
        catch (HttpRequestException)
        {
            return CloudRewriteTransportResult.Failure(RewriteFailureReason.TransportError);
        }
        catch (Exception)
        {
            return CloudRewriteTransportResult.Failure(RewriteFailureReason.TransportError);
        }
    }

    private sealed record BackendRewriteRequest(
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("provider")] string Provider);

    private sealed record BackendRewriteResponse(
        [property: JsonPropertyName("text")] string? Text,
        [property: JsonPropertyName("provider")] string? Provider);
}
