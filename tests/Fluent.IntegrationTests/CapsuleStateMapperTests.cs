using Fluent.App.Models;
using Fluent.Core.Interaction;

namespace Fluent.IntegrationTests;

public sealed class CapsuleStateMapperTests
{
    // ── Idle state mappings ────────────────────────────────────────

    [Fact]
    public void Idle_maps_to_Idle()
    {
        Assert.Equal(CapsuleVisualState.Idle, CapsuleStateMapper.Map(PushToTalkState.Idle));
    }

    [Fact]
    public void Cancelled_maps_to_Idle()
    {
        Assert.Equal(CapsuleVisualState.Idle, CapsuleStateMapper.Map(PushToTalkState.Cancelled));
    }

    // ── Recording state mappings ───────────────────────────────────

    [Fact]
    public void Arming_maps_to_Recording()
    {
        Assert.Equal(CapsuleVisualState.Recording, CapsuleStateMapper.Map(PushToTalkState.Arming));
    }

    [Fact]
    public void Recording_maps_to_Recording()
    {
        Assert.Equal(CapsuleVisualState.Recording, CapsuleStateMapper.Map(PushToTalkState.Recording));
    }

    // ── Processing state mappings ──────────────────────────────────

    [Fact]
    public void Stopping_maps_to_Processing()
    {
        Assert.Equal(CapsuleVisualState.Processing, CapsuleStateMapper.Map(PushToTalkState.Stopping));
    }

    [Fact]
    public void Transcribing_maps_to_Processing()
    {
        Assert.Equal(CapsuleVisualState.Processing, CapsuleStateMapper.Map(PushToTalkState.Transcribing));
    }

    [Fact]
    public void Rewriting_maps_to_Processing()
    {
        Assert.Equal(CapsuleVisualState.Processing, CapsuleStateMapper.Map(PushToTalkState.Rewriting));
    }

    [Fact]
    public void Inserting_maps_to_Processing()
    {
        Assert.Equal(CapsuleVisualState.Processing, CapsuleStateMapper.Map(PushToTalkState.Inserting));
    }

    // ── Error state mapping ────────────────────────────────────────

    [Fact]
    public void Failed_maps_to_Error()
    {
        Assert.Equal(CapsuleVisualState.Error, CapsuleStateMapper.Map(PushToTalkState.Failed));
    }

    // ── Unknown state fallback ─────────────────────────────────────

    [Fact]
    public void Unknown_state_falls_back_to_Idle()
    {
        // Cast an invalid integer to PushToTalkState to simulate unknown state.
        var unknown = (PushToTalkState)999;
        Assert.Equal(CapsuleVisualState.Idle, CapsuleStateMapper.Map(unknown));
    }
}
