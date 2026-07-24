---
paths:
  - "src/**"
  - "tests/**"
  - "docs/architecture/**"
---

# Architecture Rules

- Dependencies point toward `Fluent.Core`.
- `Fluent.Core` must not depend on WPF or Win32.
- Win32 belongs in `Fluent.Windows`.
- Use interfaces at boundaries.
- Do not use a service locator.
- Do not place business logic in UI code.
