# Phase 06C - DeepSeek V4 Pro Activation (Future)

Status: IMPLEMENTED — awaiting user review; live provider work remains excluded

This phase can begin only after the Development Judge approves its execution contract. The user authorized the bounded preparation on 2026-07-21; external provider configuration, any live request, deployment and publication remain excluded.

## Objective

Enable the already-prepared DeepSeek V4 Pro provider as a selectable cloud rewrite provider, without changing the local default, the consent gate, or the exact-local fallback semantics established in Phase 06B.

## Prerequisites

- Phase 06B reviewed and accepted by the user.
- An authentication system exists, so `IsAuthenticated` can be true.
- A DeepSeek API key is available **on the backend only**, supplied out of band.
- Explicit user authorization to enable a second cloud provider, including any cost implications.

## Included (when authorized)

- Flip `DeepSeekRewriteProvider.Capabilities.IsEnabled` to true and `DeepSeekServerProvider.IsEnabled` to true.
- Implement the live server-side DeepSeek transport behind an `IDeepSeekApi` seam, mirroring `IGeminiApi`, reading model and key from server configuration only.
- Add a minimal provider selector to the Profils page (Gemini / DeepSeek) with truthful availability states.
- Extend focused tests: routing to DeepSeek, exact-local fallback on DeepSeek failure, and validation parity with Gemini.

## Excluded

- Any DeepSeek key, model name, or endpoint in the Desktop or repository.
- Automatic fallback from Gemini to DeepSeek or vice versa; provider choice stays explicit.
- Changing the local default, the consent gate, or the validation and fallback semantics.
- Deployment, packaging, or publication.

## Acceptance (when executed)

- Local remains the default; the consent gate and exact-local fallback are unchanged.
- DeepSeek is reachable only under the same full gate as Gemini and only when explicitly selected.
- No Desktop or repository secret; the automated secret scan still passes.
- Release build clean, complete suite green, reviews PASS, Development Judge ALLOW, and explicit user acceptance.

## Rollback

- Set both DeepSeek capabilities back to disabled and remove the selector; the domain requires no other change, which is the point of the Phase 06B abstraction.
