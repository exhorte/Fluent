using System.Diagnostics;
using Fluent.Rewrite.Observability;
using Fluent.Rewrite.Providers;
using Fluent.Rewrite.Validation;

namespace Fluent.Rewrite.Orchestration;

/// <summary>
/// Selects Local by default and Cloud only under the full gate. The local result is always
/// computed first so that any Cloud timeout, transport error, disabled/unknown provider,
/// empty, invalid, or conversational response falls back to the exact local text.
/// </summary>
public sealed class RewriteOrchestrator
{
    private readonly LocalRewriteProvider _local;
    private readonly CloudRewriteProvider _cloud;
    private readonly ICloudRewriteValidator _cloudValidator;
    private readonly IRewriteObservabilitySink _observability;

    public RewriteOrchestrator(
        LocalRewriteProvider local,
        CloudRewriteProvider cloud,
        ICloudRewriteValidator cloudValidator,
        IRewriteObservabilitySink? observability = null)
    {
        _local = local ?? throw new ArgumentNullException(nameof(local));
        _cloud = cloud ?? throw new ArgumentNullException(nameof(cloud));
        _cloudValidator = cloudValidator ?? throw new ArgumentNullException(nameof(cloudValidator));
        _observability = observability ?? NullRewriteObservabilitySink.Instance;
    }

    public async Task<OrchestrationRewriteResult> RewriteAsync(
        OrchestrationRewriteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        Stopwatch timer = Stopwatch.StartNew();

        ProviderRewriteResult local = await _local.RewriteAsync(
            new ProviderRewriteRequest(request.Text, request.Profile),
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        string localText = local.Text;

        if (!request.Context.IsCloudEligible
            || !_cloud.TryResolveEnabled(request.Context.Provider, out IRewriteProvider provider))
        {
            return Complete(localText, RewriteProviderId.Local, RewriteStatus.LocalApplied,
                RewriteFailureReason.None, fallbackUsed: false, timer);
        }

        // The provider call AND the validation share one guard: a validator failure (for
        // example a RegexMatchTimeoutException on a hostile candidate) must degrade to the
        // exact local text, never lose the dictation.
        string cloudText;
        try
        {
            ProviderRewriteResult cloud = await provider.RewriteAsync(
                new ProviderRewriteRequest(request.Text),
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (!cloud.Succeeded || string.IsNullOrWhiteSpace(cloud.Text))
            {
                RewriteFailureReason reason = cloud.FailureReason == RewriteFailureReason.None
                    ? RewriteFailureReason.EmptyResponse
                    : cloud.FailureReason;
                return Fallback(localText, provider.Capabilities.Id, reason, timer);
            }

            RewriteValidationResult validation = _cloudValidator.Validate(request.Text, cloud.Text);
            if (!validation.IsValid)
            {
                return Fallback(localText, provider.Capabilities.Id, validation.Reason, timer);
            }

            cloudText = cloud.Text;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            return Fallback(localText, provider.Capabilities.Id, RewriteFailureReason.TransportError, timer);
        }

        return Complete(cloudText, provider.Capabilities.Id, RewriteStatus.CloudApplied,
            RewriteFailureReason.None, fallbackUsed: false, timer);
    }

    private OrchestrationRewriteResult Fallback(
        string localText,
        RewriteProviderId attemptedProvider,
        RewriteFailureReason reason,
        Stopwatch timer)
    {
        timer.Stop();
        _observability.Record(new RewriteTelemetry(attemptedProvider, timer.Elapsed, true, reason));
        return new OrchestrationRewriteResult(
            localText, RewriteProviderId.Local, RewriteStatus.LocalFallback, reason, true, timer.Elapsed);
    }

    private OrchestrationRewriteResult Complete(
        string text,
        RewriteProviderId provider,
        RewriteStatus status,
        RewriteFailureReason reason,
        bool fallbackUsed,
        Stopwatch timer)
    {
        timer.Stop();
        _observability.Record(new RewriteTelemetry(provider, timer.Elapsed, fallbackUsed, reason));
        return new OrchestrationRewriteResult(text, provider, status, reason, fallbackUsed, timer.Elapsed);
    }
}
