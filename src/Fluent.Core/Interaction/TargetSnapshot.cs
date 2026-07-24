namespace Fluent.Core.Interaction;

public sealed record TargetSnapshot(
    long WindowHandle,
    int ProcessId,
    string WindowTitle,
    string WindowClassName,
    string? FocusedElementRuntimeId,
    string? FocusedElementName,
    string? FocusedElementControlType,
    bool IsPassword,
    DateTimeOffset CapturedAt)
{
    public TargetSecurityStatus SecurityStatus { get; init; } = IsPassword
        ? TargetSecurityStatus.Password
        : TargetSecurityStatus.Unknown;

    public bool IsPasswordTarget =>
        IsPassword || SecurityStatus == TargetSecurityStatus.Password;

    public bool HasVerifiedElementIdentity =>
        !IsPasswordTarget &&
        SecurityStatus == TargetSecurityStatus.VerifiedNonPassword &&
        !string.IsNullOrWhiteSpace(FocusedElementRuntimeId);

    public bool IsUsable => WindowHandle != 0 && HasVerifiedElementIdentity;

    public bool Matches(TargetSnapshot? current)
    {
        if (current is null)
        {
            return false;
        }

        if (IsPasswordTarget || current.IsPasswordTarget ||
            SecurityStatus != TargetSecurityStatus.VerifiedNonPassword ||
            current.SecurityStatus != TargetSecurityStatus.VerifiedNonPassword)
        {
            return false;
        }

        if (WindowHandle == 0 || current.WindowHandle == 0 ||
            WindowHandle != current.WindowHandle ||
            ProcessId <= 0 || current.ProcessId <= 0 ||
            ProcessId != current.ProcessId)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(FocusedElementRuntimeId) ||
            string.IsNullOrWhiteSpace(current.FocusedElementRuntimeId))
        {
            return false;
        }

        return string.Equals(
            FocusedElementRuntimeId,
            current.FocusedElementRuntimeId,
            StringComparison.Ordinal);
    }
}
