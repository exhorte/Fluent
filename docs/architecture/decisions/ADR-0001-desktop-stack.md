# ADR-0001: Desktop Stack

Status: Accepted

Date: 2026-07-12

## Context

Fluent targets a local Windows desktop MVP with safe focus handling, a floating UI, local audio capture, local speech-to-text, and future packaging.

## Decision

Use .NET 10, C#, WPF, CommunityToolkit.Mvvm, NAudio, Whisper.net with whisper.cpp, Ollama behind a replaceable abstraction, SQLite, Microsoft.Data.Sqlite, Dapper, Serilog, xUnit, and encapsulated Win32/UI Automation adapters.

## Consequences

Positive: strong Windows integration, mature C# tooling, local-first deployment path, testable boundaries.

Negative: Windows-first implementation, WPF-specific UI work, native interop care required.

## Reversibility

Changing stack requires a new ADR and demonstrated technical constraint.
