# Privacy Principles

- Fluent est local-first par défaut.
- Audio is not saved by default.
- History must be optional, transparent, local, and deletable.
- Telemetry is forbidden.
- Secrets must not be read, logged, or stored.
- Cloud use, if ever introduced, requires explicit future consent, documentation, and user approval.
- Logs must contain operational evidence, not sensitive user content.

# Phase 07B authentication boundary

- Fluent opens Google authentication only in the system browser; it does not embed a browser surface.
- Access tokens, authorization codes, PKCE verifiers and OAuth state are held only in memory and are never logged or shown in the interface.
- A Supabase refresh token may be stored only in Windows Credential Manager for the current Windows user. Signing out or a definitive refresh rejection removes it.
- The public Supabase URL and publishable key may come from process configuration. `.env`, service-role values, OAuth client secrets, JWT signing secrets and provider API keys are never Desktop configuration.
- The public Fluent backend origin may come only from `FLUENT_BACKEND_URL` in the Desktop process. It is not a provider endpoint or credential, must be a strict HTTPS root origin, and does not enable Cloud or consent by itself.
- Signing in alone never sends dictation text to the Cloud. Cloud rewriting remains subject to authentication, explicit enablement, session consent, backend availability and an explicitly selected available provider; Gemini is the session default and DeepSeek requires a server-only valid configuration. Audio always remains local.
