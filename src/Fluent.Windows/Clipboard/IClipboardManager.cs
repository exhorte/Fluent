namespace Fluent.Windows.Clipboard;

/// <summary>
/// Abstraction over the Windows clipboard for safe save/restore during
/// automatic text insertion.
/// </summary>
public interface IClipboardManager
{
    /// <summary>Set text on the clipboard. Returns a token that can be
    /// used to detect concurrent modification.</summary>
    ClipboardToken SetText(string text);

    /// <summary>Try to restore the clipboard to a previously saved state.
    /// Returns false if the clipboard was modified concurrently.</summary>
    bool TryRestore(ClipboardToken token);

    /// <summary>Check whether the clipboard still holds the same content
    /// that was set via SetText (best-effort, not cryptographically safe).</summary>
    bool IsUnchanged(ClipboardToken token);
}
