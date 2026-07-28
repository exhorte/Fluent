using System.Security.Cryptography;
using System.Text;

namespace Fluent.Windows.Clipboard;

/// <summary>
/// Windows clipboard wrapper that provides save/restore semantics for
/// automatic text insertion.
///
/// Design:
/// - Before pasting, the current clipboard content is saved.
/// - The dictated text is placed on the clipboard.
/// - After Ctrl+V injection, the previous content is restored.
/// - A fingerprint (SHA-256 first 8 hex chars) detects concurrent
///   modification by the user or another application.
/// </summary>
public sealed class WindowsClipboardManager : IClipboardManager
{
    public ClipboardToken SetText(string text)
    {
        string previous = SafeGetText();
        string fingerprint = ComputeFingerprint(previous);
        System.Windows.Clipboard.SetText(text);
        return new ClipboardToken(previous, fingerprint, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Save the current clipboard content, set new text, and return
    /// a restore token. Convenience overload.
    /// </summary>
    public ClipboardToken SaveAndSet(string newText)
    {
        return SetText(newText);
    }

    public bool TryRestore(ClipboardToken token)
    {
        try
        {
            if (string.IsNullOrEmpty(token.Text))
            {
                // Nothing to restore — the clipboard was empty before.
                return true;
            }

            string current = SafeGetText();
            if (current == token.Text)
            {
                // Already contains the saved content; no-op.
                return true;
            }

            System.Windows.Clipboard.SetText(token.Text);

            // Verify the restore took effect.
            string after = SafeGetText();
            return after == token.Text;
        }
        catch
        {
            return false;
        }
    }

    public bool IsUnchanged(ClipboardToken token)
    {
        try
        {
            string current = SafeGetText();
            string currentFingerprint = ComputeFingerprint(current);
            return string.Equals(
                currentFingerprint,
                token.Fingerprint,
                StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private static string SafeGetText()
    {
        try
        {
            return System.Windows.Clipboard.GetText(System.Windows.TextDataFormat.UnicodeText);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string ComputeFingerprint(string text)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash)[..8];
    }
}
