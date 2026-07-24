using System.Diagnostics;
using System.Net.Http;
using Fluent.App.Cloud;

namespace Fluent.App.Auth;

public enum AuthenticationStatus
{
    Unconfigured,
    SignedOut,
    SigningIn,
    Authenticated,
    Offline,
    Expired,
    Cancelled,
    Failed
}

public interface ISystemBrowserLauncher
{
    Task OpenAsync(Uri authorizationUri, CancellationToken cancellationToken);
}

public sealed class SystemBrowserLauncher : ISystemBrowserLauncher
{
    public Task OpenAsync(Uri authorizationUri, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(authorizationUri);
        Process.Start(new ProcessStartInfo(authorizationUri.AbsoluteUri) { UseShellExecute = true });
        return Task.CompletedTask;
    }
}

/// <summary>
/// Owns the desktop Supabase session. Refresh tokens pass straight between the Auth transport
/// and Windows Credential Manager; access tokens remain in memory and become unavailable at
/// expiry. No state in this class reads .env or logs token-bearing values.
/// </summary>
public sealed class SupabaseAuthenticationState : IAuthenticationState, IDisposable
{
    private static readonly TimeSpan SignInTimeout = TimeSpan.FromSeconds(120);
    private static readonly TimeSpan AccessTokenSafetyWindow = TimeSpan.FromSeconds(30);
    private readonly SupabasePublicConfiguration? _configuration;
    private readonly string _unconfiguredReason;
    private readonly ISupabaseAuthApi _api;
    private readonly IRefreshTokenStore _refreshTokenStore;
    private readonly ILoopbackCallbackListenerFactory _callbackListenerFactory;
    private readonly ISystemBrowserLauncher _browserLauncher;
    private readonly TimeProvider _clock;
    private readonly TimeSpan _signInTimeout;
    private readonly object _sync = new();
    private Task? _refreshInFlight;
    private CancellationTokenSource? _signInCancellation;
    private SessionState? _session;
    private AuthenticationStatus _status;
    private bool _disposed;

    public SupabaseAuthenticationState(
        SupabasePublicConfiguration? configuration,
        string unconfiguredReason,
        ISupabaseAuthApi api,
        IRefreshTokenStore refreshTokenStore,
        ILoopbackCallbackListenerFactory callbackListenerFactory,
        ISystemBrowserLauncher browserLauncher,
        TimeProvider? clock = null,
        TimeSpan? signInTimeout = null)
    {
        _configuration = configuration;
        _unconfiguredReason = unconfiguredReason;
        _api = api ?? throw new ArgumentNullException(nameof(api));
        _refreshTokenStore = refreshTokenStore ?? throw new ArgumentNullException(nameof(refreshTokenStore));
        _callbackListenerFactory = callbackListenerFactory ?? throw new ArgumentNullException(nameof(callbackListenerFactory));
        _browserLauncher = browserLauncher ?? throw new ArgumentNullException(nameof(browserLauncher));
        _clock = clock ?? TimeProvider.System;
        _signInTimeout = signInTimeout ?? SignInTimeout;
        if (_signInTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(signInTimeout));
        }

