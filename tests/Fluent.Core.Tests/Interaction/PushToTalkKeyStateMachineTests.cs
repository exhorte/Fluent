using Fluent.Core.Interaction;

namespace Fluent.Core.Tests.Interaction;

public sealed class PushToTalkKeyStateMachineTests
{
    private readonly PushToTalkKeyStateMachine _machine = new();

    // ── Start transitions ──────────────────────────────────────────

    [Fact]
    public void StartRequested_transitions_from_Idle_to_Arming()
    {
        bool ok = _machine.TryTransition(PushToTalkEvent.StartRequested);
        Assert.True(ok);
        Assert.Equal(PushToTalkState.Arming, _machine.State);
    }

    [Fact]
    public void StartRequested_is_ignored_when_not_Idle()
    {
        _machine.TryTransition(PushToTalkEvent.StartRequested);
        bool ok = _machine.TryTransition(PushToTalkEvent.StartRequested);
        Assert.False(ok);
        Assert.Equal(PushToTalkState.Arming, _machine.State);
    }

    [Fact]
    public void StartRequested_is_ignored_after_MinimumHoldReached()
    {
        _machine.TryTransition(PushToTalkEvent.StartRequested);
        _machine.TryTransition(PushToTalkEvent.MinimumHoldReached);
        bool ok = _machine.TryTransition(PushToTalkEvent.StartRequested);
        Assert.False(ok);
    }

    // ── Arming transitions ──────────────────────────────────────────

    [Fact]
    public void MinimumHoldReached_transitions_from_Arming_to_Recording()
    {
        _machine.TryTransition(PushToTalkEvent.StartRequested);
        bool ok = _machine.TryTransition(PushToTalkEvent.MinimumHoldReached);
        Assert.True(ok);
        Assert.Equal(PushToTalkState.Recording, _machine.State);
    }

    [Fact]
    public void StopRequested_during_Arming_transitions_to_Cancelled()
    {
        _machine.TryTransition(PushToTalkEvent.StartRequested);
        bool ok = _machine.TryTransition(PushToTalkEvent.StopRequested);
        Assert.True(ok);
        Assert.Equal(PushToTalkState.Cancelled, _machine.State);
    }

    [Fact]
    public void Cancelled_during_Arming_transitions_to_Cancelled()
    {
        _machine.TryTransition(PushToTalkEvent.StartRequested);
        bool ok = _machine.TryTransition(PushToTalkEvent.Cancelled);
        Assert.True(ok);
        Assert.Equal(PushToTalkState.Cancelled, _machine.State);
    }

    [Fact]
    public void Cancelled_can_reset_to_Idle()
    {
        _machine.TryTransition(PushToTalkEvent.StartRequested);
        _machine.TryTransition(PushToTalkEvent.Cancelled);
        bool ok = _machine.TryTransition(PushToTalkEvent.Cancelled);
        Assert.True(ok);
        Assert.Equal(PushToTalkState.Idle, _machine.State);
    }

    // ── Recording transitions ───────────────────────────────────────

    [Fact]
    public void StopRequested_transitions_from_Recording_to_Stopping()
    {
        GoToRecording();
        bool ok = _machine.TryTransition(PushToTalkEvent.StopRequested);
        Assert.True(ok);
        Assert.Equal(PushToTalkState.Stopping, _machine.State);
    }

    [Fact]
    public void MaximumDurationReached_transitions_from_Recording_to_Stopping()
    {
        GoToRecording();
        bool ok = _machine.TryTransition(PushToTalkEvent.MaximumDurationReached);
        Assert.True(ok);
        Assert.Equal(PushToTalkState.Stopping, _machine.State);
    }

    [Fact]
    public void StopRequested_ignored_when_not_Recording()
    {
        bool ok = _machine.TryTransition(PushToTalkEvent.StopRequested);
        Assert.False(ok);
    }

    [Fact]
    public void Second_StopRequested_is_ignored()
    {
        GoToRecording();
        _machine.TryTransition(PushToTalkEvent.StopRequested);
        bool ok = _machine.TryTransition(PushToTalkEvent.StopRequested);
        Assert.False(ok);
    }

    // ── Stopping → Transcribing → Rewriting → Inserting → Idle ──────

    [Fact]
    public void Full_pipeline_transitions_to_Idle()
    {
        GoToRecording();
        Assert.True(_machine.TryTransition(PushToTalkEvent.StopRequested));
        Assert.Equal(PushToTalkState.Stopping, _machine.State);

        Assert.True(_machine.TryTransition(PushToTalkEvent.AudioCaptured));
        Assert.Equal(PushToTalkState.Transcribing, _machine.State);

        Assert.True(_machine.TryTransition(PushToTalkEvent.TranscriptionCompleted));
        Assert.Equal(PushToTalkState.Rewriting, _machine.State);

        Assert.True(_machine.TryTransition(PushToTalkEvent.RewritingCompleted));
        Assert.Equal(PushToTalkState.Inserting, _machine.State);

        Assert.True(_machine.TryTransition(PushToTalkEvent.InsertionCompleted));
        Assert.Equal(PushToTalkState.Idle, _machine.State);
    }

