using System.Text.RegularExpressions;

namespace Fluent.Rewrite.Rewriting;

public sealed partial class ProfessionalFrenchRuleBasedRewriter : ILocalTextRewriter
{
    public Task<string> RewriteAsync(
        RewriteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        string rewritten = request.Text.Trim();
        if (rewritten.Length == 0)
        {
            return Task.FromResult(string.Empty);
        }

        // Language-independent: collapse horizontal whitespace.
        rewritten = HorizontalWhitespace().Replace(rewritten, " ");

        bool isFrench = string.Equals(
            request.TranscriptionLanguage, "fr", StringComparison.OrdinalIgnoreCase);

        // French-only: remove space before comma/period.
        if (isFrench)
        {
            rewritten = SpaceBeforeTightPunctuation().Replace(rewritten, "$1");
        }

        // Language-independent: ensure space after comma.
        rewritten = MissingSpaceAfterComma().Replace(rewritten, "$1 ");

        // French-only: normalize spacing around ; ! ?
        if (isFrench)
        {
            rewritten = FrenchPunctuationSpacing().Replace(rewritten, " $1 ");
        }

        // Language-independent: final whitespace cleanup.
        rewritten = HorizontalWhitespace().Replace(rewritten, " ").Trim();

        if (!TerminalPunctuation().IsMatch(rewritten))
        {
            rewritten += ".";
        }

        return Task.FromResult(rewritten);
    }

    [GeneratedRegex(@"[^\S\r\n]+", RegexOptions.CultureInvariant)]
    private static partial Regex HorizontalWhitespace();

    [GeneratedRegex(@"[^\S\r\n]+([,.])", RegexOptions.CultureInvariant)]
    private static partial Regex SpaceBeforeTightPunctuation();

    [GeneratedRegex(@"(,)(?=\p{L})", RegexOptions.CultureInvariant, 250)]
    private static partial Regex MissingSpaceAfterComma();

    [GeneratedRegex(@"[^\S\r\n]*([;!?])[^\S\r\n]*", RegexOptions.CultureInvariant)]
    private static partial Regex FrenchPunctuationSpacing();

    [GeneratedRegex(@"[.!?…]$", RegexOptions.CultureInvariant)]
    private static partial Regex TerminalPunctuation();
}
