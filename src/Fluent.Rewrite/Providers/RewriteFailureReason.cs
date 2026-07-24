namespace Fluent.Rewrite.Providers;

public enum RewriteFailureReason
{
    None,
    NotEligible,
    ProviderDisabled,
    Timeout,
    TransportError,
    EmptyResponse,
    InvalidResponse,
    ConversationalResponse,
    ValidationRejected,
    Cancelled
}
