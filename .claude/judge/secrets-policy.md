# Secrets Policy: USE_BUT_NEVER_DISCLOSE

Authority: ADR-0007. Preserved safety asset: only the user may change this policy.

Under the risk-based model, a sensitive file no longer triggers ASK_USER automatically. Authorized agents may use secrets; they must never disclose them.

## Permitted use

The PROJECT_DIRECTOR and authorized agents may:

- Detect `.env` files and read the variables they need.
- Use API keys and tokens already provided.
- Configure services and set environment variables.
- Create a `.env.local` from a committed example.
- Use existing credentials for authorized tests and deployments.

Note: the deterministic hooks still deny *reading the secret files' contents through the Read tool* (`.env`, `*.key`, `*.pem`, ...). Secrets are consumed by the running application and tooling through the environment and a SecretBroker-style abstraction, not by the model reading their plaintext. This keeps use and non-disclosure simultaneously true.

## Non-disclosure (never)

- Never display a full secret value.
- Never copy a secret into a reply.
- Never place a secret in evidence, an exception, or a log.
- Never log an `Authorization` header.
- Never commit a `.env`.
- Never embed a secret in the binary.
- Mask values in reports: show only the variable name, its status, and at most the last four characters when strictly necessary.
- Prefer a SecretBroker or equivalent abstraction.

## Mandatory scan

A secret scan of the staged index remains mandatory before any push or release.

## When ASK_USER is still required

Only when the secret:

- Does not exist and cannot be created with available tools;
- Requires human MFA;
- Requires a payment;
- Requires accepting a legal commitment.
