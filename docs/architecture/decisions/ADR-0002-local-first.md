# ADR-0002: Local-First

Status: Accepted

Date: 2026-07-12

## Context

The product handles voice, dictated text, clipboard content, and potentially sensitive target applications.

## Decision

Audio, transcription, rewriting, dictionary, dashboard, and persistence are local-first by default. No cloud service, telemetry, subscription, authentication, or backend is mandatory for the MVP.

## Consequences

Positive: privacy alignment, offline capability, simpler non-commercial MVP.

Negative: model distribution, local performance, and hardware variability become product constraints.

## Reversibility

Optional cloud integrations require explicit consent, future product scope, privacy documentation, and user approval.
