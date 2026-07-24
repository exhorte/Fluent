# Phase 06B Implementation Summary - Optional Cloud Rewrite Engine

Task: FV-P06-T002
Date: 2026-07-19
Plan Judge: ALLOW (2026-07-19) with five follow-up conditions.

## Architecture delivered

```
RewriteOrchestrator
        |
  IRewriteProvider
        |
  +-----+------------------+
  |                        |
LocalRewriteProvider   CloudRewriteProvider
                            |
                    +-------+--------+
                    |                |
            GeminiRewriteProvider  DeepSeekRewriteProvider
                (enabled)             (disabled, V4 Pro)
```

- **Domain contracts** (`src/Fluent.Rewrite/Providers/`): `IRewriteProvider`, `RewriteProviderId`,
  `RewriteProviderCapabilities`, `ProviderRewriteRequest`, `ProviderRewriteResult`, `RewriteStatus`,
  `RewriteFailureReason`, `RewriteValidationResult`. The domain depends on neither Gemini nor DeepSeek.
- **Local mode unchanged**: `LocalRewriteProvider` wraps the accepted `SafeProfileRewriteService`. The orchestrator
  always computes the local result first, so the exact local text is available as the fallback for every failure path.
- **Cloud transport**: `ICloudRewriteClient` is the seam; `Fluent.Cloud.BackendCloudRewriteClient` calls only the
  Fluent backend (`POST {base}/v1/rewrite`). It holds no provider key and never learns the model.
- **Orchestrator gate**: Cloud is used only when `IsAuthenticated && CloudRewriteEnabled && CloudConsentGranted &&
  provider is an enabled cloud provider`. Every other state resolves to Local. DeepSeek is unreachable because
  `CloudRewriteProvider.TryResolveEnabled` refuses disabled providers.
- **Fail-closed validation**: `CloudRewriteValidator` permits genuine rephrasing but rejects empty, over-long, and
  conversational responses and requires exact multiset preservation of sensitive tokens (numbers, dates, URLs, Windows
  and Unix paths, versions, backtick commands, e-mails, dotted identifiers). Any rejection returns the exact local text.
- **Backend** (`src/Fluent.Backend/`): one authenticated, rate-limited (fixed window 30/min), validated
  `POST /v1/rewrite`; `RewriteProviderDispatcher` resolves only enabled providers; `GeminiServerProvider` reads
  `GEMINI_MODEL` and `GEMINI_API_KEY` from server configuration only and reports `gemini_not_configured` instead of
  throwing when absent; `DeepSeekServerProvider` is disabled; `BackendAuthenticator` fails closed when no token is
  configured and uses a fixed-time comparison.
- **Authentication seam**: `IAuthenticationState` / `DefaultAuthenticationState` (unauthenticated). The existing auth
  system is not rewritten; because no auth exists yet, the Cloud path is unreachable in live use.
- **Consent and UI**: `CloudRewriteSettings` is session-only. Enabling Cloud requires prior explicit consent disclosing
  that transcribed text will be sent to the Cloud and that audio always stays local. The Profils page shows truthful
  states (`MODE LOCAL`, `CLOUD - GEMINI ACTIF`, connection-required) and there is no DeepSeek selector. The header shows
  `Profil - <name> - Local|Cloud - Gemini`.
- **Observability**: `RewriteTelemetry` records only provider, duration, fallback used, and fallback cause. It has no
  string field, so no user content can be carried. The default sink is a no-op; nothing is persisted Desktop-side.

## Plan-judge conditions

1. **Observability stays operational** - `RewriteTelemetry` has no free-text field (asserted by a reflection test), the
   default sink is `NullRewriteObservabilitySink`, there is no third-party analytics sink and no Desktop persistence.
2. **New dependencies** - none added by this phase. `Directory.Packages.props` was not modified by Phase 06B and the two
   new source projects declare zero `PackageReference` entries; only shared-framework APIs are used (`System.Net.Http`,
   `System.Net.Http.Json`, `Microsoft.AspNetCore.App`). The file's three entries that differ from git HEAD
   (`Dapper`, `Microsoft.Data.Sqlite.Core`, `SQLitePCLRaw.bundle_e_sqlite3`) come from the accepted Phase 05B slice.
3. **Consent/enable state** - explicitly **session-only**, documented in `CloudRewriteSettings`; every launch starts
   disabled with consent not granted.
4. **ADR-0005** - promoted from Proposed to Accepted with a dated changelog referencing the plan verdict.
5. **Closure** - not claimed. Status is `IMPLEMENTED_AWAITING_USER_REVIEW`; no user validation is invented.

## Verification

- Focused tests: Rewrite `~Cloud` 39/39, Integration `~Cloud` 4/4, Backend 29/29 (all filters non-zero), after two bounded repair cycles.
- Release solution build: 0 warnings, 0 errors.
- Complete suite: **256/256** (184 pre-existing + 72 new), so local mode is provably unchanged.
- A focused test caught a real defect (Windows-path protection silently disabled by a mangled regex escape); it was
  fixed and the case is retained as a regression guard.

## Explicit limitations

- The backend is **not deployed** and makes **no live Gemini call** during verification; a live round-trip requires a
  server-side key supplied out of band. Automated coverage uses a fake `IGeminiApi`.
- Because no authentication system exists, the Cloud path cannot currently be exercised end to end in the running app;
  it stays local by construction.
