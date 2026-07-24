# ADR-0005: Optional Cloud Rewrite via Fluent Backend

Status: Accepted

Date: 2026-07-19

## Changelog

- 2026-07-19: Proposed alongside the Phase 06B plan.
- 2026-07-19: Accepted after the Development Judge plan verdict ALLOW (`docs/project/evidence/phase-06b-cloud/plan-judge-verdict.json`) and the implementation of FV-P06-T002. No new NuGet dependency was required; consent and Cloud enablement are session-only.

## Context

Phase 06A delivered selectable local rewrite profiles. The originally planned Phase 06B local Ollama semantic engine is abandoned. Instead, the product introduces an optional Cloud rewrite engine that stays disabled unless the user is authenticated AND has explicitly enabled and consented to Cloud rewriting. The default behavior remains fully local (ADR-0002). Cloud must never receive text merely because a user is signed in.

The Desktop must never hold provider API keys. All secrets stay on a Fluent backend that mediates provider calls. Gemini is the default selection. Phase 06C adds DeepSeek V4 Pro as an explicit session-only selection, but its server transport is default-deny until valid backend-only configuration is present. The exact provider model is server-configured and never known to the Desktop.

## Decision

Introduce a provider abstraction in the rewrite domain and a mediating backend:

- Domain contracts (`IRewriteProvider`, `RewriteProviderId`, `RewriteProviderCapabilities`, provider-level request/result/status/failure/validation types) live in `Fluent.Rewrite` and depend on neither Gemini nor DeepSeek.
- `LocalRewriteProvider` adapts the accepted Phase 04A/06A safe local rewrite and preserves current local behavior exactly.
- `CloudRewriteProvider` routes to a selected cloud provider (`GeminiRewriteProvider` by default, `DeepSeekRewriteProvider` only after explicit selection) through an injected transport interface `ICloudRewriteClient`; the Desktop transport is implemented in a new `Fluent.Cloud` project and calls only the Fluent backend, never a provider directly.
- `RewriteOrchestrator` selects Local by default and Cloud only when authenticated + Cloud enabled + consent granted + provider is an enabled cloud provider; any timeout, error, invalid response, or disabled/unknown provider falls back to the exact local text.
- A minimal `Fluent.Backend` ASP.NET project exposes one authenticated, rate-limited, validated rewrite endpoint with a provider dispatcher; each server-side provider reads its model and key only from backend process configuration. DeepSeek accepts only the exact `https://api.deepseek.com` origin and does not issue an outbound request unless its model, key and base URL are all valid.
- Cloud output passes the accepted `RewriteOutputValidator` plus additional Cloud checks (empty, length bounds, conversational-response rejection, numbers/dates/URLs/paths/versions/commands/protected-term preservation); invalid output falls back to the exact local text before insertion.
- Observability records provider used, response time, fallback used, and fallback cause only; no user content, audio, transcript, or secret is ever logged.

## Consequences

Positive: privacy-preserving optional Cloud rewriting, no Desktop secrets, provider-agnostic domain, reversible to pure local, and a DeepSeek transport that remains unavailable by default when server configuration is absent.

Negative: introduces a non-Windows backend deployable and a network trust boundary; live Gemini behavior cannot be end-to-end verified without a server-side key and hosting, so this phase verifies compilation, unit/integration logic, fallback, and validation rather than a live Gemini round-trip.

## Reversibility

Removing the Cloud path restores pure local rewriting with no contract change to the local pipeline. Disabling DeepSeek removes only its session selection and server provider while Local and Gemini retain their behavior.

## Scope Interpretation For This Phase

"Backend prepared / only what is necessary" is implemented as a minimal, compiling, unit-tested backend project with providers behind interfaces and no secrets in the repository. It is not deployed and makes no live provider call during automated verification. The Desktop is fully implemented and tested. Live Gemini use requires a server-side key provided out of band.
