# Phase 06B - Optional Cloud Rewrite Engine

Status: IMPLEMENTED_AWAITING_USER_REVIEW (final Development Judge ALLOW to present, 2026-07-19; not closed)

## Objective

Add an optional Cloud rewrite engine, mediated by a Fluent backend, that stays disabled unless the user is authenticated AND has explicitly enabled and consented to Cloud rewriting. Local rewriting remains the default and is unchanged. The Desktop holds no provider API keys. Gemini is the default and only active provider; DeepSeek (V4 Pro) is architecturally prepared but disabled and never called.

## Included

- Provider-agnostic domain contracts in `Fluent.Rewrite`: `IRewriteProvider`, `RewriteProviderId`, `RewriteProviderCapabilities`, and provider-level `RewriteRequest`, `RewriteResult`, `RewriteStatus`, `RewriteFailureReason`, `RewriteValidationResult`.
- `LocalRewriteProvider` adapting the accepted safe local rewrite; local behavior is byte-for-byte preserved.
- `CloudRewriteProvider` routing to a selected cloud provider through an injected `ICloudRewriteClient`.
- `GeminiRewriteProvider` (enabled) and `DeepSeekRewriteProvider` (disabled, never invoked in the active path).
- `RewriteOrchestrator` selecting Local by default and Cloud only under the full gate, with exact-local fallback on timeout, error, invalid response, or disabled/unknown provider.
- A Cloud output validator composing the accepted `RewriteOutputValidator` with additional checks: empty, length bounds, conversational-response rejection, and preservation of numbers, dates, URLs, paths, versions, commands, and protected terms.
- A `Fluent.Cloud` Desktop transport project that calls only the Fluent backend endpoint, never a provider directly, and holds no keys.
- A minimal `Fluent.Backend` ASP.NET project: one authenticated, rate-limited, validated rewrite endpoint; a provider dispatcher; a server-side Gemini provider reading model and key from server environment only; a disabled server-side DeepSeek provider.
- A rewrite context carrying `IsAuthenticated`, `CloudRewriteEnabled`, `CloudConsentGranted`, and `Provider`, plus a default not-authenticated authentication-state seam (auth system is not rewritten).
- First-Cloud-use explicit consent gate and minimal truthful UI states (Local mode, Cloud enabled, Gemini active, Local mode used, Cloud service unavailable). No DeepSeek selector.
- Observability recording provider, response time, fallback used, and fallback cause only; no user content.
- Focused unit/integration tests, Release build, complete suite, reviews, and evidence.

## Excluded

- Ollama, llama.cpp, local model download, local models, and streaming.
- Any provider API key, encrypted key, embedded secret, or distributed secret configuration in the Desktop or repository.
- Direct Desktop-to-provider calls; the Desktop talks only to the Fluent backend.
- Enabling, wiring a UI for, or calling DeepSeek in this phase.
- A full SaaS platform, account management UI, synchronization, packaging, History, or advanced Settings.
- Rewriting the existing authentication system.
- Sending any text to the Cloud when the user is merely authenticated but has not enabled and consented.
- Live provider calls during automated verification.

## Behavior Contract

- Default is Local. Not authenticated, or Cloud disabled, or consent not granted, or provider disabled/unknown all resolve to Local.
- Cloud is used only when authenticated AND Cloud enabled AND consent granted AND provider is an enabled cloud provider (Gemini).
- The first Cloud use requires explicit consent disclosing that transcribed text will be sent to the Cloud for rewriting; audio always stays local.
- Any Cloud timeout, transport error, invalid or conversational response, or empty output returns the exact local text.
- DeepSeek is never called in this phase.
- No user text, audio, transcript, selection, or secret is persisted or logged. Only operational telemetry (provider, duration, fallback, cause) is recorded.
- The Desktop never learns the exact Gemini model.

## Planned Tasks

1. Add domain provider contracts and capabilities.
2. Implement `LocalRewriteProvider` preserving current local output.
3. Implement `ICloudRewriteClient`, `CloudRewriteProvider`, `GeminiRewriteProvider`, and disabled `DeepSeekRewriteProvider`.
4. Implement `RewriteOrchestrator` gating and exact-local fallback.
5. Implement the Cloud output validator additions.
6. Add the `Fluent.Cloud` transport project (backend HTTP client, no keys).
7. Add the minimal `Fluent.Backend` project: endpoint, auth, rate limiter, validation, dispatcher, server Gemini provider, disabled server DeepSeek provider.
8. Add the rewrite context, authentication-state seam, consent gate, and minimal UI states.
9. Add observability without user content.
10. Add focused tests; run Release build, complete suite, reviews, and final Judge; update docs and evidence.

## Acceptance

- Release build PASS; all existing tests PASS; new tests PASS.
- Local mode is unchanged and remains the default.
- The Desktop and repository contain no provider API key or embedded secret (enforced by an automated scan test).
- Gemini is fully wired through the backend transport; the Desktop never calls Gemini directly and never knows the model.
- DeepSeek V4 Pro is architecturally preconfigured but disabled and never called.
- Fallback to the exact local text is exact on every timeout, error, invalid, conversational, or empty Cloud result.
- Cloud validation preserves numbers, dates, URLs, paths, versions, commands, and protected terms and rejects empty or conversational output.
- First Cloud use requires explicit consent; authentication alone never sends text.
- Observability logs no user content.
- Code review PASS, security review PASS, test review PASS, Development Judge ALLOW.

## Deliverables

- Update `project-state.md`, `roadmap.md`, ADR-0005, `component-boundaries.md`.
- Create `docs/project/evidence/phase-06b-cloud/`.
- Final status: `IMPLEMENTED_AWAITING_USER_REVIEW`. Do not auto-start the next phase.
- Open a separate contract for future DeepSeek V4 Pro activation.

## Rollback

- Remove the orchestrator Cloud path, `Fluent.Cloud`, `Fluent.Backend`, provider contracts, and UI Cloud states.
- Restore the direct `SafeProfileRewriteService` call in the dictation pipeline.
- Rebuild and rerun the complete suite; local behavior is unaffected.
