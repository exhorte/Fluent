using Fluent.Rewrite.Providers;

namespace Fluent.Rewrite.Orchestration;

/// <summary>
/// The gating context supplied per dictation. Cloud rewriting is used only when the user
/// is authenticated AND has enabled Cloud rewriting AND has granted consent AND the
/// selected provider is an enabled cloud provider. Being merely authenticated never sends
/// text to the Cloud.
/// </summary>
public sealed record RewriteContext(
    bool IsAuthenticated,
    bool CloudRewriteEnabled,
    bool CloudConsentGranted,
    RewriteProviderId Provider)
{
    public static RewriteContext LocalOnly { get; } =
        new(false, false, false, RewriteProviderId.Local);

    public bool IsCloudEligible =>
        IsAuthenticated
        && CloudRewriteEnabled
        && CloudConsentGranted
        && Provider != RewriteProviderId.Local;
}
