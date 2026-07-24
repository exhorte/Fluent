namespace Fluent.Core.Interaction;

public enum InsertionDecisionKind
{
    PasteIntoOriginalTarget,
    ClipboardFallbackTargetChanged,
    BlockedPasswordTarget,
    BlockedUnverifiedTarget,
    BlockedMissingTarget
}
