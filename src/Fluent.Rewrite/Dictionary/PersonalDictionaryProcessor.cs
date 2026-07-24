using System.Buffers;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Fluent.Rewrite.Dictionary;

public sealed partial class PersonalDictionaryProcessor
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(250);

    private readonly TimeSpan _timeout;

    public PersonalDictionaryProcessor()
        : this(DefaultTimeout)
    {
    }

    public PersonalDictionaryProcessor(TimeSpan timeout)
    {
        if (timeout < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(timeout),
                timeout,
                "The timeout cannot be negative.");
        }

        _timeout = timeout;
    }

    public DictionaryProcessingResult Apply(
        string transcript,
        IReadOnlyList<PersonalDictionaryEntry>? snapshot)
    {
        ArgumentNullException.ThrowIfNull(transcript);

        Stopwatch stopwatch = Stopwatch.StartNew();
        if (snapshot is null ||
            !TryPrepareEntries(snapshot, out PersonalDictionaryEntry[] entries))
        {
            return RawFallback(
                transcript,
                DictionaryProcessingOutcome.RawFallbackInvalid);
        }

        if (transcript.Length == 0 || entries.Length == 0)
        {
            return new DictionaryProcessingResult(
                transcript,
                0,
                DictionaryProcessingOutcome.Unchanged);
        }

        if (HasTimedOut(stopwatch))
        {
            return RawFallback(
                transcript,
                DictionaryProcessingOutcome.RawFallbackTimeout);
        }

        try
        {
            MatchCollection protectedMatches = ProtectedToken().Matches(transcript);
            List<TextSpan> protectedSpans = new(protectedMatches.Count);
            foreach (Match match in protectedMatches)
            {
                if (HasTimedOut(stopwatch))
                {
                    return RawFallback(
                        transcript,
                        DictionaryProcessingOutcome.RawFallbackTimeout);
                }

                protectedSpans.Add(new TextSpan(match.Index, match.Length));
            }

            return ApplyEntries(
                transcript,
                entries,
                protectedSpans,
                stopwatch);
        }
        catch (RegexMatchTimeoutException)
        {
            return RawFallback(
                transcript,
                DictionaryProcessingOutcome.RawFallbackTimeout);
        }
        catch (ArgumentException)
        {
            return RawFallback(
                transcript,
                DictionaryProcessingOutcome.RawFallbackInvalid);
        }
    }

    private DictionaryProcessingResult ApplyEntries(
        string transcript,
        IReadOnlyList<PersonalDictionaryEntry> entries,
        IReadOnlyList<TextSpan> protectedSpans,
        Stopwatch stopwatch)
    {
        StringBuilder output = new(transcript.Length);
        int replacementCount = 0;
        int protectedIndex = 0;

        for (int sourceIndex = 0; sourceIndex < transcript.Length;)
        {
            if (HasTimedOut(stopwatch))
            {
                return RawFallback(
                    transcript,
                    DictionaryProcessingOutcome.RawFallbackTimeout);
            }

            while (protectedIndex < protectedSpans.Count &&
                   protectedSpans[protectedIndex].End <= sourceIndex)
            {
                protectedIndex++;
            }

            if (protectedIndex < protectedSpans.Count &&
                protectedSpans[protectedIndex].Start == sourceIndex)
            {
                TextSpan protectedSpan = protectedSpans[protectedIndex];
                output.Append(transcript, protectedSpan.Start, protectedSpan.Length);
                sourceIndex = protectedSpan.End;
                protectedIndex++;
                continue;
            }

            PersonalDictionaryEntry? matchingEntry = null;
            foreach (PersonalDictionaryEntry entry in entries)
            {
                if (!CouldMatchAt(transcript, sourceIndex, entry.SpokenForm) ||
                    OverlapsNextProtectedSpan(
                        sourceIndex,
                        entry.SpokenForm.Length,
                        protectedSpans,
                        protectedIndex))
                {
                    continue;
                }

                matchingEntry = entry;
                break;
            }

            if (matchingEntry is null)
            {
                output.Append(transcript[sourceIndex]);
                sourceIndex++;
                continue;
            }

            output.Append(matchingEntry.Replacement);
            sourceIndex += matchingEntry.SpokenForm.Length;
            replacementCount++;
        }

        return replacementCount == 0
            ? new DictionaryProcessingResult(
                transcript,
                0,
                DictionaryProcessingOutcome.Unchanged)
            : new DictionaryProcessingResult(
                output.ToString(),
                replacementCount,
                DictionaryProcessingOutcome.Applied);
    }

    private static bool TryPrepareEntries(
        IReadOnlyList<PersonalDictionaryEntry> snapshot,
        out PersonalDictionaryEntry[] entries)
    {
        entries = [];
        if (snapshot.Count > PersonalDictionaryValidation.MaximumEntryCount)
        {
            return false;
        }

        Dictionary<string, PersonalDictionaryEntry> uniqueEntries =
            new(StringComparer.OrdinalIgnoreCase);
        foreach (PersonalDictionaryEntry? candidate in snapshot)
        {
            if (candidate is null ||
                !PersonalDictionaryValidation.TryNormalize(
                    candidate.SpokenForm,
                    candidate.Replacement,
                    out PersonalDictionaryEntry? normalized,
                    out _))
            {
                return false;
            }

            if (!uniqueEntries.TryAdd(normalized!.SpokenForm, normalized))
            {
                return false;
            }
        }

        entries = uniqueEntries.Values
            .OrderByDescending(entry => entry.SpokenForm.Length)
            .ThenBy(entry => entry.SpokenForm, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.SpokenForm, StringComparer.Ordinal)
            .ToArray();
        return true;
    }

    private static bool CouldMatchAt(
        string transcript,
        int start,
        string spokenForm)
    {
        if (start + spokenForm.Length > transcript.Length ||
            !transcript.AsSpan(start, spokenForm.Length).Equals(
                spokenForm.AsSpan(),
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (StartsWithWordCharacter(spokenForm) &&
            IsWordCharacterBefore(transcript, start))
        {
            return false;
        }

        int end = start + spokenForm.Length;
        return !EndsWithWordCharacter(spokenForm) ||
               !IsWordCharacterAt(transcript, end);
    }

    private static bool StartsWithWordCharacter(string value)
    {
        OperationStatus status = Rune.DecodeFromUtf16(
            value.AsSpan(),
            out Rune rune,
            out _);
        return status != OperationStatus.Done || IsWordRune(rune);
    }

    private static bool EndsWithWordCharacter(string value)
    {
        OperationStatus status = Rune.DecodeLastFromUtf16(
            value.AsSpan(),
            out Rune rune,
            out _);
        return status != OperationStatus.Done || IsWordRune(rune);
    }

    private static bool IsWordCharacterBefore(string value, int index)
    {
        if (index <= 0)
        {
            return false;
        }

        OperationStatus status = Rune.DecodeLastFromUtf16(
            value.AsSpan(0, index),
            out Rune rune,
            out _);
        return status != OperationStatus.Done || IsWordRune(rune);
    }

    private static bool IsWordCharacterAt(string value, int index)
    {
        if (index >= value.Length)
        {
            return false;
        }

        OperationStatus status = Rune.DecodeFromUtf16(
            value.AsSpan(index),
            out Rune rune,
            out _);
        return status != OperationStatus.Done || IsWordRune(rune);
    }

    private static bool IsWordRune(Rune value)
    {
        UnicodeCategory category = Rune.GetUnicodeCategory(value);
        return Rune.IsLetterOrDigit(value) ||
               category is UnicodeCategory.NonSpacingMark or
                   UnicodeCategory.SpacingCombiningMark or
                   UnicodeCategory.ConnectorPunctuation;
    }

    private static bool OverlapsNextProtectedSpan(
        int start,
        int length,
        IReadOnlyList<TextSpan> protectedSpans,
        int protectedIndex)
    {
        if (protectedIndex >= protectedSpans.Count)
        {
            return false;
        }

        TextSpan next = protectedSpans[protectedIndex];
        int end = start + length;
        return start < next.End && end > next.Start;
    }

    private bool HasTimedOut(Stopwatch stopwatch)
    {
        return stopwatch.Elapsed >= _timeout;
    }

    private static DictionaryProcessingResult RawFallback(
        string transcript,
        DictionaryProcessingOutcome outcome)
    {
        return new DictionaryProcessingResult(transcript, 0, outcome);
    }

    private readonly record struct TextSpan(int Start, int Length)
    {
        public int End => Start + Length;
    }

    [GeneratedRegex(
        "`[^`\\r\\n]*`" +
        "|(?<![\\p{L}\\p{N}_])(?:[a-z][a-z0-9+.-]*://|www\\.)[^\\s`]+" +
        "|\"(?:[a-z]:\\\\|\\\\\\\\)[^\"\\r\\n<>|?*]+\"" +
        "|(?<![\\p{L}\\p{N}_])(?:[a-z]:\\\\|\\\\\\\\)[^\\s\\r\\n<>|?*]+" +
        "|(?<![\\p{L}\\p{N}_])(?:\\.{1,2}[\\/\\\\])+[^\\s`]+" +
        "|(?<![\\p{L}\\p{N}_])/(?:[^\\s/`]+/)*[^\\s`]+" +
        "|(?<![\\p{L}\\p{N}_])(?=[^\\s`]*(?:[\\p{L}\\p{N}_][.@:/\\\\][\\p{L}\\p{N}_]|[\\p{L}\\p{N}_]::[\\p{L}\\p{N}_]))[^\\s`]+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        250)]
    private static partial Regex ProtectedToken();
}
