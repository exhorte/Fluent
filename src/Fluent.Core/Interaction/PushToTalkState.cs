namespace Fluent.Core.Interaction;

/// <summary>
/// States of the Push-to-Talk key state machine. Transitions are thread-safe
/// and idempotent — duplicate events are silently ignored.
/// </summary>
public enum PushToTalkState
{
    /// <summary>Awaiting Ctrl+Win combination. Default state.</summary>
    Idle,

    /// <summary>Ctrl and Win are both held but the minimum hold duration
    /// has not yet elapsed. If released before the threshold, the state
    /// resets to Idle without triggering recording.</summary>
    Arming,

    /// <summary>Recording is active. The user is holding Ctrl+Win and
    /// speaking into the microphone.</summary>
    Recording,

    /// <summary>One or both keys have been released; the stop has been
    /// requested but audio capture may not have completed yet.</summary>
    Stopping,

    /// <summary>Audio has been captured and transcription is in progress.</summary>
    Transcribing,

    /// <summary>Transcription is complete; rewriting (dictionary + profile)
    /// is in progress.</summary>
    Rewriting,

    /// <summary>Rewriting is complete; the final text is being inserted
    /// into the target application.</summary>
    Inserting,

    /// <summary>A non-recoverable error occurred. The state machine will
    /// transition back to Idle after cleanup.</summary>
    Failed,

    /// <summary>The user cancelled the operation (e.g., released keys
    /// before the minimum hold duration).</summary>
    Cancelled
}
