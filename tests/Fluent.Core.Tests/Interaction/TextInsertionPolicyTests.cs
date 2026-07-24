using Fluent.Core.Interaction;

namespace Fluent.Core.Tests.Interaction;

public sealed class TextInsertionPolicyTests
{
    private readonly TextInsertionPolicy _policy = new();

    [Fact]
    public void Decide_allows_paste_when_target_matches()
    {
        TargetSnapshot lockedTarget = CreateTarget(windowHandle: 42, runtimeId: "1.2.3");
        TargetSnapshot currentTarget = CreateTarget(windowHandle: 42, runtimeId: "1.2.3");

        InsertionDecision decision = _policy.Decide(lockedTarget, currentTarget);

        Assert.Equal(InsertionDecisionKind.PasteIntoOriginalTarget, decision.Kind);
        Assert.True(decision.ShouldPaste);
        Assert.True(decision.ShouldCopyToClipboard);
    }

    [Fact]
    public void Decide_uses_clipboard_fallback_when_window_changed()
    {
        TargetSnapshot lockedTarget = CreateTarget(windowHandle: 42, runtimeId: "1.2.3");
        TargetSnapshot currentTarget = CreateTarget(windowHandle: 84, runtimeId: "1.2.3");

        InsertionDecision decision = _policy.Decide(lockedTarget, currentTarget);

        Assert.Equal(InsertionDecisionKind.ClipboardFallbackTargetChanged, decision.Kind);
        Assert.False(decision.ShouldPaste);
        Assert.True(decision.ShouldCopyToClipboard);
    }

    [Fact]
    public void Decide_blocks_without_clipboard_when_current_element_identity_is_missing()
    {
        TargetSnapshot lockedTarget = CreateTarget(windowHandle: 42, runtimeId: "1.2.3");
        TargetSnapshot currentTarget = CreateTarget(windowHandle: 42, runtimeId: null);

        InsertionDecision decision = _policy.Decide(lockedTarget, currentTarget);

        Assert.Equal(InsertionDecisionKind.BlockedUnverifiedTarget, decision.Kind);
        Assert.False(decision.ShouldPaste);
        Assert.False(decision.ShouldCopyToClipboard);
    }

    [Fact]
    public void Decide_blocks_without_clipboard_when_locked_element_identity_is_missing()
    {
        TargetSnapshot lockedTarget = CreateTarget(windowHandle: 42, runtimeId: null);
        TargetSnapshot currentTarget = CreateTarget(windowHandle: 42, runtimeId: "1.2.3");

        InsertionDecision decision = _policy.Decide(lockedTarget, currentTarget);

        Assert.Equal(InsertionDecisionKind.BlockedUnverifiedTarget, decision.Kind);
        Assert.False(decision.ShouldPaste);
        Assert.False(decision.ShouldCopyToClipboard);
    }

    [Fact]
    public void Decide_blocks_without_clipboard_when_locked_element_security_is_unknown()
    {
        TargetSnapshot lockedTarget = CreateTarget(
            windowHandle: 42,
            runtimeId: "1.2.3",
            securityStatus: TargetSecurityStatus.Unknown);
        TargetSnapshot currentTarget = CreateTarget(windowHandle: 42, runtimeId: "1.2.3");

        InsertionDecision decision = _policy.Decide(lockedTarget, currentTarget);

        Assert.Equal(InsertionDecisionKind.BlockedUnverifiedTarget, decision.Kind);
        Assert.False(decision.ShouldPaste);
        Assert.False(decision.ShouldCopyToClipboard);
    }

    [Fact]
    public void Decide_blocks_without_clipboard_when_current_element_security_is_unknown()
    {
        TargetSnapshot lockedTarget = CreateTarget(windowHandle: 42, runtimeId: "1.2.3");
        TargetSnapshot currentTarget = CreateTarget(
            windowHandle: 42,
            runtimeId: "1.2.3",
            securityStatus: TargetSecurityStatus.Unknown);

        InsertionDecision decision = _policy.Decide(lockedTarget, currentTarget);

        Assert.Equal(InsertionDecisionKind.BlockedUnverifiedTarget, decision.Kind);
        Assert.False(decision.ShouldPaste);
        Assert.False(decision.ShouldCopyToClipboard);
    }

    [Fact]
    public void Decide_blocks_password_targets_without_clipboard_copy()
    {
        TargetSnapshot lockedTarget = CreateTarget(windowHandle: 42, runtimeId: "1.2.3", isPassword: true);
        TargetSnapshot currentTarget = CreateTarget(windowHandle: 42, runtimeId: "1.2.3", isPassword: true);

        InsertionDecision decision = _policy.Decide(lockedTarget, currentTarget);

        Assert.Equal(InsertionDecisionKind.BlockedPasswordTarget, decision.Kind);
        Assert.False(decision.ShouldPaste);
        Assert.False(decision.ShouldCopyToClipboard);
    }

    [Fact]
    public void Decide_blocks_explicit_password_security_when_legacy_flag_is_false()
    {
        TargetSnapshot lockedTarget = CreateTarget(
            windowHandle: 42,
            runtimeId: "1.2.3",
            securityStatus: TargetSecurityStatus.Password);
        TargetSnapshot currentTarget = CreateTarget(windowHandle: 42, runtimeId: "1.2.3");

        InsertionDecision decision = _policy.Decide(lockedTarget, currentTarget);

        Assert.Equal(InsertionDecisionKind.BlockedPasswordTarget, decision.Kind);
        Assert.False(decision.ShouldPaste);
        Assert.False(decision.ShouldCopyToClipboard);
    }

    [Fact]
    public void Decide_blocks_when_target_is_missing()
    {
        InsertionDecision decision = _policy.Decide(null, null);

        Assert.Equal(InsertionDecisionKind.BlockedMissingTarget, decision.Kind);
        Assert.False(decision.ShouldPaste);
        Assert.False(decision.ShouldCopyToClipboard);
    }

    private static TargetSnapshot CreateTarget(
        long windowHandle,
        string? runtimeId,
        bool isPassword = false,
        TargetSecurityStatus? securityStatus = null)
    {
        return new TargetSnapshot(
            WindowHandle: windowHandle,
            ProcessId: 100,
            WindowTitle: "Target",
            WindowClassName: "TargetClass",
            FocusedElementRuntimeId: runtimeId,
            FocusedElementName: "Editor",
            FocusedElementControlType: "ControlType.Edit",
            IsPassword: isPassword,
            CapturedAt: DateTimeOffset.UtcNow)
        {
            SecurityStatus = securityStatus ?? (isPassword
                ? TargetSecurityStatus.Password
                : TargetSecurityStatus.VerifiedNonPassword)
        };
    }
}
