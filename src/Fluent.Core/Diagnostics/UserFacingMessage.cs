namespace Fluent.Core.Diagnostics;

/// <summary>
/// A user-facing message: a plain-language description and a recovery hint.
/// Never contains raw exception text or technical detail.
/// </summary>
public sealed record UserFacingMessage(string Message, string Recovery)
{
    /// <summary>The message and its recovery hint as one sentence pair.</summary>
    public string Combined => $"{Message} {Recovery}";
}
