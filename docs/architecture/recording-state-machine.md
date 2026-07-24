# Recording State Machine

Planned happy path:

```text
Idle
-> TargetLocked
-> Recording
-> Transcribing
-> Rewriting
-> Validating
-> Inserting
-> Completed
-> Idle
```

Alternative terminal or recovery states:

- Cancelled
- TargetLost
- ClipboardFallback
- Failed

Rules:

- Target identity is captured before recording.
- Insertion is allowed only if the original target remains valid.
- Failure never triggers command execution or Enter.
