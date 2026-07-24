namespace Fluent.Rewrite.Dictionary;

public sealed record DictionaryProcessingResult(
    string Text,
    int ReplacementCount,
    DictionaryProcessingOutcome Outcome)
{
    public bool WasApplied => Outcome == DictionaryProcessingOutcome.Applied;
}
