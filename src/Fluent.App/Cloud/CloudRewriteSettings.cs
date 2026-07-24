using Fluent.Rewrite.Providers;

namespace Fluent.App.Cloud;

/// <summary>
/// Session-only Cloud rewrite preferences. Nothing is persisted: every launch starts with
/// Cloud rewriting disabled and consent not granted, per the Phase 06B decision to defer any
/// persistence design. Enabling requires prior explicit consent.
/// </summary>
public sealed class CloudRewriteSettings
{
    public RewriteProviderId SelectedProvider { get; private set; } = RewriteProviderId.Gemini;

    public bool CloudRewriteEnabled { get; private set; }

    public bool CloudConsentGranted { get; private set; }

    public event EventHandler? Changed;

    public void GrantConsent()
    {
        if (CloudConsentGranted)
        {
            return;
        }

        CloudConsentGranted = true;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool TryEnable()
    {
        if (!CloudConsentGranted || CloudRewriteEnabled)
        {
            return false;
        }

        CloudRewriteEnabled = true;
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void Disable()
    {
        if (!CloudRewriteEnabled)
        {
            return;
        }

        CloudRewriteEnabled = false;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Selects an already-known Cloud provider for this process only. Choosing a provider
    /// never enables Cloud rewriting and never grants consent.
    /// </summary>
    public bool TrySelectProvider(RewriteProviderId provider)
    {
        if (provider is not (RewriteProviderId.Gemini or RewriteProviderId.DeepSeek)
            || provider == SelectedProvider)
        {
            return false;
        }

        SelectedProvider = provider;
        Changed?.Invoke(this, EventArgs.Empty);
        return true;
    }
}
