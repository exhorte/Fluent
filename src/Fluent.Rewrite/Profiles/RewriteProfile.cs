namespace Fluent.Rewrite.Profiles;

public sealed record RewriteProfile(
    string Id,
    string DisplayName,
    string Instructions,
    bool IsAvailable = true,
    string Description = "",
    string UnavailableReason = "");
