namespace Fluent.Windows.Clipboard;

/// <summary>
/// Opaque token returned by <see cref="IClipboardManager.SetText"/>.
/// Used to verify clipboard integrity before and after automatic paste.
/// </summary>
public sealed class ClipboardToken
{
    internal ClipboardToken(string text, string fingerprint, DateTimeOffset timestamp)
    {
        Text = text;
        Fingerprint = fingerprint;
        Timestamp = timestamp;
    }

    /// <summary>The text that was placed on the clipboard.</summary>
    public string Text { get; }

    /// <summary>A non-cryptographic fingerprint computed from the text
    /// to detect concurrent modification.</summary>
    internal string Fingerprint { get; }

    /// <summary>When the token was created.</summary>
    public DateTimeOffset Timestamp { get; }
}
