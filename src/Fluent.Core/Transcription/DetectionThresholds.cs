namespace Fluent.Core.Transcription;

/// <summary>
/// Centralised detection thresholds for automatic language detection.
/// These are internal constants that may be calibrated after real-world smoke tests.
/// </summary>
public static class DetectionThresholds
{
    /// <summary>Minimum confidence for a detection to be considered reliable.</summary>
    public const double MinimumConfidence = 0.65;

    /// <summary>Minimum margin between top and second candidate to avoid ambiguity.</summary>
    public const double MinimumMargin = 0.15;

    /// <summary>Audio shorter than this (seconds) may not produce reliable detection.</summary>
    public const double MinimumAudioDurationSeconds = 0.8;

    /// <summary>Sample window used for detection when a shorter pass is preferred.</summary>
    public const int DetectionSampleWindow = 16000 * 5; // 5 seconds at 16 kHz
}
