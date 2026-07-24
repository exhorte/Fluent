namespace Fluent.Windows.Input;

public readonly record struct KeyboardInputSendResult(
    bool Succeeded,
    uint SentInputCount,
    uint RequestedInputCount,
    int ErrorCode);
