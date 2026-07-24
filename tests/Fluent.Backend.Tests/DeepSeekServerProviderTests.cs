using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Fluent.Backend.Rewriting;

namespace Fluent.Backend.Tests;

public sealed class DeepSeekServerProviderTests
{
    [Fact]
    public async Task Complete_server_configuration_enables_the_provider_and_returns_output()
    {
        FakeDeepSeekApi api = new("Texte reformulé.");
        DeepSeekServerProvider provider = new(api, ConfiguredServerConfiguration());

        ServerRewriteResult result = await provider.RewriteAsync("texte source", CancellationToken.None);

        Assert.True(provider.IsEnabled);
        Assert.True(result.Succeeded);
        Assert.Equal("Texte reformulé.", result.Text);
        Assert.Equal(1, api.CallCount);
    }

    [Theory]
    [InlineData("DEEPSEEK_MODEL", "")]
    [InlineData("DEEPSEEK_API_KEY", "")]
    [InlineData("DEEPSEEK_BASE_URL", "https://untrusted.example")]
    [InlineData("DEEPSEEK_BASE_URL", "http://api.deepseek.com")]
    [InlineData("DEEPSEEK_BASE_URL", "https://api.deepseek.com/v1")]
    public async Task Missing_or_invalid_configuration_is_unavailable_without_provider_call(string key, string value)
    {
        FakeDeepSeekApi api = new("never used");
        Dictionary<string, string> values = new()
        {
            ["DEEPSEEK_MODEL"] = "configured-deepseek-model",
            ["DEEPSEEK_API_KEY"] = "not-a-real-key",
            ["DEEPSEEK_BASE_URL"] = "https://api.deepseek.com"
        };
        values[key] = value;
        DeepSeekServerProvider provider = new(api, TestConfiguration.From(values.Select(pair => (pair.Key, pair.Value)).ToArray()));

        ServerRewriteResult result = await provider.RewriteAsync("texte source", CancellationToken.None);

        Assert.False(provider.IsEnabled);
        Assert.False(result.Succeeded);
        Assert.Equal("deepseek_not_configured", result.Error);
        Assert.Equal(0, api.CallCount);
    }

    [Fact]
    public async Task Prompt_never_contains_model_or_key()
    {
        const string model = "private-model-label";
        const string key = "not-a-real-secret";
        FakeDeepSeekApi api = new();
        DeepSeekServerProvider provider = new(api, TestConfiguration.From(
            ("DEEPSEEK_MODEL", model),
            ("DEEPSEEK_API_KEY", key),
            ("DEEPSEEK_BASE_URL", "https://api.deepseek.com")));

        await provider.RewriteAsync("texte source", CancellationToken.None);

        Assert.NotNull(api.LastPrompt);
        Assert.DoesNotContain(model, api.LastPrompt, StringComparison.Ordinal);
        Assert.DoesNotContain(key, api.LastPrompt, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("https://api.deepseek.com")]
    [InlineData("https://api.deepseek.com/")]
    public void Exact_https_origin_is_accepted(string value) =>
        Assert.True(HttpDeepSeekApi.IsAllowedBaseUrl(value));

    [Theory]
    [InlineData("http://api.deepseek.com")]
    [InlineData("https://api.deepseek.com:444")]
    [InlineData("https://api.deepseek.com/v1")]
    [InlineData("https://user@api.deepseek.com")]
    [InlineData("https://api.deepseek.com?target=other")]
    [InlineData("https://api.deepseek.com#fragment")]
    [InlineData("https://api.deepseek.com.evil.example")]
    public void Ambiguous_or_untrusted_base_url_is_rejected(string value) =>
        Assert.False(HttpDeepSeekApi.IsAllowedBaseUrl(value));

    [Fact]
    public async Task Http_transport_uses_fixed_route_and_header_authorization_only()
    {
        RecordingHandler handler = new(HttpStatusCode.OK,
            "{\"choices\":[{\"message\":{\"content\":\"Texte reformulé.\"}}]}");
        using HttpClient client = new(handler);
        HttpDeepSeekApi api = new(client, ConfiguredServerConfiguration());

        string? output = await api.GenerateAsync("prompt test", CancellationToken.None);

        Assert.Equal("Texte reformulé.", output);
        Assert.Equal("https://api.deepseek.com/chat/completions", handler.RequestUri!.AbsoluteUri);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Empty(handler.RequestUri.Query);
        Assert.Equal("Bearer", handler.AuthorizationScheme);
        Assert.Equal("not-a-real-key", handler.AuthorizationParameter);
        Assert.Contains("configured-deepseek-model", handler.Body, StringComparison.Ordinal);
        Assert.Contains("\"stream\":false", handler.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("not-a-real-key", handler.Body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Http_transport_with_invalid_configuration_never_calls_its_handler()
    {
        RecordingHandler handler = new(HttpStatusCode.OK, "{}");
        using HttpClient client = new(handler);
        HttpDeepSeekApi api = new(client, TestConfiguration.From(
            ("DEEPSEEK_MODEL", "configured-deepseek-model"),
            ("DEEPSEEK_API_KEY", "not-a-real-key"),
            ("DEEPSEEK_BASE_URL", "https://untrusted.example")));

        string? output = await api.GenerateAsync("prompt test", CancellationToken.None);

        Assert.Null(output);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task Http_transport_returns_null_for_a_non_success_response()
    {
        RecordingHandler handler = new(HttpStatusCode.TooManyRequests, "{}");
        using HttpClient client = new(handler);
        HttpDeepSeekApi api = new(client, ConfiguredServerConfiguration());

        string? output = await api.GenerateAsync("prompt test", CancellationToken.None);

        Assert.Null(output);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task Http_transport_propagates_caller_cancellation()
    {
        CancellationHandler handler = new();
        using HttpClient client = new(handler);
        HttpDeepSeekApi api = new(client, ConfiguredServerConfiguration());
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => api.GenerateAsync("prompt test", cancellation.Token));
    }

    private static IConfiguration ConfiguredServerConfiguration() => TestConfiguration.From(
        ("DEEPSEEK_MODEL", "configured-deepseek-model"),
        ("DEEPSEEK_API_KEY", "not-a-real-key"),
        ("DEEPSEEK_BASE_URL", "https://api.deepseek.com"));

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _response;

        public RecordingHandler(HttpStatusCode statusCode, string response)
        {
            _statusCode = statusCode;
            _response = response;
        }

        public int CallCount { get; private set; }

        public Uri? RequestUri { get; private set; }

        public HttpMethod? Method { get; private set; }

        public string? AuthorizationScheme { get; private set; }

        public string? AuthorizationParameter { get; private set; }

        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            RequestUri = request.RequestUri;
            Method = request.Method;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_response, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class CancellationHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromCanceled<HttpResponseMessage>(cancellationToken);
    }
}
