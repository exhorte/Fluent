using System.Buffers;
using System.Globalization;
using System.Text;
using Fluent.Core.Dictionary;

namespace Fluent.Rewrite.Dictionary;

internal static class PersonalDictionaryValidation
{
    public const int MaximumEntryCount =
        PersonalDictionaryLimits.MaximumEntryCount;
    public const int MaximumSpokenFormLength =
        PersonalDictionaryLimits.MaximumSpokenFormLength;
    public const int MaximumReplacementLength =
        PersonalDictionaryLimits.MaximumReplacementLength;

    public static bool TryNormalize(
        string? spokenForm,
        string? replacement,
        out PersonalDictionaryEntry? entry,
        out string message)
    {
        entry = null;

        if (spokenForm is null || replacement is null)
        {
            message = "Les deux champs sont obligatoires.";
            return false;
        }

        string normalizedSpokenForm = spokenForm.Trim();
        string normalizedReplacement = replacement.Trim();

        if (normalizedSpokenForm.Length == 0 || normalizedReplacement.Length == 0)
        {
            message = "Les deux champs sont obligatoires.";
            return false;
        }

        if (normalizedSpokenForm.Length > MaximumSpokenFormLength)
        {
            message = $"La forme prononcée est limitée à {MaximumSpokenFormLength} caractères.";
            return false;
        }

        if (normalizedReplacement.Length > MaximumReplacementLength)
        {
            message = $"Le remplacement est limité à {MaximumReplacementLength} caractères.";
            return false;
        }

        if (ContainsForbiddenUnicode(normalizedSpokenForm) ||
            ContainsForbiddenUnicode(normalizedReplacement))
        {
            message = "Les caractères invisibles, de contrôle ou Unicode invalides ne sont pas autorisés.";
            return false;
        }

        if (!ContainsLetterOrDigit(normalizedSpokenForm))
        {
            message = "La forme prononcée doit contenir au moins une lettre ou un chiffre.";
            return false;
        }

        if (string.Equals(
                normalizedSpokenForm,
                normalizedReplacement,
                StringComparison.Ordinal))
        {
            message = "La correction doit modifier le texte prononcé.";
            return false;
        }

        entry = new PersonalDictionaryEntry(
            normalizedSpokenForm,
            normalizedReplacement);
        message = string.Empty;
        return true;
    }

    private static bool ContainsForbiddenUnicode(string value)
    {
        for (int index = 0; index < value.Length;)
        {
            OperationStatus status = Rune.DecodeFromUtf16(
                value.AsSpan(index),
                out Rune rune,
                out int consumed);
            if (status != OperationStatus.Done)
            {
                return true;
            }

            UnicodeCategory category = Rune.GetUnicodeCategory(rune);
            if (category is UnicodeCategory.Control or
                UnicodeCategory.Format or
                UnicodeCategory.LineSeparator or
                UnicodeCategory.ParagraphSeparator)
            {
                return true;
            }

            index += consumed;
        }

        return false;
    }

    private static bool ContainsLetterOrDigit(string value)
    {
        for (int index = 0; index < value.Length;)
        {
            OperationStatus status = Rune.DecodeFromUtf16(
                value.AsSpan(index),
                out Rune rune,
                out int consumed);
            if (status != OperationStatus.Done)
            {
                return false;
            }

            if (Rune.IsLetterOrDigit(rune))
            {
                return true;
            }

            index += consumed;
        }

        return false;
    }
}
