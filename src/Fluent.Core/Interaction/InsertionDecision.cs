namespace Fluent.Core.Interaction;

public sealed record InsertionDecision(InsertionDecisionKind Kind, string Reason)
{
    public bool ShouldPaste => Kind == InsertionDecisionKind.PasteIntoOriginalTarget;

    public bool ShouldCopyToClipboard =>
        Kind is InsertionDecisionKind.PasteIntoOriginalTarget or InsertionDecisionKind.ClipboardFallbackTargetChanged;
}
