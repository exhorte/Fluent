namespace Fluent.Backend.Rewriting;

/// <summary>
/// A server-side rewrite provider. Only enabled providers are dispatched. Secrets and the
/// exact model are read from server configuration or environment, never from the client.
/// </summary>
public interface IServerRewriteProvider
{
    string Id { get; }

    bool IsEnabled { get; }

    Task<ServerRewriteResult> RewriteAsync(string text, CancellationToken cancellationToken);
}
