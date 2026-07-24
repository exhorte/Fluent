using Microsoft.Extensions.Configuration;

namespace Fluent.Backend.Rewriting;

/// <summary>
/// Server-side DeepSeek provider. It has no defaults: absent or invalid server process
/// configuration leaves it unavailable and prevents an outbound provider request.
/// </summary>
public sealed class DeepSeekServerProvider : IServerRewriteProvider
{
    private readonly IDeepSeekApi _api;
    private readonly string? _model;
    private readonly bool _isConfigured;

    public DeepSeekServerProvider(IDeepSeekApi api, IConfiguration configuration)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        ArgumentNullException.ThrowIfNull(configuration);

        _model = configuration["DEEPSEEK_MODEL"];
        _isConfigured = HttpDeepSeekApi.IsAllowedModel(_model)
            && !string.IsNullOrWhiteSpace(configuration["DEEPSEEK_API_KEY"])
            && HttpDeepSeekApi.IsAllowedBaseUrl(configuration["DEEPSEEK_BASE_URL"]);
    }

    public string Id => "deepseek";

    public bool IsEnabled => _isConfigured;

    public async Task<ServerRewriteResult> RewriteAsync(string text, CancellationToken cancellationToken)
    {
        if (!_isConfigured)
        {
            return ServerRewriteResult.Unavailable("deepseek_not_configured");
        }

        string prompt = RewritePromptBuilder.Build(text);
        string? output = await _api.GenerateAsync(prompt, cancellationToken);
        if (string.IsNullOrWhiteSpace(output))
        {
            return ServerRewriteResult.Unavailable("deepseek_empty_response");
        }

        return ServerRewriteResult.Success(output.Trim());
    }
}
