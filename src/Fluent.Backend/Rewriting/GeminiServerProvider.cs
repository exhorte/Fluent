using Microsoft.Extensions.Configuration;

namespace Fluent.Backend.Rewriting;

/// <summary>
/// Server-side Gemini provider. The model and API key are read only from server
/// configuration (environment). If either is absent, it reports unavailable instead of
/// throwing, so the Desktop falls back to local text. It never returns the model to the client.
/// </summary>
public sealed class GeminiServerProvider : IServerRewriteProvider
{
    private readonly IGeminiApi _api;
    private readonly string? _model;
    private readonly bool _hasApiKey;

    public GeminiServerProvider(IGeminiApi api, IConfiguration configuration)
    {
        _api = api ?? throw new ArgumentNullException(nameof(api));
        ArgumentNullException.ThrowIfNull(configuration);
        _model = configuration["GEMINI_MODEL"];
        _hasApiKey = !string.IsNullOrWhiteSpace(configuration["GEMINI_API_KEY"]);
    }

    public string Id => "gemini";

    public bool IsEnabled => true;

    public async Task<ServerRewriteResult> RewriteAsync(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_model) || !_hasApiKey)
        {
            return ServerRewriteResult.Unavailable("gemini_not_configured");
        }

        string prompt = RewritePromptBuilder.Build(text);
        string? output = await _api.GenerateAsync(prompt, cancellationToken);

        if (string.IsNullOrWhiteSpace(output))
        {
            return ServerRewriteResult.Unavailable("gemini_empty_response");
        }

        return ServerRewriteResult.Success(output.Trim());
    }
}
