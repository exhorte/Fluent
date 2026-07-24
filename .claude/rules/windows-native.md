---
paths:
  - "src/Fluent.Windows/**"
  - "tests/Fluent.Windows.Tests/**"
---

# Windows Native Rules

- Isolate P/Invoke.
- Check return codes.
- Convert Win32 errors to explicit results.
- Use `SafeHandle` where needed.
- Provide tests or manual harness evidence.
- Do not paste if the target changed.
