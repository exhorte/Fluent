namespace Fluent.Backend.Rewriting;

/// <summary>
/// Transport abstraction for the live Gemini call. The concrete implementation reads the
/// model and API key from server configuration; tests substitute a fake and no live call
/// is made during automated verification.
/// </summary>
public interface IGeminiApi
{
    Task<string?> GenerateAsync(string prompt, CancellationToken cancellationToken);
}
