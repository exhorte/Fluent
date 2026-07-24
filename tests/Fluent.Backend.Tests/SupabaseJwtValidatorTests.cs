using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Fluent.Backend.Auth;

namespace Fluent.Backend.Tests;

public sealed class SupabaseJwtValidatorTests
{
    [Fact]
    public async Task Missing_trusted_configuration_fails_closed_as_unavailable()
    {
        SupabaseJwtValidator validator = new(TestConfiguration.From(), new TestHttpClientFactory(_ => throw new InvalidOperationException()));

        SupabaseJwtValidationResult result = await validator.ValidateAsync("Bearer token", CancellationToken.None);

        Assert.Equal(SupabaseJwtValidationStatus.Unavailable, result.Status);
    }

    [Fact]
    public async Task Missing_or_malformed_bearer_is_rejected_before_metadata_is_requested()
    {
        TestHttpClientFactory factory = new(_ => throw new InvalidOperationException("should not fetch"));
        SupabaseJwtValidator validator = CreateValidator(factory);

        SupabaseJwtValidationResult missing = await validator.ValidateAsync(null, CancellationToken.None);
        SupabaseJwtValidationResult malformed = await validator.ValidateAsync("Basic token", CancellationToken.None);

        Assert.Equal(SupabaseJwtValidationStatus.Invalid, missing.Status);
        Assert.Equal(SupabaseJwtValidationStatus.Invalid, malformed.Status);
    }

    [Fact]
    public async Task Hs256_token_is_rejected_without_fetching_jwks()
    {
        byte[] secret = Enumerable.Repeat((byte)7, 32).ToArray();
        JwtSecurityToken token = new(
            issuer: "https://fluent-test.supabase.co/auth/v1",
            audience: "authenticated",
            claims: [new("sub", Guid.NewGuid().ToString("D")), new("role", "authenticated")],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(secret), SecurityAlgorithms.HmacSha256));
        string rawToken = new JwtSecurityTokenHandler().WriteToken(token);
        SupabaseJwtValidator validator = CreateValidator(new TestHttpClientFactory(_ => throw new InvalidOperationException("should not fetch")));

        SupabaseJwtValidationResult result = await validator.ValidateAsync($"Bearer {rawToken}", CancellationToken.None);

