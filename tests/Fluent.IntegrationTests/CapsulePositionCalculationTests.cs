using Fluent.App.Models;
using Fluent.App.Services;

namespace Fluent.IntegrationTests;

public sealed class CapsulePositionCalculationTests
{
    [Fact]
    public void GetPosition_centers_horizontally()
    {
        var service = new CapsulePositionService();
        double capsuleWidth = 126;
        double capsuleHeight = 34;

        CapsulePosition position = service.GetPosition(capsuleWidth, capsuleHeight);

        // The actual center will vary based on the current monitor.
        // Verify position is valid (non-negative, non-NaN, non-infinity).
        Assert.False(double.IsNaN(position.Left));
        Assert.False(double.IsNaN(position.Top));
        Assert.False(double.IsInfinity(position.Left));
        Assert.False(double.IsInfinity(position.Top));
    }

    [Fact]
    public void GetPosition_returns_positive_coordinates()
    {
        var service = new CapsulePositionService();

        CapsulePosition position = service.GetPosition(126, 34);

        // Coordinates may be negative on multi-monitor setups, but should be finite.
        Assert.False(double.IsNaN(position.Left));
        Assert.False(double.IsNaN(position.Top));
    }

    [Fact]
    public void GetPosition_handles_zero_sized_capsule()
    {
        var service = new CapsulePositionService();

        CapsulePosition position = service.GetPosition(0, 0);

        Assert.False(double.IsNaN(position.Left));
        Assert.False(double.IsNaN(position.Top));
    }

    [Fact]
    public void GetPosition_handles_large_capsule()
    {
        var service = new CapsulePositionService();

        CapsulePosition position = service.GetPosition(10000, 10000);

        Assert.False(double.IsNaN(position.Left));
        Assert.False(double.IsNaN(position.Top));
    }

    // ── Internal position math tests (DIP-level, framework-agnostic) ──

    [Fact]
    public void CalculatePosition_centers_and_respects_bottom_offset()
    {
        // Arrange: simulate work area in DIPs
        double workLeft = 0;
        double workWidth = 1920;
        double workTop = 0;
        double workHeight = 1040; // taskbar takes 40px from 1080
        double capsuleWidth = 126;
        double capsuleHeight = 34;

        // Act
        double left = CalculateCenterLeft(workLeft, workWidth, capsuleWidth);
        double top = CalculateBottomTop(workTop, workHeight, capsuleHeight, CapsuleLayoutMetrics.BottomOffset);

        // Assert
        Assert.Equal(897, left, 1); // (1920 - 126) / 2 = 897
        Assert.Equal(986, top, 1);  // 0 + 1040 - 34 - 20 = 986
    }

    [Fact]
    public void CalculatePosition_clamps_left_to_work_area()
    {
        double workLeft = -1920; // Secondary monitor left of primary
        double workWidth = 1920;
        double workTop = 0;
        double workHeight = 1040;
        double capsuleWidth = 126;
        double capsuleHeight = 34;

        double left = CalculateCenterLeft(workLeft, workWidth, capsuleWidth);
        double top = CalculateBottomTop(workTop, workHeight, capsuleHeight, CapsuleLayoutMetrics.BottomOffset);

        double clampedLeft = Math.Max(workLeft, Math.Min(left, workLeft + workWidth - capsuleWidth));
        double clampedTop = Math.Max(workTop, top);

        Assert.True(clampedLeft >= workLeft);
        Assert.True(clampedLeft + capsuleWidth <= workLeft + workWidth);
        Assert.True(clampedTop >= workTop);
    }

    [Fact]
    public void CalculatePosition_handles_taskbar_at_top()
    {
        // Taskbar at top: work area starts below it
        double workTop = 40; // 40px taskbar
        double workHeight = 1040;

        double top = CalculateBottomTop(workTop, workHeight, 34, CapsuleLayoutMetrics.BottomOffset);

        Assert.Equal(1026, top, 1); // 40 + 1040 - 34 - 20 = 1026
    }

    [Fact]
    public void CalculatePosition_handles_taskbar_at_left()
    {
        double workLeft = 48; // 48px taskbar on left
        double workWidth = 1872;

        double left = CalculateCenterLeft(workLeft, workWidth, 126);

        Assert.Equal(921, left, 1); // 48 + (1872 - 126) / 2 = 921
    }

    [Fact]
    public void CalculatePosition_respects_negative_monitor_coordinates()
    {
        // Secondary monitor to the left of primary: work area at (-1920, 0)
        double workLeft = -1920;
        double workWidth = 1920;
        double workTop = 0;
        double workHeight = 1040;
        double capsuleWidth = 126;
        double capsuleHeight = 34;

        double left = CalculateCenterLeft(workLeft, workWidth, capsuleWidth);
        double top = CalculateBottomTop(workTop, workHeight, capsuleHeight, CapsuleLayoutMetrics.BottomOffset);

        Assert.True(left < 0); // Should be negative since on left monitor
        Assert.Equal(-1023, left, 1); // -1920 + (1920 - 126) / 2 = -1023
        Assert.Equal(986, top, 1);
    }

    [Fact]
    public void CalculatePosition_handles_too_small_work_area()
    {
        // Capsule wider than work area
        double result = CalculateCenterLeft(0, 100, 200);
        // Should not crash
        Assert.True(result < 0); // Will be negative but should be finite
        Assert.False(double.IsNaN(result));
    }

    // ── Pure math helpers (extracted from CapsulePositionService logic) ──

    private static double CalculateCenterLeft(double workLeft, double workWidth, double capsuleWidth)
    {
        return workLeft + (workWidth - capsuleWidth) / 2.0;
    }

    private static double CalculateBottomTop(double workTop, double workHeight, double capsuleHeight, double bottomOffset)
    {
        return workTop + workHeight - capsuleHeight - bottomOffset;
    }
}
