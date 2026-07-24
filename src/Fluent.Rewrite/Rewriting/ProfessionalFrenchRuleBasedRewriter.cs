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

        rewritten = HorizontalWhitespace().Replace(rewritten, " ");
        rewritten = SpaceBeforeTightPunctuation().Replace(rewritten, "$1");
        rewritten = MissingSpaceAfterComma().Replace(rewritten, "$1 ");
        rewritten = FrenchPunctuationSpacing().Replace(rewritten, " $1 ");
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
