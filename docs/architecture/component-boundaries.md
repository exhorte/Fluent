# Component Boundaries

## Fluent.App

Composition root, WPF shell, tray integration, navigation, ViewModels, and native Supabase session composition. Its auth code uses the system browser plus a bounded 127.0.0.1 loopback callback, keeps access tokens in memory, and stores only refresh tokens through Windows Credential Manager. The Desktop may read one public backend origin from `FLUENT_BACKEND_URL` in its process environment; it accepts only a normalized HTTPS root origin and never loads `.env`, provider endpoints or provider credentials. `DashboardStatusPresentation` is a pure in-process formatter for existing profile, dictionary, session and Cloud-permission state; it owns no persistence, transport, identity or user content.

## Fluent.Core

Domain types, state machines, contracts, interfaces, and product invariants. It must not reference WPF, Win32, or Windows-only implementation assemblies.

## Fluent.Windows

Win32, UI Automation, focus, clipboard, input, and Windows integrity-level adapters.

## Fluent.Audio

Microphone access, buffers, RMS levels, audio format conversion, and resampling.

## Fluent.Speech

Whisper model loading and transcription orchestration.

## Fluent.Rewrite

Dictionary processing, profile rewriting, provider-agnostic rewrite contracts, the rewrite orchestrator, local and cloud provider abstractions, and output validation. It depends on no specific cloud provider.

## Fluent.Cloud

Desktop transport to the Fluent backend rewrite endpoint. It calls only the backend, never a provider directly, and holds no provider secret.

## Fluent.Backend

Minimal server-side rewrite endpoint: cryptographic Supabase JWT authentication through trusted issuer OIDC/JWKS discovery, rate limiting by validated user subject, request validation and provider dispatch. Gemini is the default Cloud provider; DeepSeek is selectable only after the existing Cloud gate and is default-deny unless its model, key and exact `https://api.deepseek.com` backend configuration are valid. It rejects symmetric/unknown algorithms and never trusts a raw bearer header for a quota partition. All provider keys and exact model values live here only, read from server process configuration.

## Fluent.Persistence

SQLite repositories, migrations, and local application data paths.
