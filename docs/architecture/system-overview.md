# System Overview

Planned flow:

```text
Ctrl+Space
-> GlobalHotkeyService
-> TargetDetector
-> RecordingCoordinator
-> MicrophoneCapture
-> SpeechToText
-> DictionaryProcessor
-> ProfileRewriter
-> OutputValidator
-> TextInserter
-> HistoryRepository
```

Phase 00 creates the skeleton only. The flow is architectural intent, not implemented behavior.