    [Fact]
    public void New_cycle_possible_after_returning_to_Idle()
    {
        GoToRecording();
        _machine.TryTransition(PushToTalkEvent.StopRequested);
        _machine.TryTransition(PushToTalkEvent.AudioCaptured);
        _machine.TryTransition(PushToTalkEvent.TranscriptionCompleted);
        _machine.TryTransition(PushToTalkEvent.RewritingCompleted);
        _machine.TryTransition(PushToTalkEvent.InsertionCompleted);

        bool ok = _machine.TryTransition(PushToTalkEvent.StartRequested);
        Assert.True(ok);
        Assert.Equal(PushToTalkState.Arming, _machine.State);
    }

    // ── Error paths ─────────────────────────────────────────────────

    [Fact]
    public void Error_in_Stopping_transitions_to_Failed()
    {
        GoToRecording();
        _machine.TryTransition(PushToTalkEvent.StopRequested);
        bool ok = _machine.TryTransition(PushToTalkEvent.Error);
        Assert.True(ok);
        Assert.Equal(PushToTalkState.Failed, _machine.State);
    }

    [Fact]
    public void Error_in_Transcribing_transitions_to_Failed()
    {
        GoToRecording();
        _machine.TryTransition(PushToTalkEvent.StopRequested);
        _machine.TryTransition(PushToTalkEvent.AudioCaptured);
        bool ok = _machine.TryTransition(PushToTalkEvent.Error);
        Assert.True(ok);
        Assert.Equal(PushToTalkState.Failed, _machine.State);
    }

    [Fact]
    public void Error_in_Rewriting_transitions_to_Failed()
    {
        GoToRecording();
        _machine.TryTransition(PushToTalkEvent.StopRequested);
        _machine.TryTransition(PushToTalkEvent.AudioCaptured);
        _machine.TryTransition(PushToTalkEvent.TranscriptionCompleted);
        bool ok = _machine.TryTransition(PushToTalkEvent.Error);
        Assert.True(ok);
        Assert.Equal(PushToTalkState.Failed, _machine.State);
    }

    [Fact]
    public void Error_in_Inserting_transitions_to_Failed()
    {
        GoToRecording();
        _machine.TryTransition(PushToTalkEvent.StopRequested);
        _machine.TryTransition(PushToTalkEvent.AudioCaptured);
        _machine.TryTransition(PushToTalkEvent.TranscriptionCompleted);
        _machine.TryTransition(PushToTalkEvent.RewritingCompleted);
        bool ok = _machine.TryTransition(PushToTalkEvent.Error);
        Assert.True(ok);
        Assert.Equal(PushToTalkState.Failed, _machine.State);
    }

    [Fact]
    public void Failed_can_reset_to_Idle()
    {
        GoToRecording();
        _machine.TryTransition(PushToTalkEvent.StopRequested);
        _machine.TryTransition(PushToTalkEvent.Error);
        Assert.Equal(PushToTalkState.Failed, _machine.State);

        bool ok = _machine.TryTransition(PushToTalkEvent.Error);
        Assert.True(ok);
        Assert.Equal(PushToTalkState.Idle, _machine.State);
    }

    // ── IsBusy / IsRecording ────────────────────────────────────────

    [Fact]
    public void IsBusy_is_false_at_Idle()
    {
        Assert.False(_machine.IsBusy);
        Assert.False(_machine.IsRecording);
    }

    [Fact]
    public void IsRecording_is_true_during_Arming()
    {
        _machine.TryTransition(PushToTalkEvent.StartRequested);
        Assert.True(_machine.IsRecording);
    }

    [Fact]
    public void IsRecording_is_true_during_Recording()
    {
        GoToRecording();
        Assert.True(_machine.IsRecording);
    }

    [Fact]
    public void IsRecording_is_false_after_Stopping()
    {
        GoToRecording();
        _machine.TryTransition(PushToTalkEvent.StopRequested);
        Assert.False(_machine.IsRecording);
    }

    [Fact]
    public void ResetToIdle_clears_everything()
    {
        GoToRecording();
        _machine.ResetToIdle();
        Assert.Equal(PushToTalkState.Idle, _machine.State);
        Assert.False(_machine.IsBusy);
    }

    // ── Helpers ─────────────────────────────────────────────────────

    private void GoToRecording()
    {
        Assert.True(_machine.TryTransition(PushToTalkEvent.StartRequested));
        Assert.True(_machine.TryTransition(PushToTalkEvent.MinimumHoldReached));
    }
}
