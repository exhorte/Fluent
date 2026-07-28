namespace Fluent.Core.Interaction;

/// <summary>
/// Centralised Push-to-Talk constants. No magic numbers in the codebase.
/// </summary>
public static class PushToTalkConfiguration
{
    /// <summary>Minimum time the user must hold Ctrl+Win before recording
    /// starts. Shorter holds are treated as accidental and discarded.</summary>
    public static readonly TimeSpan MinimumHoldDuration = TimeSpan.FromMilliseconds(200);

    /// <summary>Minimum recording duration. Audio shorter than this is
    /// discarded (reuses the existing guard in the dictation pipeline).</summary>
    public static readonly TimeSpan MinimumRecordingDuration = TimeSpan.FromMilliseconds(250);

    /// <summary>Maximum recording duration. After this, recording stops
    /// automatically even if the keys are still held.</summary>
    public static readonly TimeSpan MaximumRecordingDuration = TimeSpan.FromMinutes(5);

    /// <summary>Delay after releasing keys before processing begins.
    /// Allows the final audio frames to flush.</summary>
    public static readonly TimeSpan StopProcessingDelay = TimeSpan.FromMilliseconds(50);
}
