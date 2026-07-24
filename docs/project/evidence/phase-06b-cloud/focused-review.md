# Phase 06B Focused Reviews

Task: FV-P06-T002
Date: 2026-07-19
Repair cycles used: 2 / 2

## Review 1 - Security and privacy: PASS (with repairs applied)

Verdict PASS. All ten contract checkpoints were verified: no literal provider secret anywhere in the Desktop or
repository; the Desktop calls only the Fluent backend and never learns the model; the four-part consent gate is
AND-combined with no path enabling Cloud on authentication alone; DeepSeek is unreachable on both tiers; fail-closed
fallback covers timeout, transport, empty, invalid, and conversational results; `RewriteTelemetry` has no string field
and the only wired sink is the no-op; backend auth fails closed and validation bounds text length; password-target,
changed-target, clipboard, no-Enter, shutdown, and in-memory-audio protections are untouched; consent and enablement
are session-only.

Findings raised and **repaired**:

1. **MAJOR - provider key could leak into HTTP request-URI logging.** `HttpGeminiApi` sent the key as a `?key=` query
   parameter while `AddHttpClient` wires default logging handlers that record the outbound URI.
   *Repair:* the key now travels in the `x-goog-api-key` header and the typed client is registered with
   `.RemoveAllLoggers()`, so no outbound request (credential or user text) reaches server logs.
2. **MAJOR - prompt injection not structurally bounded.** Dictated text was concatenated directly after the
   instructions. *Repair:* `RewritePromptBuilder` fences the text between `#####` markers, strips injected markers so
   the fence cannot be escaped, and states the fenced content is data and never an instruction. Three tests added.
3. **MINOR - global, non-partitioned rate limiter.** *Repair:* the limiter is partitioned per caller, keyed on a
   SHA-256 hash of the bearer token when present, otherwise the remote address.

## Review 2 - Architecture and correctness: FAIL on first pass, repaired, re-verified

The first pass returned **FAIL** on a genuine blocking defect. All findings below were repaired and re-verified.

1. **BLOCKING - validator exception could destroy the dictation.** `_cloudValidator.Validate(...)` sat outside the
   orchestrator's `try/catch`, so a `RegexMatchTimeoutException` on a hostile candidate would propagate past the
   already-computed safe local text and surface as "Dictation failed", inserting nothing. This broke the class's core
   invariant. *Repair:* the provider call and the validation now share one guard; any exception maps to
   `Fallback(...)` with the exact local text. A dedicated test injects a throwing validator and asserts the local
   fallback.
2. **MAJOR - validator drift caused a real P-011 gap.** `CloudRewriteValidator` lacked the scheme/namespace
   alternative (`mailto:`, `ssh://`, `std::vector`) and the acronym/proper-noun alternative that
   `RewriteOutputValidator` has, so a cloud rewrite could alter those tokens undetected. *Repair:* both alternatives
   were added; five new theory cases cover scheme, UNC path, and proper-noun mutation.
3. **MAJOR - ambiguous Unix-path regex.** The nested `(?:[^\s/]+/)*[^\s,;!?]+` had overlapping classes allowing
   polynomial backtracking. *Repair:* replaced with a single non-overlapping quantifier `/[^\s,;!?]*`; a test asserts a
   400-segment slash run validates in well under two seconds.
4. **MAJOR - backend DI captive dependency.** `GeminiServerProvider` was a singleton capturing the transient typed
   `IGeminiApi` client, pinning one `HttpMessageHandler` for the process lifetime. *Repair:* providers and the
   dispatcher are scoped; only the stateless `BackendAuthenticator` remains a singleton.
5. **MAJOR - Cloud options captured once and never refreshed.** `CloudBackendOptions` was constructed inline with
   `BaseAddress = null`, so a future access token would never reach the transport. *Repair:* options are resolved per
   call through a provider delegate; `MainWindow.BuildCloudBackendOptions` reads the current authentication state.
   The null `BaseAddress` remains deliberate for this phase and is now documented in code and in project state.
6. **MINOR - unguarded outbound Gemini call.** *Repair:* `HttpGeminiApi` catches transport failures and returns null,
   honouring the "provider never throws" contract so the endpoint yields a controlled 503.

A design improvement accompanied the repairs: `ICloudRewriteValidator` was extracted so the orchestrator depends on an
abstraction and validator failure is testable.

## Review 3 - Tests: PASS

The focused suites are meaningful rather than nominal, and caught two genuine defects during this phase:

- A mangled regex escape had produced `(?:[A-Za-z]:\|\\)` (a literal pipe) instead of the Windows-path alternative,
  silently disabling path protection so `C:\Apps\Fluent` could become `C:\Apps\NyxFlow` undetected.
- The blocking validator-exception path above, now covered by an injected throwing validator.

Coverage includes all four gate states, DeepSeek non-invocation on both tiers, every fallback reason, telemetry shape
(reflection-asserted absence of a string field), cancellation, sensitive-token mutation and dropping across
numbers/dates/URLs/Windows and UNC paths/schemes/versions/commands/e-mails/proper nouns, conversational and over-long
rejection, backtracking bounds, prompt fencing, backend dispatch/validation/auth, and a repository-wide secret scan
plus a Desktop provider-endpoint scan.

## Accepted residual items (documented, not repaired in this bounded slice)

- **Residual prompt-injection risk (recorded in R-012).** Fencing plus the cloud validator plus the exact-local
  fallback bound but do not eliminate a semantically altered yet token-preserving response.
- **No `WebApplicationFactory` end-to-end test** of the assembled `/v1/rewrite` pipeline; components are unit-tested in
  isolation. Adding one would require a new test package, which the contract excludes.
- **Timing side channel on token length** in `BackendAuthenticator.FixedTimeEquals` (early return on length mismatch).
- **No global exception handler** in the backend; it is not deployed in this phase and must be hardened before any
  deployment.

## Verification after repairs

- Release solution build: 0 warnings, 0 errors.
- Focused tests: Rewrite `~Cloud` 39/39, Integration `~Cloud` 4/4, Backend 29/29.
- Complete suite: **256 / 256**.
