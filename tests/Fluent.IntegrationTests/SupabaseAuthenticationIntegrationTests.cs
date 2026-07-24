using System.Security.Cryptography;
using System.Text;
using Fluent.App.Auth;

namespace Fluent.IntegrationTests;

public sealed class SupabaseAuthenticationIntegrationTests
{
    [Fact]
    public void Pkce_uses_s256_and_fresh_random_verifier_without_application_state()
    {
        PkceAuthorization first = PkceAuthorization.Create();
        PkceAuthorization second = PkceAuthorization.Create();
        string expectedChallenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(first.Verifier)));

        Assert.Equal(expectedChallenge, first.Challenge);
        Assert.NotEqual(first.Verifier, second.Verifier);
        Assert.NotEqual(first.Challenge, second.Challenge);
        Assert.DoesNotContain("=", first.Verifier, StringComparison.Ordinal);
        Assert.DoesNotContain("=", first.Challenge, StringComparison.Ordinal);
    }

    [Fact]
    public void Public_configuration_accepts_only_expected_supabase_https_origin()
    {
        Assert.True(SupabasePublicConfiguration.TryCreate(
            "https://fluent-test.supabase.co/",
            "sb_publishable_public_value",
            out SupabasePublicConfiguration? configuration,
            out _));
        Assert.NotNull(configuration);
        Assert.False(SupabasePublicConfiguration.TryCreate("http://fluent-test.supabase.co", "public", out _, out _));
        Assert.False(SupabasePublicConfiguration.TryCreate("https://example.com", "public", out _, out _));
        Assert.False(SupabasePublicConfiguration.TryCreate("https://fluent-test.supabase.co?x=1", "public", out _, out _));
        Assert.False(SupabasePublicConfiguration.TryCreate("https://fluent-test.supabase.co/not-a-project-origin", "public", out _, out _));
    }

    [Fact]
    public async Task Unconfigured_desktop_never_launches_browser_or_network()
    {
        RecordingBrowser browser = new();
        SupabaseAuthenticationState state = new(
            null,
            "Configuration absente.",
            new FakeAuthApi(),
            new FakeRefreshTokenStore(),
            new FakeListenerFactory(),
            browser);

        await state.SignInWithGoogleAsync();

        Assert.Equal(AuthenticationStatus.Unconfigured, state.Status);
        Assert.Equal(0, browser.OpenCount);
        Assert.False(state.IsAuthenticated);
    }

    [Fact]
    public async Task Google_sign_in_uses_system_browser_pkce_callback_and_memory_access_token()
    {
        SupabasePublicConfiguration configuration = CreateConfiguration();
        FakeListener listener = new();
        RecordingBrowser browser = new();
        FakeAuthApi api = new();
        FakeRefreshTokenStore store = new();
        SupabaseAuthenticationState state = new(
            configuration,
            string.Empty,
            api,
            store,
            new FakeListenerFactory(listener),
            browser);

        await state.SignInWithGoogleAsync();

        Assert.Equal(1, browser.OpenCount);
        Assert.NotNull(browser.LastUri);
        Assert.Equal("https", browser.LastUri!.Scheme);
        Assert.Equal("fluent-test.supabase.co", browser.LastUri.Host);
        Assert.Equal("/auth/v1/authorize", browser.LastUri.AbsolutePath);
        IReadOnlyDictionary<string, string> query = ReadQuery(browser.LastUri);
        Assert.Equal(5, query.Count);
        Assert.Equal("google", query["provider"]);
        Assert.Equal(listener.CallbackUri.AbsoluteUri, query["redirect_to"]);
        Assert.Equal("pkce", query["flow_type"]);
        Assert.Equal("s256", query["code_challenge_method"]);
        Assert.False(query.ContainsKey("state"));
        Assert.Equal(1, api.ExchangeCallCount);
        Assert.NotNull(api.LastVerifier);
        Assert.Equal(Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(api.LastVerifier!))), query["code_challenge"]);
        Assert.Equal("access-token", state.AccessToken);
        Assert.True(state.IsAuthenticated);
        Assert.Equal(AuthenticationStatus.Authenticated, state.Status);
        Assert.Equal("refresh-token", store.SavedToken);
        Assert.Equal("Ada Lovelace", state.User?.DisplayName);
        Assert.Equal("ada@example.test", state.User?.Email);
    }

    [Fact]
    public async Task Startup_refresh_is_single_flight_and_only_refresh_token_is_read()
    {
        TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        FakeAuthApi api = new()
        {
            RefreshHandler = async _ =>
            {
                await gate.Task;
                return FakeAuthApi.CreateSession();
            }
        };
        FakeRefreshTokenStore store = new() { StoredToken = "refresh-token" };
        SupabaseAuthenticationState state = new(
            CreateConfiguration(),
            string.Empty,
            api,
            store,
            new FakeListenerFactory(),
            new RecordingBrowser());

        Task first = state.RestoreSessionAsync();
        Task second = state.RestoreSessionAsync();
        gate.SetResult();
        await Task.WhenAll(first, second);

        Assert.Equal(1, api.RefreshCallCount);
        Assert.Equal(1, store.ReadCount);
        Assert.Equal("access-token", state.AccessToken);
        Assert.Equal("refresh-token", store.SavedToken);
    }

    [Fact]
    public async Task Definitively_rejected_refresh_wipes_persisted_refresh_token_and_expires_session()
    {
        FakeAuthApi api = new() { RefreshException = new SupabaseAuthException(SupabaseAuthFailureKind.Rejected) };
        FakeRefreshTokenStore store = new() { StoredToken = "old-refresh-token" };
        SupabaseAuthenticationState state = new(
            CreateConfiguration(),
            string.Empty,
            api,
            store,
            new FakeListenerFactory(),
            new RecordingBrowser());

        await state.RestoreSessionAsync();

        Assert.Equal(AuthenticationStatus.Expired, state.Status);
        Assert.True(store.DeleteCalled);
        Assert.Null(state.AccessToken);
    }

    [Fact]
    public async Task Unavailable_refresh_keeps_refresh_token_but_never_marks_cloud_session_authenticated()
    {
        FakeAuthApi api = new() { RefreshException = new SupabaseAuthException(SupabaseAuthFailureKind.Unavailable) };
        FakeRefreshTokenStore store = new() { StoredToken = "recoverable-refresh-token" };
        SupabaseAuthenticationState state = new(
            CreateConfiguration(),
            string.Empty,
            api,
            store,
            new FakeListenerFactory(),
            new RecordingBrowser());

        await state.RestoreSessionAsync();

        Assert.Equal(AuthenticationStatus.Offline, state.Status);
        Assert.False(store.DeleteCalled);
        Assert.False(state.IsAuthenticated);
        Assert.Null(state.AccessToken);
    }

    [Fact]
    public async Task Cancelling_browser_wait_never_exchanges_a_code()
    {
        FakeListener listener = new() { WaitForever = true };
        RecordingBrowser browser = new();
        FakeAuthApi api = new();
        SupabaseAuthenticationState state = new(
            CreateConfiguration(),
            string.Empty,
            api,
            new FakeRefreshTokenStore(),
            new FakeListenerFactory(listener),
            browser);

        Task signIn = state.SignInWithGoogleAsync();
        await browser.Opened.Task;
        state.CancelSignIn();
        state.CancelSignIn();
        await signIn;

        Assert.Equal(AuthenticationStatus.Cancelled, state.Status);
        Assert.False(state.IsOperationInProgress);
        Assert.Equal(1, listener.DisposeCount);
        Assert.Equal(0, api.ExchangeCallCount);
        Assert.False(state.IsAuthenticated);
    }

    [Fact]
    public async Task Bad_oauth_state_is_normalized_without_raw_provider_details()
    {
        await using ILoopbackCallbackListener listener = LoopbackCallbackListener.Start();
        using HttpClient client = new();

        Task<SupabaseAuthorizationCallback> waiting = listener.WaitForCallbackAsync(CancellationToken.None);
        _ = await client.GetStringAsync(new Uri(
            listener.CallbackUri
            + "?error=invalid_request&error_code=bad_oauth_state&error_description=sensitive-provider-detail"));
        SupabaseAuthorizationCallback callback = await waiting;

        Assert.False(callback.IsSuccessful);
        Assert.Null(callback.Code);
        Assert.Equal(SupabaseAuthorizationFailure.Rejected, callback.Failure);
        Assert.DoesNotContain("bad_oauth_state", callback.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-provider-detail", callback.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task OAuth_error_finishes_attempt_disposes_listener_and_allows_immediate_retry()
    {
        FakeListener failedListener = new()
        {
            CallbackUri = new Uri("http://127.0.0.1:49152/callback"),
            Callback = new SupabaseAuthorizationCallback(null, SupabaseAuthorizationFailure.Rejected)
        };
        FakeListener successfulListener = new()
        {
            CallbackUri = new Uri("http://127.0.0.1:49153/callback")
        };
        FakeListenerFactory listeners = new(failedListener, successfulListener);
        RecordingBrowser browser = new();
        FakeAuthApi api = new();
        SupabaseAuthenticationState state = new(
            CreateConfiguration(),
            string.Empty,
            api,
            new FakeRefreshTokenStore(),
            listeners,
            browser);
        bool cleanupCompletedBeforeTerminalNotification = false;
        state.Changed += (_, _) =>
        {
            if (state.Status == AuthenticationStatus.Failed)
            {
                cleanupCompletedBeforeTerminalNotification = failedListener.DisposeCount == 1
                    && !state.IsOperationInProgress;
            }
        };

        await state.SignInWithGoogleAsync();

        Assert.Equal(AuthenticationStatus.Failed, state.Status);
        Assert.Equal("Connexion Google impossible. Réessayez.", state.StatusMessage);
        Assert.False(state.IsOperationInProgress);
        Assert.True(cleanupCompletedBeforeTerminalNotification);
        Assert.Equal(1, failedListener.DisposeCount);
        Assert.Equal(0, api.ExchangeCallCount);
        state.CancelSignIn();
        state.CancelSignIn();

        await state.SignInWithGoogleAsync();

        Assert.Equal(AuthenticationStatus.Authenticated, state.Status);
        Assert.Equal(1, api.ExchangeCallCount);
        Assert.Equal(1, successfulListener.DisposeCount);
        Assert.Equal(2, browser.OpenCount);
        Assert.Equal(2, listeners.StartCount);
        Assert.Equal(2, browser.OpenedUris.Count);
        IReadOnlyDictionary<string, string> firstQuery = ReadQuery(browser.OpenedUris[0]);
        IReadOnlyDictionary<string, string> secondQuery = ReadQuery(browser.OpenedUris[1]);
        Assert.Equal(failedListener.CallbackUri.AbsoluteUri, firstQuery["redirect_to"]);
        Assert.Equal(successfulListener.CallbackUri.AbsoluteUri, secondQuery["redirect_to"]);
        Assert.NotEqual(firstQuery["code_challenge"], secondQuery["code_challenge"]);
        Assert.False(firstQuery.ContainsKey("state"));
        Assert.False(secondQuery.ContainsKey("state"));
    }

    [Fact]
    public async Task Missing_callback_times_out_to_failed_and_releases_listener()
    {
        FakeListener listener = new() { WaitForever = true };
        FakeAuthApi api = new();
        SupabaseAuthenticationState state = new(
            CreateConfiguration(),
            string.Empty,
            api,
            new FakeRefreshTokenStore(),
            new FakeListenerFactory(listener),
            new RecordingBrowser(),
            signInTimeout: TimeSpan.FromMilliseconds(50));

        await state.SignInWithGoogleAsync();

        Assert.Equal(AuthenticationStatus.Failed, state.Status);
        Assert.False(state.IsOperationInProgress);
        Assert.Equal(1, listener.DisposeCount);
        Assert.Equal(0, api.ExchangeCallCount);
        state.CancelSignIn();
        state.CancelSignIn();
    }

    [Fact]
    public async Task Loopback_listener_accepts_one_bounded_callback_on_127_only()
    {
        await using ILoopbackCallbackListener listener = LoopbackCallbackListener.Start();
        using HttpClient client = new();

        Task<SupabaseAuthorizationCallback> waiting = listener.WaitForCallbackAsync(CancellationToken.None);
        _ = await client.GetStringAsync(new Uri(listener.CallbackUri + "?code=test-code"));
        SupabaseAuthorizationCallback callback = await waiting;

        Assert.Equal("test-code", callback.Code);
        Assert.Equal(SupabaseAuthorizationFailure.None, callback.Failure);
        Assert.True(callback.IsSuccessful);
    }

    private static SupabasePublicConfiguration CreateConfiguration()
    {
        Assert.True(SupabasePublicConfiguration.TryCreate(
            "https://fluent-test.supabase.co",
            "sb_publishable_public_value",
            out SupabasePublicConfiguration? configuration,
            out _));
        return configuration!;
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed class FakeAuthApi : ISupabaseAuthApi
    {
        public int ExchangeCallCount { get; private set; }

        public string? LastVerifier { get; private set; }

        public int RefreshCallCount { get; private set; }

        public Func<string, Task<SupabaseSessionPayload>>? RefreshHandler { get; init; }

        public Exception? RefreshException { get; init; }

        public Task<SupabaseSessionPayload> ExchangeCodeAsync(
            SupabasePublicConfiguration configuration,
            string authorizationCode,
            string verifier,
            CancellationToken cancellationToken)
        {
            ExchangeCallCount++;
            LastVerifier = verifier;
            Assert.Equal("test-code", authorizationCode);
            Assert.False(string.IsNullOrWhiteSpace(verifier));
            return Task.FromResult(CreateSession());
        }

        public async Task<SupabaseSessionPayload> RefreshAsync(
            SupabasePublicConfiguration configuration,
            string refreshToken,
            CancellationToken cancellationToken)
        {
            RefreshCallCount++;
            if (RefreshException is not null)
            {
                throw RefreshException;
            }

            return RefreshHandler is null
                ? CreateSession()
                : await RefreshHandler(refreshToken);
        }

        public static SupabaseSessionPayload CreateSession() => new(
            "access-token",
            "refresh-token",
            DateTimeOffset.UtcNow.AddHours(1),
            new AuthenticatedUser(Guid.NewGuid().ToString("D"), "ada@example.test", "Ada Lovelace"));
    }

    private sealed class FakeRefreshTokenStore : IRefreshTokenStore
    {
        public string? StoredToken { get; init; }

        public string? SavedToken { get; private set; }

        public bool DeleteCalled { get; private set; }

        public int ReadCount { get; private set; }

        public Task<string?> ReadAsync(CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return Task.FromResult(StoredToken);
        }

        public Task SaveAsync(string refreshToken, CancellationToken cancellationToken = default)
        {
            SavedToken = refreshToken;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(CancellationToken cancellationToken = default)
        {
            DeleteCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingBrowser : ISystemBrowserLauncher
    {
        private readonly List<Uri> _openedUris = [];

        public int OpenCount { get; private set; }

        public Uri? LastUri { get; private set; }

        public IReadOnlyList<Uri> OpenedUris => _openedUris;

        public TaskCompletionSource Opened { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task OpenAsync(Uri authorizationUri, CancellationToken cancellationToken)
        {
            OpenCount++;
            LastUri = authorizationUri;
            _openedUris.Add(authorizationUri);
            Opened.TrySetResult();
            return Task.CompletedTask;
        }
    }

    private sealed class FakeListenerFactory : ILoopbackCallbackListenerFactory
    {
        private readonly Queue<FakeListener> _listeners;

        public FakeListenerFactory(params FakeListener[] listeners)
        {
            _listeners = new Queue<FakeListener>(listeners.Length == 0 ? [new FakeListener()] : listeners);
        }

        public int StartCount { get; private set; }

        public ILoopbackCallbackListener Start()
        {
            StartCount++;
            return _listeners.Count == 0
                ? throw new InvalidOperationException("Aucun listener de test disponible.")
                : _listeners.Dequeue();
        }
    }

    private sealed class FakeListener : ILoopbackCallbackListener
    {
        public bool WaitForever { get; init; }

        public Uri CallbackUri { get; init; } = new("http://127.0.0.1:49152/callback");

        public SupabaseAuthorizationCallback Callback { get; init; } = new(
            "test-code",
            SupabaseAuthorizationFailure.None);

        public int DisposeCount { get; private set; }

        public async Task<SupabaseAuthorizationCallback> WaitForCallbackAsync(CancellationToken cancellationToken)
        {
            if (WaitForever)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            return Callback;
        }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }

    private static IReadOnlyDictionary<string, string> ReadQuery(Uri uri)
    {
        Dictionary<string, string> values = new(StringComparer.Ordinal);
        foreach (string pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] parts = pair.Split('=', 2);
            if (parts.Length == 2)
            {
                Assert.True(values.TryAdd(
                    Uri.UnescapeDataString(parts[0]),
                    Uri.UnescapeDataString(parts[1])));
            }
        }

        return values;
    }
}
