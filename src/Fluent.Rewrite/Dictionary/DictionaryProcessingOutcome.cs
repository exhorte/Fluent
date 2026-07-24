namespace Fluent.Rewrite.Dictionary;

public enum DictionaryProcessingOutcome
{
    Unchanged,
    Applied,
    RawFallbackInvalid,
    RawFallbackTimeout
}
