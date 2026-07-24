using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace Fluent.Backend.Rewriting;

/// <summary>
/// Live Gemini transport. Model and API key are read only from server configuration and are
/// never exposed to the client. This type performs the outbound provider call; it is not
/// exercised by automated tests, which substitute a fake <see cref="IGeminiApi"/>.
/// </summary>
public sealed class HttpGeminiApi : IGeminiApi
{
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models/";
    private readonly HttpClient _httpClient;
    private readonly string? _model;
    private readonly string? _apiKey;

    public HttpGeminiApi(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ArgumentNullException.ThrowIfNull(configuration);
        _model = configuration["GEMINI_MODEL"];
        _apiKey = configuration["GEMINI_API_KEY"];
    }

    public async Task<string?> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_model) || string.IsNullOrWhiteSpace(_apiKey))
        {
            return null;
        }

        // The API key is sent as a header, never as a query-string parameter, so it cannot
        // leak into request-URI logging.
        Uri endpoint = new($"{BaseUrl}{_model}:generateContent");
        GeminiRequest payload = new([new GeminiContent([new GeminiPart(prompt)])]);

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.TryAddWithoutValidation("x-goog-api-key", _apiKey);

            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            GeminiResponse? body = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken);
            return body?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            // Fail closed: the provider contract never throws, so the endpoint returns a
            // controlled unavailable result and the Desktop keeps its exact local text.
            return null;
        }
    }

    private sealed record GeminiRequest(
        [property: JsonPropertyName("contents")] IReadOnlyList<GeminiContent> Contents);

    private sealed record GeminiContent(
        [property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts);

    private sealed record GeminiPart(
        [property: JsonPropertyName("text")] string Text);

    private sealed record GeminiResponse(
        [property: JsonPropertyName("candidates")] IReadOnlyList<GeminiCandidate>? Candidates);

    private sealed record GeminiCandidate(
        [property: JsonPropertyName("content")] GeminiContent? Content);
}
