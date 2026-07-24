namespace Fluent.Cloud;

/// <summary>
/// Configuration for reaching the Fluent backend. It holds no provider API key.
/// <see cref="AccessToken"/> is a user session bearer supplied by authentication, never a
/// Gemini or DeepSeek key, and is not persisted by this type.
/// </summary>
public sealed class CloudBackendOptions
{
    public Uri? BaseAddress { get; init; }

    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(8);

    public string? AccessToken { get; init; }
}
