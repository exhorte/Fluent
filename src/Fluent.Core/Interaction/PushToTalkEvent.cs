namespace Fluent.Core.Interaction;

/// <summary>
/// Events produced by the low-level keyboard hook and consumed by the
/// Push-to-Talk state machine.
/// </summary>
public enum PushToTalkEvent
{
    /// <summary>Both Ctrl and Win are now held down. Emitted once per
    /// activation, even if key-repeat messages arrive.</summary>
    StartRequested,

    /// <summary>Either Ctrl or Win has been released. Emitted once per
    /// deactivation.</summary>
    StopRequested,

    /// <summary>The minimum hold duration has elapsed while both keys
    /// remained held. Recording should begin.</summary>
    MinimumHoldReached,

    /// <summary>The maximum recording duration has been reached.
    /// Recording must stop even though the keys are still held.</summary>
    MaximumDurationReached,

    /// <summary>Audio capture has completed and the recorded audio is
    /// ready for transcription.</summary>
    AudioCaptured,

    /// <summary>Transcription has completed successfully.</summary>
    TranscriptionCompleted,

    /// <summary>Rewriting has completed successfully.</summary>
    RewritingCompleted,

    /// <summary>Text insertion has completed (success or failure).</summary>
    InsertionCompleted,

    /// <summary>An error occurred at any stage. The state machine should
    /// clean up and return to Idle.</summary>
    Error,

    /// <summary>The operation was cancelled (e.g., keys released before
    /// minimum hold).</summary>
    Cancelled
}