        _status = configuration is null ? AuthenticationStatus.Unconfigured : AuthenticationStatus.SignedOut;
    }

    public event EventHandler? Changed;

    public bool IsAuthenticated => CurrentSession is not null;

    public string? AccessToken => CurrentSession?.AccessToken;

    public AuthenticationStatus Status => CurrentSession is null && _status == AuthenticationStatus.Authenticated
        ? AuthenticationStatus.Expired
        : _status;

    public AuthenticatedUser? User => CurrentSession?.User;

    public bool IsOperationInProgress => Status == AuthenticationStatus.SigningIn;

    public string StatusMessage => Status switch
    {
        AuthenticationStatus.Unconfigured => _unconfiguredReason,
        AuthenticationStatus.SigningIn => "Connexion sécurisée en cours dans votre navigateur…",
        AuthenticationStatus.Authenticated => "Connecté de façon sécurisée.",
        AuthenticationStatus.Offline => "Service d’authentification indisponible ; le mode local reste actif.",
        AuthenticationStatus.Expired => "La session a expiré. Connectez-vous à nouveau.",
        AuthenticationStatus.Cancelled => "Connexion annulée.",
        AuthenticationStatus.Failed => "Connexion Google impossible. Réessayez.",
        _ => "Non connecté ; le mode local reste actif."
    };

    public static SupabaseAuthenticationState CreateDefault(HttpClient httpClient)
    {
        SupabasePublicConfiguration.TryLoadFromEnvironment(out SupabasePublicConfiguration? configuration, out string reason);
        return new SupabaseAuthenticationState(
            configuration,
            reason,
            new SupabaseAuthApi(httpClient),
            new WindowsCredentialManagerRefreshTokenStore(),
            LoopbackCallbackListener.CreateFactory(),
            new SystemBrowserLauncher());
    }

    public Task RestoreSessionAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (_configuration is null)
        {
            return Task.CompletedTask;
        }

        lock (_sync)
        {
            _refreshInFlight ??= RefreshStoredSessionAsync();
            return _refreshInFlight.WaitAsync(cancellationToken);
        }
    }

    public async Task SignInWithGoogleAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (_configuration is null)
        {
            return;
        }

        CancellationTokenSource attemptCancellation;
        lock (_sync)
        {
            if (_signInCancellation is not null)
            {
                return;
            }

            attemptCancellation = new CancellationTokenSource();
            _signInCancellation = attemptCancellation;
        }

        SetStatus(AuthenticationStatus.SigningIn);
        using CancellationTokenSource timeoutCancellation = new();
        timeoutCancellation.CancelAfter(_signInTimeout);
        using CancellationTokenSource linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            attemptCancellation.Token,
            timeoutCancellation.Token);
        AuthenticationStatus terminalStatus = AuthenticationStatus.Failed;
        SupabaseSessionPayload? authenticatedSession = null;
        try
        {
            await using ILoopbackCallbackListener listener = _callbackListenerFactory.Start();
            PkceAuthorization pkce = PkceAuthorization.Create();
            Uri authorizationUri = BuildGoogleAuthorizationUri(_configuration, listener.CallbackUri, pkce);
            await _browserLauncher.OpenAsync(authorizationUri, linkedCancellation.Token);

            SupabaseAuthorizationCallback callback = await listener.WaitForCallbackAsync(linkedCancellation.Token);
            if (callback.IsSuccessful)
            {
                SupabaseSessionPayload session = await _api.ExchangeCodeAsync(
                    _configuration,
                    callback.Code!,
                    pkce.Verifier,
                    linkedCancellation.Token);
                await _refreshTokenStore.SaveAsync(session.RefreshToken, linkedCancellation.Token);
                authenticatedSession = session;
            }
        }
        catch (OperationCanceledException) when (attemptCancellation.IsCancellationRequested || cancellationToken.IsCancellationRequested)
        {
            terminalStatus = AuthenticationStatus.Cancelled;
        }
        catch (OperationCanceledException) when (timeoutCancellation.IsCancellationRequested)
        {
            terminalStatus = AuthenticationStatus.Failed;
        }
        catch (SupabaseAuthException exception) when (exception.Kind == SupabaseAuthFailureKind.Unavailable)
        {
            terminalStatus = AuthenticationStatus.Offline;
        }
        catch (SupabaseAuthException)
        {
            terminalStatus = AuthenticationStatus.Failed;
        }
        catch (Exception)
        {
            terminalStatus = AuthenticationStatus.Failed;
        }
        finally
        {
            lock (_sync)
            {
                if (ReferenceEquals(_signInCancellation, attemptCancellation))
                {
                    _signInCancellation = null;
                }
            }

            attemptCancellation.Dispose();
        }

        if (authenticatedSession is not null)
        {
            SetAuthenticated(authenticatedSession);
        }
        else
        {
            SetStatus(terminalStatus);
        }
    }

    public void CancelSignIn()
    {
        lock (_sync)
        {
            _signInCancellation?.Cancel();
        }
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        CancelSignIn();
        await _refreshTokenStore.DeleteAsync(cancellationToken);
        lock (_sync)
        {
            _session = null;
        }

        SetStatus(_configuration is null ? AuthenticationStatus.Unconfigured : AuthenticationStatus.SignedOut);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        CancelSignIn();
        lock (_sync)
        {
            _signInCancellation?.Dispose();
            _signInCancellation = null;
            _session = null;
        }
    }

    internal static Uri BuildGoogleAuthorizationUri(
        SupabasePublicConfiguration configuration,
        Uri callbackUri,
        PkceAuthorization pkce)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(callbackUri);
        ArgumentNullException.ThrowIfNull(pkce);

        UriBuilder builder = new(configuration.BuildAuthUri("auth/v1/authorize"));
        builder.Query = string.Join(
            "&",
            new[]
            {
                ("provider", "google"),
                ("redirect_to", callbackUri.AbsoluteUri),
                ("flow_type", "pkce"),
                ("code_challenge", pkce.Challenge),
                ("code_challenge_method", "s256")
            }.Select(pair => $"{pair.Item1}={Uri.EscapeDataString(pair.Item2)}"));
        return builder.Uri;
    }

    private async Task RefreshStoredSessionAsync()
    {
        await Task.Yield();

        try
        {
            string? refreshToken = await _refreshTokenStore.ReadAsync();
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                SetStatus(AuthenticationStatus.SignedOut);
                return;
            }

            SupabaseSessionPayload session = await _api.RefreshAsync(_configuration!, refreshToken, CancellationToken.None);
            await _refreshTokenStore.SaveAsync(session.RefreshToken);
            SetAuthenticated(session);
        }
        catch (SupabaseAuthException exception) when (exception.Kind == SupabaseAuthFailureKind.Rejected)
        {
            try
            {
                await _refreshTokenStore.DeleteAsync();
            }
            catch (Exception)
            {
                // A failure to delete is not a valid session. It must still be treated as expired.
            }

            ClearSession(AuthenticationStatus.Expired);
        }
        catch (SupabaseAuthException)
        {
            ClearSession(AuthenticationStatus.Offline);
        }
        catch (Exception)
        {
            ClearSession(AuthenticationStatus.Offline);
        }
        finally
        {
            lock (_sync)
            {
                _refreshInFlight = null;
            }
        }
    }

    private SessionState? CurrentSession
    {
        get
        {
            lock (_sync)
            {
                if (_session is null || _session.ExpiresAt <= _clock.GetUtcNow().Add(AccessTokenSafetyWindow))
                {
                    return null;
                }

                return _session;
            }
        }
    }

    private void SetAuthenticated(SupabaseSessionPayload session)
    {
        lock (_sync)
        {
            _session = new SessionState(session.AccessToken, session.ExpiresAt, session.User);
            _status = AuthenticationStatus.Authenticated;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void ClearSession(AuthenticationStatus status)
    {
        lock (_sync)
        {
            _session = null;
            _status = status;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void SetStatus(AuthenticationStatus status)
    {
        lock (_sync)
        {
            _status = status;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private sealed record SessionState(string AccessToken, DateTimeOffset ExpiresAt, AuthenticatedUser User);
}
