namespace Fluent.App.Services;

/// <summary>
/// Computes the screen position for the recording capsule overlay window
/// with multi-monitor, DPI, and taskbar awareness.
/// </summary>
public interface ICapsulePositionService
{
    /// <summary>
    /// Returns the recommended position for a capsule of the given
    /// <paramref name="capsuleWidth"/> and <paramref name="capsuleHeight"/>
    /// in device-independent pixels (DIPs).
    /// </summary>
    CapsulePosition GetPosition(double capsuleWidth, double capsuleHeight);
}

/// <summary>
/// Screen position in device-independent pixels (DIPs).
/// </summary>
public readonly record struct CapsulePosition(double Left, double Top);
