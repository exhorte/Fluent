namespace Fluent.Windows.Hotkeys;

/// <summary>
/// Abstraction over a low-level keyboard hook (WH_KEYBOARD_LL) that detects
/// the Ctrl+Win modifier-only combination for Push-to-Talk.
///
/// The hook notifies consumers via <see cref="StartRequested"/> and
/// <see cref="StopRequested"/> events, which fire exactly once per
/// activation/deactivation cycle. Events are raised on an arbitrary thread
/// (the hook callback thread); consumers must marshal to the UI thread.
/// </summary>
public interface IGlobalPushToTalkHook : IDisposable
{
    /// <summary>Raised when both Ctrl and Win are first held down (once).</summary>
    event EventHandler? StartRequested;

    /// <summary>Raised when either Ctrl or Win is released (once).</summary>
    event EventHandler? StopRequested;

    /// <summary>True while the hook is installed.</summary>
    bool IsInstalled { get; }

    /// <summary>Install the WH_KEYBOARD_LL hook. Idempotent.</summary>
    void Install();

    /// <summary>Uninstall the hook. Idempotent. Safe to call when not installed.</summary>
    void Uninstall();
}
