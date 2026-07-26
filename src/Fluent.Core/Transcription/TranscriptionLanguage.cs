namespace Fluent.Core.Transcription;

/// <summary>User preference for transcription language detection.</summary>
public enum TranscriptionLanguageMode
{
    Auto,
    French,
    English
}

/// <summary>Concrete transcription language. Never includes Auto.</summary>
public enum TranscriptionLanguage
{
    French,
    English
}
