using Fluent.Core.Diagnostics;

namespace Fluent.Core.Tests.Diagnostics;

public sealed class DictationErrorPresenterTests
{
    [Fact]
    public void Every_stage_has_a_non_empty_message_and_recovery()
    {
        foreach (DictationFailureStage stage in Enum.GetValues<DictationFailureStage>())
        {
            UserFacingMessage message = DictationErrorPresenter.Describe(stage);

            Assert.False(string.IsNullOrWhiteSpace(message.Message), $"Message empty for {stage}.");
            Assert.False(string.IsNullOrWhiteSpace(message.Recovery), $"Recovery empty for {stage}.");
            Assert.Equal($"{message.Message} {message.Recovery}", message.Combined);
        }
    }

    [Fact]
    public void No_stage_leaks_technical_or_sensitive_tokens()
    {
        string[] forbidden =
        [
            "Exception", "System.", "0x", "HRESULT", "stack", "COMException",
            "null", "Sqlite", "Http", "Token"
        ];

        foreach (DictationFailureStage stage in Enum.GetValues<DictationFailureStage>())
        {
            UserFacingMessage message = DictationErrorPresenter.Describe(stage);
            string combined = message.Combined;

            foreach (string token in forbidden)
            {
                Assert.DoesNotContain(token, combined, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Unknown_stage_gives_the_generic_recovery()
    {
        UserFacingMessage message = DictationErrorPresenter.Describe(DictationFailureStage.Unknown);

        Assert.Contains("erreur est survenue", message.Message, StringComparison.Ordinal);
        Assert.Contains("redémarrez Fluent", message.Recovery, StringComparison.Ordinal);
    }

    [Fact]
    public void Insertion_recovery_mentions_the_clipboard_fallback()
    {
        UserFacingMessage message = DictationErrorPresenter.Describe(DictationFailureStage.Insertion);

        Assert.Contains("Ctrl+V", message.Recovery, StringComparison.Ordinal);
    }
}
