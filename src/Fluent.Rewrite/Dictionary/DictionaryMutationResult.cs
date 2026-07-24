namespace Fluent.Rewrite.Dictionary;

public sealed record DictionaryMutationResult(
    DictionaryMutationOutcome Outcome,
    string Message)
{
    public bool Succeeded => Outcome is
        DictionaryMutationOutcome.Added or
        DictionaryMutationOutcome.Updated or
        DictionaryMutationOutcome.Removed;
}
