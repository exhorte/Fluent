using Fluent.App.Models;

namespace Fluent.IntegrationTests;

public sealed class CapsuleLayoutMetricsTests
{
    // ── Circle metrics ─────────────────────────────────────────────

    [Fact]
    public void IdleCircleDiameter_is_positive()
    {
        Assert.True(CapsuleLayoutMetrics.IdleCircleDiameter > 0);
    }

    [Fact]
    public void IdleCircleSpacing_is_positive()
    {
        Assert.True(CapsuleLayoutMetrics.IdleCircleSpacing > 0);
    }

    [Fact]
    public void IdleCircleStrokeThickness_uses_1_dip()
    {
        Assert.Equal(1.0, CapsuleLayoutMetrics.IdleCircleStrokeThickness);
    }

    [Fact]
    public void IdleTotalWidth_is_three_circles_plus_spacing()
    {
        double expected = CapsuleLayoutMetrics.IdleCircleDiameter * 3
            + CapsuleLayoutMetrics.IdleCircleSpacing * 2;
        Assert.Equal(expected, CapsuleLayoutMetrics.IdleTotalWidth);
    }

    // ── Active pill metrics ────────────────────────────────────────

    [Fact]
    public void ActiveWidth_is_greater_than_ActiveHeight()
    {
        Assert.True(CapsuleLayoutMetrics.ActiveWidth > CapsuleLayoutMetrics.ActiveHeight);
    }

    [Fact]
    public void ActiveCornerRadius_is_half_ActiveHeight()
    {
        Assert.Equal(CapsuleLayoutMetrics.ActiveHeight / 2, CapsuleLayoutMetrics.ActiveCornerRadius);
    }

    [Fact]
    public void ActiveBorderThickness_is_positive()
    {
        Assert.True(CapsuleLayoutMetrics.ActiveBorderThickness > 0);
    }

    // ── Button metrics ─────────────────────────────────────────────

    [Fact]
    public void ButtonDiameter_is_positive()
    {
        Assert.True(CapsuleLayoutMetrics.ButtonDiameter > 0);
    }

    [Fact]
    public void ButtonDiameter_is_less_than_ActiveHeight()
    {
        Assert.True(CapsuleLayoutMetrics.ButtonDiameter < CapsuleLayoutMetrics.ActiveHeight);
    }

    // ── Waveform metrics ───────────────────────────────────────────

    [Fact]
    public void WaveformBarCount_is_positive()
    {
        Assert.True(CapsuleLayoutMetrics.WaveformBarCount > 0);
    }

    [Fact]
    public void WaveformBarMinHeight_is_less_than_max()
    {
        Assert.True(CapsuleLayoutMetrics.WaveformBarMinHeight < CapsuleLayoutMetrics.WaveformBarMaxHeight);
    }

    [Fact]
    public void WaveformBarMaxHeight_is_within_WaveformHeight()
    {
        Assert.True(CapsuleLayoutMetrics.WaveformBarMaxHeight <= CapsuleLayoutMetrics.WaveformHeight);
    }

    // ── Positioning metrics ────────────────────────────────────────

    [Fact]
    public void BottomOffset_is_positive()
    {
        Assert.True(CapsuleLayoutMetrics.BottomOffset > 0);
    }

    // ── Transition metrics ─────────────────────────────────────────

    [Fact]
    public void All_transition_durations_are_positive()
    {
        Assert.True(CapsuleLayoutMetrics.TransitionDurationMs > 0);
        Assert.True(CapsuleLayoutMetrics.ProcessingTransitionDurationMs > 0);
        Assert.True(CapsuleLayoutMetrics.IdleTransitionDurationMs > 0);
    }

    [Fact]
    public void Transition_durations_are_under_300ms()
    {
        Assert.True(CapsuleLayoutMetrics.TransitionDurationMs <= 300);
        Assert.True(CapsuleLayoutMetrics.ProcessingTransitionDurationMs <= 300);
        Assert.True(CapsuleLayoutMetrics.IdleTransitionDurationMs <= 300);
    }

    // ── Compactness checks ─────────────────────────────────────────

    [Fact]
    public void IdleTotalWidth_is_compact()
    {
        // Three 30px circles + spacing should be under ~130 DIPs
        Assert.True(CapsuleLayoutMetrics.IdleTotalWidth <= 130);
    }

    [Fact]
    public void ActiveWidth_is_compact()
    {
        Assert.True(CapsuleLayoutMetrics.ActiveWidth <= 132);
    }

    [Fact]
    public void ActiveHeight_is_compact()
    {
        Assert.True(CapsuleLayoutMetrics.ActiveHeight <= 36);
    }
}
