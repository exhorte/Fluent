---
paths:
  - "**/*.cs"
  - "**/*.csproj"
  - "**/*.props"
---

# .NET And C# Rules

- Nullable reference types stay enabled.
- Async APIs accept `CancellationToken` where cancellation matters.
- Avoid `async void` except UI events.
- Dispose `IDisposable` and `IAsyncDisposable` correctly.
- Do not swallow exceptions.
- Native boundaries return explicit results.
- Avoid global mutable state.
- Add tests for business rules.
