namespace Fluent.Core;

public static class FluentPrinciples
{
    public static IReadOnlyList<ProductPrinciple> All { get; } =
    [
        new("P-001", "Windows only for the first MVP."),
        new("P-002", "Local-first; no mandatory cloud service."),
        new("P-003", "No audio is saved by default."),
        new("P-004", "No telemetry."),
        new("P-005", "Never paste automatically into a password field."),
        new("P-006", "Never send Enter automatically."),
        new("P-007", "Never execute a dictated command."),
        new("P-008", "The floating window must not steal focus."),
        new("P-009", "If the initial target disappears or changes, copy to clipboard and show an explicit indication instead of pasting into a new target."),
        new("P-010", "Rewriting must never invent information."),
        new("P-011", "Numbers, proper nouns, URLs, paths, versions, commands, and identifiers must be preserved."),
        new("P-012", "Win32 calls must be isolated behind testable interfaces."),
        new("P-013", "No major change without written acceptance criteria."),
        new("P-014", "A phase cannot be completed only because code compiles."),
        new("P-015", "Versioned documents are the source of truth."),
        new("P-016", "Automatic model memory is not canonical."),
        new("P-017", "No destructive or external operation without appropriate authorization."),
        new("P-018", "The Judge cannot modify the rules that limit its own powers.")
    ];
}
