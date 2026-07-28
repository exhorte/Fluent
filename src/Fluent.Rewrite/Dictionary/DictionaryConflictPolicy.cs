namespace Fluent.Rewrite.Dictionary;

/// <summary>
/// How an import resolves an incoming entry whose spoken form already exists.
/// Conflicts are always resolved explicitly, never silently.
/// </summary>
public enum DictionaryConflictPolicy
{
    /// <summary>Keep the existing entry; skip the incoming one.</summary>
    SkipExisting,

    /// <summary>Replace the existing entry with the incoming one.</summary>
    OverwriteExisting
}
