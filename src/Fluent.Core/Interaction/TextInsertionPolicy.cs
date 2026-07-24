namespace Fluent.Core.Interaction;

public sealed class TextInsertionPolicy
{
    public InsertionDecision Decide(TargetSnapshot? lockedTarget, TargetSnapshot? currentTarget)
    {
        if (lockedTarget is null || currentTarget is null)
        {
            return new InsertionDecision(
                InsertionDecisionKind.BlockedMissingTarget,
                "No locked target or current target is available.");
        }

        if (lockedTarget.IsPasswordTarget || currentTarget.IsPasswordTarget)
        {
            return new InsertionDecision(
                InsertionDecisionKind.BlockedPasswordTarget,
                "Password targets are blocked.");
        }

        if (!lockedTarget.HasVerifiedElementIdentity || !currentTarget.HasVerifiedElementIdentity)
        {
            return new InsertionDecision(
                InsertionDecisionKind.BlockedUnverifiedTarget,
                "The focused element identity or security state could not be verified.");
        }

        if (!lockedTarget.Matches(currentTarget))
        {
            return new InsertionDecision(
                InsertionDecisionKind.ClipboardFallbackTargetChanged,
                "The active target changed after capture.");
        }

        return new InsertionDecision(
            InsertionDecisionKind.PasteIntoOriginalTarget,
            "The current target matches the locked target.");
    }
}