        Assert.Equal(SupabaseJwtValidationStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Valid_rs256_token_with_expected_claims_is_accepted()
    {
        using RSA rsa = RSA.Create(2048);
        string keyId = "test-key";
        SupabaseJwtValidator validator = CreateValidator(new TestHttpClientFactory(request => CreateJwksResponse(request, rsa, keyId)));
        Guid userId = Guid.NewGuid();

        string token = CreateRs256Token(rsa, keyId, userId, "authenticated", "https://fluent-test.supabase.co/auth/v1");
        SupabaseJwtValidationResult result = await validator.ValidateAsync($"Bearer {token}", CancellationToken.None);

        Assert.Equal(SupabaseJwtValidationStatus.Valid, result.Status);
        Assert.Equal(userId.ToString("D"), result.UserId);
    }

    [Fact]
    public async Task Valid_signature_with_non_authenticated_role_is_forbidden()
    {
        using RSA rsa = RSA.Create(2048);
        string keyId = "test-key";
        SupabaseJwtValidator validator = CreateValidator(new TestHttpClientFactory(request => CreateJwksResponse(request, rsa, keyId)));

        string token = CreateRs256Token(rsa, keyId, Guid.NewGuid(), "anon", "https://fluent-test.supabase.co/auth/v1");
        SupabaseJwtValidationResult result = await validator.ValidateAsync($"Bearer {token}", CancellationToken.None);

        Assert.Equal(SupabaseJwtValidationStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Wrong_issuer_is_rejected_even_with_a_valid_signature()
    {
        using RSA rsa = RSA.Create(2048);
        string keyId = "test-key";
        SupabaseJwtValidator validator = CreateValidator(new TestHttpClientFactory(request => CreateJwksResponse(request, rsa, keyId)));

        string token = CreateRs256Token(rsa, keyId, Guid.NewGuid(), "authenticated", "https://other.supabase.co/auth/v1");
        SupabaseJwtValidationResult result = await validator.ValidateAsync($"Bearer {token}", CancellationToken.None);

        Assert.Equal(SupabaseJwtValidationStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Wrong_audience_is_rejected_even_with_a_valid_signature()
    {
        using RSA rsa = RSA.Create(2048);
        string keyId = "test-key";
        SupabaseJwtValidator validator = CreateValidator(new TestHttpClientFactory(request => CreateJwksResponse(request, rsa, keyId)));

        string token = CreateRs256Token(
            rsa, keyId, Guid.NewGuid(), "authenticated", "https://fluent-test.supabase.co/auth/v1", audience: "other");
        SupabaseJwtValidationResult result = await validator.ValidateAsync($"Bearer {token}", CancellationToken.None);

        Assert.Equal(SupabaseJwtValidationStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Expired_not_yet_valid_and_subjectless_tokens_are_rejected()
    {
        using RSA rsa = RSA.Create(2048);
        string keyId = "test-key";
        SupabaseJwtValidator validator = CreateValidator(new TestHttpClientFactory(request => CreateJwksResponse(request, rsa, keyId)));
        DateTime now = DateTime.UtcNow;

        string expired = CreateRs256Token(
            rsa, keyId, Guid.NewGuid(), "authenticated", "https://fluent-test.supabase.co/auth/v1",
            notBefore: now.AddMinutes(-10), expires: now.AddMinutes(-5));
        string notYetValid = CreateRs256Token(
            rsa, keyId, Guid.NewGuid(), "authenticated", "https://fluent-test.supabase.co/auth/v1",
            notBefore: now.AddMinutes(5), expires: now.AddMinutes(10));
        string subjectless = CreateRs256Token(
            rsa, keyId, Guid.NewGuid(), "authenticated", "https://fluent-test.supabase.co/auth/v1", includeSubject: false);

        Assert.Equal(SupabaseJwtValidationStatus.Invalid,
            (await validator.ValidateAsync($"Bearer {expired}", CancellationToken.None)).Status);
        Assert.Equal(SupabaseJwtValidationStatus.Invalid,
            (await validator.ValidateAsync($"Bearer {notYetValid}", CancellationToken.None)).Status);
        Assert.Equal(SupabaseJwtValidationStatus.Invalid,
            (await validator.ValidateAsync($"Bearer {subjectless}", CancellationToken.None)).Status);
    }

    [Fact]
    public async Task Unknown_kid_requests_one_jwks_refresh_then_fails_closed()
    {
        using RSA rsa = RSA.Create(2048);
        TestHttpClientFactory factory = new(request => CreateJwksResponse(request, rsa, "known-key"));
        SupabaseJwtValidator validator = CreateValidator(factory);
        string token = CreateRs256Token(
            rsa, "unknown-key", Guid.NewGuid(), "authenticated", "https://fluent-test.supabase.co/auth/v1");

        SupabaseJwtValidationResult result = await validator.ValidateAsync($"Bearer {token}", CancellationToken.None);

        Assert.Equal(SupabaseJwtValidationStatus.Invalid, result.Status);
        Assert.Equal(2, factory.MetadataRequestCount);
        Assert.Equal(2, factory.JwksRequestCount);
        Assert.Equal(4, factory.RequestCount);
    }

    [Fact]
    public async Task Concurrent_unknown_kid_validations_share_one_bounded_refresh_cycle()
    {
        using RSA rsa = RSA.Create(2048);
        TaskCompletionSource refreshStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseRefresh = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int metadataSequence = 0;
        TestHttpClientFactory factory = TestHttpClientFactory.CreateAsync(async (request, cancellationToken) =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("openid-configuration", StringComparison.Ordinal)
                && Interlocked.Increment(ref metadataSequence) == 2)
            {
                refreshStarted.TrySetResult();
                await releaseRefresh.Task.WaitAsync(cancellationToken);
            }

            return CreateJwksResponse(request, rsa, "known-key");
        });
        SupabaseJwtValidator validator = CreateValidator(factory);
        string token = CreateRs256Token(
            rsa, "unknown-key", Guid.NewGuid(), "authenticated", "https://fluent-test.supabase.co/auth/v1");

        Task<SupabaseJwtValidationResult> first = validator.ValidateAsync(
            $"Bearer {token}",
            CancellationToken.None);
        await refreshStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task<SupabaseJwtValidationResult> second = validator.ValidateAsync(
            $"Bearer {token}",
            CancellationToken.None);
        releaseRefresh.TrySetResult();

        SupabaseJwtValidationResult[] results = await Task.WhenAll(first, second);

        Assert.All(results, result => Assert.Equal(SupabaseJwtValidationStatus.Invalid, result.Status));
        Assert.Equal(2, factory.MetadataRequestCount);
        Assert.Equal(2, factory.JwksRequestCount);
        Assert.Equal(4, factory.RequestCount);
    }

    [Fact]
    public async Task Refreshed_jwks_can_validate_rotated_key_and_is_reused()
    {
        using RSA rsa = RSA.Create(2048);
        int jwksSequence = 0;
        TestHttpClientFactory factory = new(request =>
        {
            bool isJwks = request.RequestUri!.AbsolutePath.EndsWith("jwks.json", StringComparison.Ordinal);
            string keyId = isJwks && Interlocked.Increment(ref jwksSequence) >= 2
                ? "rotated-key"
                : "known-key";
            return CreateJwksResponse(request, rsa, keyId);
        });
        SupabaseJwtValidator validator = CreateValidator(factory);
        Guid userId = Guid.NewGuid();
        string token = CreateRs256Token(
            rsa, "rotated-key", userId, "authenticated", "https://fluent-test.supabase.co/auth/v1");

        SupabaseJwtValidationResult first = await validator.ValidateAsync(
            $"Bearer {token}",
            CancellationToken.None);
        SupabaseJwtValidationResult second = await validator.ValidateAsync(
            $"Bearer {token}",
            CancellationToken.None);

        Assert.Equal(SupabaseJwtValidationStatus.Valid, first.Status);
        Assert.Equal(userId.ToString("D"), first.UserId);
        Assert.Equal(SupabaseJwtValidationStatus.Valid, second.Status);
        Assert.Equal(2, factory.MetadataRequestCount);
        Assert.Equal(2, factory.JwksRequestCount);
    }

    [Fact]
    public async Task Failed_jwks_refresh_is_not_published_and_a_later_attempt_can_retry()
    {
        using RSA rsa = RSA.Create(2048);
        int jwksSequence = 0;
        bool failRefresh = true;
        TestHttpClientFactory factory = new(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("jwks.json", StringComparison.Ordinal)
                && Interlocked.Increment(ref jwksSequence) >= 2
                && failRefresh)
            {
                return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            }

            return CreateJwksResponse(request, rsa, "known-key");
        });
        SupabaseJwtValidator validator = CreateValidator(factory);
        string token = CreateRs256Token(
            rsa, "unknown-key", Guid.NewGuid(), "authenticated", "https://fluent-test.supabase.co/auth/v1");

        SupabaseJwtValidationResult failed = await validator.ValidateAsync(
            $"Bearer {token}",
            CancellationToken.None);
        failRefresh = false;
        SupabaseJwtValidationResult retried = await validator.ValidateAsync(
            $"Bearer {token}",
            CancellationToken.None);

        Assert.Equal(SupabaseJwtValidationStatus.Unavailable, failed.Status);
        Assert.Equal(SupabaseJwtValidationStatus.Invalid, retried.Status);
        Assert.Equal(3, factory.MetadataRequestCount);
        Assert.Equal(4, factory.JwksRequestCount);
    }

    [Fact]
    public async Task Caller_cancelled_while_refresh_is_in_flight_does_not_start_another_cycle()
    {
        using RSA rsa = RSA.Create(2048);
        TaskCompletionSource refreshStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseRefresh = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int metadataSequence = 0;
        TestHttpClientFactory factory = TestHttpClientFactory.CreateAsync(async (request, cancellationToken) =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("openid-configuration", StringComparison.Ordinal)
                && Interlocked.Increment(ref metadataSequence) == 2)
            {
                refreshStarted.TrySetResult();
                await releaseRefresh.Task;
            }

            return CreateJwksResponse(request, rsa, "known-key");
        });
        SupabaseJwtValidator validator = CreateValidator(factory);
        string token = CreateRs256Token(
            rsa, "unknown-key", Guid.NewGuid(), "authenticated", "https://fluent-test.supabase.co/auth/v1");
        Task<SupabaseJwtValidationResult> first = validator.ValidateAsync(
            $"Bearer {token}",
            CancellationToken.None);
        await refreshStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        using CancellationTokenSource cancellation = new();
        Task<SupabaseJwtValidationResult> cancelled = validator.ValidateAsync(
            $"Bearer {token}",
            cancellation.Token);
        cancellation.Cancel();

        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled);
        }
        finally
        {
            releaseRefresh.TrySetResult();
        }

