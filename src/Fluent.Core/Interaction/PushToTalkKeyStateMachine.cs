namespace Fluent.Core.Interaction;

/// <summary>
/// Deterministic finite-state machine for Ctrl+Win Push-to-Talk keyboard
/// tracking. This class has zero Win32 dependencies and is fully testable.
///
/// Thread-safety: all public methods lock on a private sync object, so the
/// machine can be driven from the low-level keyboard hook callback (which
/// runs on an arbitrary thread) and queried from the UI thread concurrently.
/// </summary>
public sealed class PushToTalkKeyStateMachine
{
    private readonly object _sync = new();
    private PushToTalkState _state = PushToTalkState.Idle;

    /// <summary>Current state (thread-safe snapshot).</summary>
    public PushToTalkState State
    {
        get { lock (_sync) return _state; }
    }

    /// <summary>
    /// Attempt to transition the state machine with the given event.
    /// Returns true if the transition was valid and applied; false if the
    /// event is invalid in the current state (silently ignored).
    /// </summary>
    public bool TryTransition(PushToTalkEvent evt)
    {
        lock (_sync)
        {
            PushToTalkState? next = (_state, evt) switch
            {
                // ── Idle ──────────────────────────────────────────
                (PushToTalkState.Idle, PushToTalkEvent.StartRequested)
                    => PushToTalkState.Arming,

                // ── Arming ────────────────────────────────────────
                (PushToTalkState.Arming, PushToTalkEvent.MinimumHoldReached)
                    => PushToTalkState.Recording,
                (PushToTalkState.Arming, PushToTalkEvent.StopRequested)
                    => PushToTalkState.Cancelled,
                (PushToTalkState.Arming, PushToTalkEvent.Cancelled)
                    => PushToTalkState.Cancelled,

                // ── Recording ─────────────────────────────────────
                (PushToTalkState.Recording, PushToTalkEvent.StopRequested)
                    => PushToTalkState.Stopping,
                (PushToTalkState.Recording, PushToTalkEvent.MaximumDurationReached)
                    => PushToTalkState.Stopping,

                // ── Stopping ──────────────────────────────────────
                (PushToTalkState.Stopping, PushToTalkEvent.AudioCaptured)
                    => PushToTalkState.Transcribing,
                (PushToTalkState.Stopping, PushToTalkEvent.Error)
                    => PushToTalkState.Failed,

                // ── Transcribing ──────────────────────────────────
                (PushToTalkState.Transcribing, PushToTalkEvent.TranscriptionCompleted)
                    => PushToTalkState.Rewriting,
                (PushToTalkState.Transcribing, PushToTalkEvent.Error)
                    => PushToTalkState.Failed,

                // ── Rewriting ─────────────────────────────────────
                (PushToTalkState.Rewriting, PushToTalkEvent.RewritingCompleted)
                    => PushToTalkState.Inserting,
                (PushToTalkState.Rewriting, PushToTalkEvent.Error)
                    => PushToTalkState.Failed,

                // ── Inserting ─────────────────────────────────────
                (PushToTalkState.Inserting, PushToTalkEvent.InsertionCompleted)
                    => PushToTalkState.Idle,
                (PushToTalkState.Inserting, PushToTalkEvent.Error)
                    => PushToTalkState.Failed,

                // ── Failed / Cancelled → reset ────────────────────
                (PushToTalkState.Failed, PushToTalkEvent.Error)
                    => PushToTalkState.Idle,
                (PushToTalkState.Cancelled, PushToTalkEvent.Cancelled)
                    => PushToTalkState.Idle,

                // ── Anything else is invalid ──────────────────────
                _ => null
            };

            if (next is null)
            {
                return false;
            }

            _state = next.Value;
            return true;
        }
    }

    /// <summary>Force-reset to Idle (e.g. on shutdown or unrecoverable error).</summary>
    public void ResetToIdle()
    {
        lock (_sync)
        {
            _state = PushToTalkState.Idle;
        }
    }

    /// <summary>True when the machine is in a processing state (not Idle).</summary>
    public bool IsBusy
    {
        get
        {
            lock (_sync)
            {
                return _state != PushToTalkState.Idle;
            }
        }
    }

    /// <summary>True when recording is active (Arming or Recording).</summary>
    public bool IsRecording
    {
        get
        {
            lock (_sync)
            {
                return _state is PushToTalkState.Arming
                    or PushToTalkState.Recording;
            }
        }
    }
}
