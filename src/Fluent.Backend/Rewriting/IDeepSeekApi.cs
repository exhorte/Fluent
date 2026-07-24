namespace Fluent.Backend.Rewriting;

/// <summary>
/// Narrow server-only seam for the DeepSeek transport. Tests supply deterministic fakes;
/// the Desktop never depends on this interface and never receives provider configuration.
/// </summary>
public interface IDeepSeekApi
{
    Task<string?> GenerateAsync(string prompt, CancellationToken cancellationToken);
}
