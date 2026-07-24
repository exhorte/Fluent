namespace Fluent.Core.Diagnostics;

/// <summary>
/// The stage of a dictation at which a failure surfaced. Used to present a safe,
/// non-technical message with recovery guidance instead of a raw exception.
/// </summary>
public enum DictationFailureStage
{
    Microphone,
    Transcription,
    Rewriting,
    Insertion,
    Unknown
}
