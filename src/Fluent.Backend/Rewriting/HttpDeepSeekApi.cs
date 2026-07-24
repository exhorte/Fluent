using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace Fluent.Backend.Rewriting;

/// <summary>
/// Server-only DeepSeek chat-completions transport. A request is impossible unless all
/// three configuration values are present and the base URL is the exact approved HTTPS
/// origin. The authorization value is sent only in a header.
/// </summary>
public sealed class HttpDeepSeekApi : IDeepSeekApi
{
    private static readonly Uri ChatCompletionsEndpoint = new("https://api.deepseek.com/chat/completions");
    private readonly HttpClient _httpClient;
    private readonly string? _model;
    private readonly string? _apiKey;
    private readonly bool _isConfigured;

    public HttpDeepSeekApi(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        ArgumentNullException.ThrowIfNull(configuration);

        _model = configuration["DEEPSEEK_MODEL"];
        _apiKey = configuration["DEEPSEEK_API_KEY"];
        _isConfigured = IsAllowedModel(_model)
            && !string.IsNullOrWhiteSpace(_apiKey)
            && IsAllowedBaseUrl(configuration["DEEPSEEK_BASE_URL"]);
    }

    public static bool IsAllowedBaseUrl(string? configuredBaseUrl)
    {
        if (!Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        return uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && uri.Host.Equals("api.deepseek.com", StringComparison.OrdinalIgnoreCase)
            && uri.IsDefaultPort
            && string.IsNullOrEmpty(uri.UserInfo)
            && (uri.AbsolutePath == "/" || string.IsNullOrEmpty(uri.AbsolutePath))
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment);
    }

    public static bool IsAllowedModel(string? model) =>
        !string.IsNullOrWhiteSpace(model)
        && model.Length <= 128
        && !model.Any(char.IsControl);

    public async Task<string?> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        if (!_isConfigured || string.IsNullOrWhiteSpace(prompt))
        {
            return null;
        }

        DeepSeekRequest payload = new(_model!, [new DeepSeekMessage("user", prompt)], Stream: false);

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Post, ChatCompletionsEndpoint)
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_apiKey}");

            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            DeepSeekResponse? body = await response.Content.ReadFromJsonAsync<DeepSeekResponse>(cancellationToken);
            return body?.Choices?.FirstOrDefault()?.Message?.Content;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private sealed record DeepSeekRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<DeepSeekMessage> Messages,
        [property: JsonPropertyName("stream")] bool Stream);

    private sealed record DeepSeekMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record DeepSeekResponse(
        [property: JsonPropertyName("choices")] IReadOnlyList<DeepSeekChoice>? Choices);

    private sealed record DeepSeekChoice(
        [property: JsonPropertyName("message")] DeepSeekMessage? Message);
}
