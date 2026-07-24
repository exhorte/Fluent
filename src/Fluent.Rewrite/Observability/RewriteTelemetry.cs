using Fluent.Rewrite.Providers;

namespace Fluent.Rewrite.Observability;

/// <summary>
/// Operational rewrite telemetry. It carries no user content, transcript, audio, or
/// secret: only the provider used, the response time, whether a fallback occurred, and
/// the fallback cause.
/// </summary>
public sealed record RewriteTelemetry(
    RewriteProviderId Provider,
    TimeSpan Duration,
    bool FallbackUsed,
    RewriteFailureReason FallbackReason);
