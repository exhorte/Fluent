namespace Fluent.Rewrite.Profiles;

public sealed record ProfileSelectionResult(
    bool Succeeded,
    RewriteProfile Selected,
    string Message);
