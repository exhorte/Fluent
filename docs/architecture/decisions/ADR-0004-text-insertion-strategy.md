# ADR-0004: Text Insertion Strategy

Status: Proposed

Date: 2026-07-12

## Context

Fluent must insert text into the user-selected target without stealing focus, entering passwords, executing commands, or pasting into a changed target.

## Decision

Use UI Automation to identify and secure the target. Use clipboard plus SendInput for insertion only when the original target is still valid. If the target changed or disappeared, copy to clipboard and show an explicit fallback indication.

## Consequences

Positive: uses Windows-native capabilities and supports broad applications.

Negative: custom controls, elevated apps, and clipboard timing require careful testing.

## Reversibility

Phase 01 must validate this strategy against Notepad, browser, VS Code, and Windows Terminal before acceptance.