        SupabaseJwtValidationResult completed = await first;

        Assert.Equal(SupabaseJwtValidationStatus.Invalid, completed.Status);
        Assert.Equal(2, factory.MetadataRequestCount);
        Assert.Equal(2, factory.JwksRequestCount);
    }

    private static SupabaseJwtValidator CreateValidator(IHttpClientFactory httpClientFactory) =>
        new(
            TestConfiguration.From(
                (SupabaseJwtValidator.IssuerEnvironmentVariable, "https://fluent-test.supabase.co/auth/v1"),
                (SupabaseJwtValidator.AudienceEnvironmentVariable, "authenticated")),
            httpClientFactory);

    private static HttpResponseMessage CreateJwksResponse(HttpRequestMessage request, RSA rsa, string keyId)
    {
        if (request.RequestUri!.AbsolutePath.EndsWith("openid-configuration", StringComparison.Ordinal))
        {
            return JsonResponse("""
                {"issuer":"https://fluent-test.supabase.co/auth/v1","jwks_uri":"https://fluent-test.supabase.co/auth/v1/.well-known/jwks.json"}
                """);
        }

        RSAParameters parameters = rsa.ExportParameters(false);
        return JsonResponse(
            $"{{\"keys\":[{{\"kty\":\"RSA\",\"kid\":\"{keyId}\",\"use\":\"sig\",\"alg\":\"RS256\",\"n\":\"{Base64Url(parameters.Modulus!)}\",\"e\":\"{Base64Url(parameters.Exponent!)}\"}}]}}");
    }

    private static string CreateRs256Token(
        RSA rsa,
        string keyId,
        Guid userId,
        string role,
        string issuer,
        string audience = "authenticated",
        DateTime? notBefore = null,
        DateTime? expires = null,
        bool includeSubject = true)
    {
        RsaSecurityKey key = new(rsa) { KeyId = keyId };
        List<System.Security.Claims.Claim> claims = [new("role", role)];
        if (includeSubject)
        {
            claims.Add(new("sub", userId.ToString("D")));
        }

        JwtSecurityToken token = new(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: notBefore ?? DateTime.UtcNow.AddMinutes(-1),
            expires: expires ?? DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static HttpResponseMessage JsonResponse(string content) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
    };

    private static string Base64Url(byte[] value) =>
        Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;
        private int _requestCount;
        private int _metadataRequestCount;
        private int _jwksRequestCount;

        public TestHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = (request, _) => Task.FromResult(handler(request));
        }

        private TestHttpClientFactory(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        public int RequestCount => Volatile.Read(ref _requestCount);

        public int MetadataRequestCount => Volatile.Read(ref _metadataRequestCount);

        public int JwksRequestCount => Volatile.Read(ref _jwksRequestCount);

        public static TestHttpClientFactory CreateAsync(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
            new(handler);

        public HttpClient CreateClient(string name) => new(new TestHandler(async (request, cancellationToken) =>
        {
            Interlocked.Increment(ref _requestCount);
            if (request.RequestUri!.AbsolutePath.EndsWith("openid-configuration", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref _metadataRequestCount);
            }
            else if (request.RequestUri.AbsolutePath.EndsWith("jwks.json", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref _jwksRequestCount);
            }

            return await _handler(request, cancellationToken);
        }));
    }

    private sealed class TestHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public TestHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            _handler(request, cancellationToken);
    }
}
