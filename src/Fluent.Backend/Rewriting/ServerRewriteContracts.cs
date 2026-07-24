using System.Text.Json.Serialization;

namespace Fluent.Backend.Rewriting;

public sealed record ServerRewriteRequest(
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("provider")] string? Provider);

public sealed record ServerRewriteResponse(
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("provider")] string Provider);

public sealed record ServerRewriteResult(bool Succeeded, string? Text, string? Error)
{
    public static ServerRewriteResult Success(string text) => new(true, text, null);

    public static ServerRewriteResult Unavailable(string error) => new(false, null, error);
}
