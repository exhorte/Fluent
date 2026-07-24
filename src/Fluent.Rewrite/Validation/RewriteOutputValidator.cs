using System.Text.RegularExpressions;

namespace Fluent.Rewrite.Validation;

public sealed partial class RewriteOutputValidator
{
    public bool IsValid(string source, string candidate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(candidate);

        return Extract(LexicalToken(), source).SequenceEqual(
                   Extract(LexicalToken(), candidate),
                   StringComparer.Ordinal)
               && Extract(SensitiveToken(), source).SequenceEqual(
                   Extract(SensitiveToken(), candidate),
                   StringComparer.Ordinal);
    }

    private static IEnumerable<string> Extract(Regex pattern, string text)
    {
        return pattern.Matches(text).Select(match => match.Value);
    }

    [GeneratedRegex(@"[\p{L}\p{M}\p{N}_'’\-]+", RegexOptions.CultureInvariant, 250)]
    private static partial Regex LexicalToken();

    [GeneratedRegex(
        @"`[^`\r\n]+`|""(?:[A-Za-z]:\\|\\\\)[^""]+""|https?://[^\s]+|www\.[^\s]+|\b[\p{L}_][\p{L}\p{N}_+.-]*:{1,2}[^\s,;!?]+|\b[\p{L}\p{N}._%+\-]+@[\p{L}\p{N}.\-]+\.[\p{L}]{2,}\b|\b[\p{L}\p{N}_\-]+(?:\.[\p{L}\p{N}_\-]+)+\b|(?:[A-Za-z]:\\|\\\\)[^\s,;!?]+|(?<![\p{L}\p{N}_])/(?:[^\s/]+/)*[^\s,;!?]+|\b\d{1,2}:\d{2}\b|\bv?\d+(?:\.\d+){1,4}(?:[-+][\p{L}\p{N}.-]+)?\b|(?<![\p{L}\p{N}_])[-+]?\d+(?:[.,]\d+)?%?(?![\p{L}\p{N}_])|\b[\p{L}_][\p{L}\p{N}_-]*\d[\p{L}\p{N}_-]*\b|\b[A-ZÀ-ÖØ-Þ][\p{L}\p{N}_-]*[A-ZÀ-ÖØ-Þ][\p{L}\p{N}_-]*\b",
        RegexOptions.CultureInvariant,
        250)]
    private static partial Regex SensitiveToken();
}
