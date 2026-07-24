using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Fluent.App.Auth;

public enum SupabaseAuthorizationFailure
{
    None,
    Rejected,
    Invalid
}

public sealed record SupabaseAuthorizationCallback(string? Code, SupabaseAuthorizationFailure Failure)
{
    public bool IsSuccessful => !string.IsNullOrWhiteSpace(Code) && Failure == SupabaseAuthorizationFailure.None;
}

public interface ILoopbackCallbackListener : IAsyncDisposable
{
    Uri CallbackUri { get; }

    Task<SupabaseAuthorizationCallback> WaitForCallbackAsync(CancellationToken cancellationToken);
}

public interface ILoopbackCallbackListenerFactory
{
    ILoopbackCallbackListener Start();
}

/// <summary>
/// Receives one OAuth callback through a socket already bound by this process to 127.0.0.1 and
/// an OS-assigned port. The parser deliberately accepts only a minimal bounded GET request.
/// </summary>
public sealed class LoopbackCallbackListener : ILoopbackCallbackListener
{
    private const int MaximumHeaderBytes = 8 * 1024;
    private static readonly TimeSpan ConnectionTimeout = TimeSpan.FromSeconds(5);
    private readonly TcpListener _listener;
    private bool _disposed;

    private LoopbackCallbackListener(TcpListener listener)
    {
        _listener = listener;
        int port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        CallbackUri = new Uri($"http://127.0.0.1:{port}/callback");
    }

    public Uri CallbackUri { get; }

    public static LoopbackCallbackListener Start()
    {
        TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start(backlog: 1);
        return new LoopbackCallbackListener(listener);
    }

    public async Task<SupabaseAuthorizationCallback> WaitForCallbackAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        try
        {
            using TcpClient client = await _listener.AcceptTcpClientAsync(cancellationToken);
            using NetworkStream stream = client.GetStream();
            using CancellationTokenSource connectionTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            connectionTimeout.CancelAfter(ConnectionTimeout);
            string request = await ReadHeaderAsync(stream, connectionTimeout.Token);
            SupabaseAuthorizationCallback callback = ParseRequest(request);
            await WriteResponseAsync(stream, callback.IsSuccessful, connectionTimeout.Token);
            return callback;
        }
        finally
        {
            _listener.Stop();
        }
    }

    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            _listener.Stop();
        }

        return ValueTask.CompletedTask;
    }

    private static async Task<string> ReadHeaderAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[MaximumHeaderBytes];
        int length = 0;

        while (length < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer.AsMemory(length, buffer.Length - length), cancellationToken);
            if (read == 0)
            {
                break;
            }

            length += read;
            if (length >= 4
                && buffer.AsSpan(0, length).IndexOf("\r\n\r\n"u8) >= 0)
            {
                return Encoding.ASCII.GetString(buffer, 0, length);
            }
        }

        throw new InvalidOperationException("Callback HTTP invalide ou trop volumineux.");
    }

    private static SupabaseAuthorizationCallback ParseRequest(string request)
    {
        string[] lines = request.Split("\r\n", StringSplitOptions.None);
        string[] requestLine = lines.FirstOrDefault()?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
        if (requestLine.Length != 3
            || !string.Equals(requestLine[0], "GET", StringComparison.Ordinal)
            || !requestLine[2].StartsWith("HTTP/", StringComparison.Ordinal)
            || lines.Any(line => line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(line["Content-Length:".Length..].Trim(), "0", StringComparison.Ordinal)))
        {
            return new SupabaseAuthorizationCallback(null, SupabaseAuthorizationFailure.Invalid);
        }

        if (!Uri.TryCreate($"http://127.0.0.1{requestLine[1]}", UriKind.Absolute, out Uri? callback)
            || !string.Equals(callback.AbsolutePath, "/callback", StringComparison.Ordinal)
            || !string.IsNullOrEmpty(callback.Fragment))
        {
            return new SupabaseAuthorizationCallback(null, SupabaseAuthorizationFailure.Invalid);
        }

        IReadOnlyDictionary<string, string> values = ParseQuery(callback.Query);
        values.TryGetValue("code", out string? code);
        bool hasProviderError = values.ContainsKey("error")
            || values.ContainsKey("error_code")
            || values.ContainsKey("error_description");
        if (hasProviderError)
        {
            return new SupabaseAuthorizationCallback(null, SupabaseAuthorizationFailure.Rejected);
        }

        return string.IsNullOrWhiteSpace(code)
            ? new SupabaseAuthorizationCallback(null, SupabaseAuthorizationFailure.Invalid)
            : new SupabaseAuthorizationCallback(code, SupabaseAuthorizationFailure.None);
    }

    private static IReadOnlyDictionary<string, string> ParseQuery(string query)
    {
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        foreach (string pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = pair.Split('=', 2);
            if (parts.Length != 2 || !values.TryAdd(Uri.UnescapeDataString(parts[0]), Uri.UnescapeDataString(parts[1])))
            {
                return new Dictionary<string, string>(StringComparer.Ordinal);
            }
        }

        return values;
    }

    private static async Task WriteResponseAsync(NetworkStream stream, bool succeeded, CancellationToken cancellationToken)
    {
        string body = succeeded
            ? "<html><body>Connexion terminée. Vous pouvez revenir à Fluent.</body></html>"
            : "<html><body>Connexion non terminée. Vous pouvez revenir à Fluent.</body></html>";
        byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
        string header = $"HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {bodyBytes.Length}\r\nConnection: close\r\n\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(header), cancellationToken);
        await stream.WriteAsync(bodyBytes, cancellationToken);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private sealed class Factory : ILoopbackCallbackListenerFactory
    {
        public ILoopbackCallbackListener Start() => LoopbackCallbackListener.Start();
    }

    public static ILoopbackCallbackListenerFactory CreateFactory() => new Factory();
}
