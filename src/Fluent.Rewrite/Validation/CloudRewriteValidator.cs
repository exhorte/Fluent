using System.Text.RegularExpressions;
using Fluent.Rewrite.Providers;

namespace Fluent.Rewrite.Validation;

/// <summary>
/// Validates a cloud rewrite candidate before it may replace the local text.
/// Unlike <see cref="RewriteOutputValidator"/> (which enforces exact lexical order for the
/// deterministic local profile), the cloud validator permits genuine rephrasing but still
/// fails closed: it rejects empty, over-long, and conversational responses, and requires
/// that every sensitive token (numbers, dates, URLs, paths, versions, commands, e-mails,
/// dotted identifiers) is preserved exactly as a multiset. Any rejection routes to the
/// exact local text.
/// </summary>
public sealed partial class CloudRewriteValidator : ICloudRewriteValidator
{
    private const int MaxLengthSlack = 80;
    private const int MaxLengthFactor = 4;

    public RewriteValidationResult Validate(string source, string candidate)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(candidate);

        if (string.IsNullOrWhiteSpace(candidate))
        {
            return RewriteValidationResult.Invalid(RewriteFailureReason.EmptyResponse);
        }

        if (candidate.Length > (source.Length * MaxLengthFactor) + MaxLengthSlack)
        {
            return RewriteValidationResult.Invalid(RewriteFailureReason.InvalidResponse);
        }

        if (Conversational().IsMatch(candidate))
        {
            return RewriteValidationResult.Invalid(RewriteFailureReason.ConversationalResponse);
        }

        List<string> sourceTokens = Extract(source);
        List<string> candidateTokens = Extract(candidate);
        if (!TokensPreserved(sourceTokens, candidateTokens))
        {
            return RewriteValidationResult.Invalid(RewriteFailureReason.InvalidResponse);
        }

        return RewriteValidationResult.Valid;
    }

    private static bool TokensPreserved(List<string> source, List<string> candidate)
    {
        if (source.Count != candidate.Count)
        {
            return false;
        }

        source.Sort(StringComparer.Ordinal);
        candidate.Sort(StringComparer.Ordinal);
        return source.SequenceEqual(candidate, StringComparer.Ordinal);
    }

    private static List<string> Extract(string text)
    {
        return SensitiveToken().Matches(text).Select(match => match.Value).ToList();
    }

    [GeneratedRegex(
        @"`[^`\r\n]+`|https?://[^\s]+|www\.[^\s]+|\b[\p{L}\p{N}._%+\-]+@[\p{L}\p{N}.\-]+\.[\p{L}]{2,}\b|(?:[A-Za-z]:\\|\\\\)[^\s,;!?]+|\b[\p{L}_][\p{L}\p{N}_+.-]*:{1,2}[^\s,;!?]+|(?<![\p{L}\p{N}_])/[^\s,;!?]*|\b\d{1,4}[/\-.]\d{1,2}[/\-.]\d{1,4}\b|\b\d{1,2}:\d{2}\b|\bv?\d+(?:\.\d+){1,4}(?:[-+][\p{L}\p{N}.-]+)?\b|(?<![\p{L}\p{N}_])[-+]?\d+(?:[.,]\d+)?%?(?![\p{L}\p{N}_])|\b[\p{L}\p{N}_\-]+(?:\.[\p{L}\p{N}_\-]+)+\b|\b[\p{L}_][\p{L}\p{N}_-]*\d[\p{L}\p{N}_-]*\b|\b[A-ZÀ-ÖØ-Þ][\p{L}\p{N}_-]*[A-ZÀ-ÖØ-Þ][\p{L}\p{N}_-]*\b",
        RegexOptions.CultureInvariant,
        250)]
    private static partial Regex SensitiveToken();

    [GeneratedRegex(
        @"^\s*(?:bien s[uû]r|voici|d'accord|je (?:vais|peux|ne peux|comprends|suis)|en tant qu|voil[aà]|sure|here(?:'s| is| are)|as an ai|i (?:cannot|can't|can|am|will)|certainly|of course|d[eé]sol[eé]|sorry)\b",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        250)]
    private static partial Regex Conversational();
}
