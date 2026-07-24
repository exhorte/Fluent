using Fluent.Backend.Auth;
using Fluent.Backend.Rewriting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// RemoveAllLoggers prevents the default HTTP client logging handlers from recording the
// outbound request, so no provider credential or user text can reach the server logs.
builder.Services.AddHttpClient<IGeminiApi, HttpGeminiApi>().RemoveAllLoggers();
builder.Services.AddHttpClient<IDeepSeekApi, HttpDeepSeekApi>(client => client.Timeout = TimeSpan.FromSeconds(8)).RemoveAllLoggers();
builder.Services.AddHttpClient("supabase-jwks", client => client.Timeout = TimeSpan.FromSeconds(5)).RemoveAllLoggers();

// Providers and the dispatcher are scoped: GeminiServerProvider depends on the typed
// IGeminiApi client, which is transient. Registering the provider as a singleton would
// capture that client for the process lifetime and defeat HttpMessageHandler rotation.
builder.Services.AddScoped<IServerRewriteProvider, GeminiServerProvider>();
builder.Services.AddScoped<IServerRewriteProvider, DeepSeekServerProvider>();
builder.Services.AddScoped<RewriteProviderDispatcher>();
builder.Services.AddSingleton<ISupabaseJwtValidator, SupabaseJwtValidator>();
builder.Services.AddSingleton<IValidatedUserRateLimiter, ValidatedUserRateLimiter>();

WebApplication app = builder.Build();

app.MapPost("/v1/rewrite", async (
    HttpContext context,
    ServerRewriteRequest? request,
    ISupabaseJwtValidator jwtValidator,
    IValidatedUserRateLimiter rateLimiter,
    RewriteProviderDispatcher dispatcher,
    CancellationToken cancellationToken) =>
{
    SupabaseJwtValidationResult authentication = await jwtValidator.ValidateAsync(
        context.Request.Headers.Authorization,
        cancellationToken);
    if (authentication.Status == SupabaseJwtValidationStatus.Unavailable)
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    if (authentication.Status == SupabaseJwtValidationStatus.Invalid)
    {
        return Results.Unauthorized();
    }

    if (authentication.Status == SupabaseJwtValidationStatus.Forbidden)
    {
        return Results.Forbid();
    }

    if (!await rateLimiter.TryAcquireAsync(authentication.UserId!, cancellationToken))
    {
        return Results.StatusCode(StatusCodes.Status429TooManyRequests);
    }

    RewriteRequestValidationResult validation = RewriteRequestValidator.Validate(request);
    if (!validation.IsValid)
    {
        return Results.BadRequest(new { error = validation.Error });
    }

    if (!dispatcher.TryResolve(request!.Provider, out IServerRewriteProvider provider))
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    ServerRewriteResult result = await provider.RewriteAsync(request.Text!, cancellationToken);
    if (!result.Succeeded || string.IsNullOrWhiteSpace(result.Text))
    {
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    return Results.Ok(new ServerRewriteResponse(result.Text, provider.Id));
});

app.Run();

namespace Fluent.Backend
{
    public partial class Program;
}
