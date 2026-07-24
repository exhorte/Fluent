# Phase 06A - Selectable Local Profiles

Status: IN PROGRESS - PLAN APPROVED BY DEVELOPMENT JUDGE (ALLOW, 2026-07-18)

## Objective

Let the user select a real local transformation profile for each dictation while preserving the accepted dictionary, validation, target-safety, privacy, and insertion behavior.

## Included

- A canonical local profile catalog containing Professional French, Developer, and Simplified French metadata.
- Professional French remains the default and retains the accepted deterministic punctuation behavior.
- Developer becomes a real available profile that returns the exact post-dictionary source without rewriting technical content.
- Simplified French is shown truthfully as unavailable until a safe local semantic engine exists.
- One session-only profile selection state with canonical-ID validation.
- An immutable selected-profile snapshot captured when recording starts and used until that dictation completes.
- Deterministic routing to the correct local rewriter with exact-source fallback on invalid routing or failure.
- A real Profiles dashboard page, navigation, current-profile disclosure, and truthful Overview/header text.
- Focused profile, routing, pipeline, WPF, security, Release, complete-suite, review, and Windows smoke evidence.

## Excluded

- Ollama, model installation, model download, network access, prompts, or semantic generation.
- Pretending Simplified French is operational when it cannot safely simplify vocabulary.
- Persistence of the selected profile, SQLite migration, settings storage, registry, or files.
- History, analytics, telemetry, export, synchronization, cloud, accounts, or secrets.
- Whisper, audio, model quality, hotkey, capsule, Win32, target identity, password, clipboard, insertion policy, or Enter changes.
- New packages, packaging, deployment, publication, commit, or push.

## Behavior Contract

- Every launch starts with Professional French selected.
- Only catalog-owned available profiles can be selected.
- Developer preserves the exact post-dictionary source, including Unicode, line breaks, code, commands, paths, URLs, versions, numbers, and identifiers.
- Simplified French remains visible but disabled with a local-semantic-engine explanation.
- A profile change during an active recording applies only to the following dictation.
- Any unknown, unavailable, failed, empty, or unsafe route returns the exact post-dictionary source through the accepted safe rewrite service.
- No user text, selection, or profile activity is persisted or logged.

## Planned Tasks

1. Extend the canonical profile catalog and add validated session selection.
2. Add deterministic profile routing and exact Developer pass-through.
3. Capture the selected profile at recording start and use it through completion.
4. Add the real Profiles page and navigation with honest availability states.
5. Update Overview/header copy to show the actual selected rewrite profile without confusing it with the Whisper engine profile.
6. Add focused unit and integration tests for catalog, selection, routing, exact preservation, cancellation, snapshots, and pipeline ordering.
7. Run Release build, complete tests, bounded reviews, final Judge, and user Windows smoke.

## Acceptance

- Professional French is selected by default and still produces the accepted safe deterministic output.
- Developer can be selected and produces exact post-dictionary output for ordinary and technical samples.
- Simplified French cannot be selected and is not presented as implemented.
- Unknown or fabricated profile objects cannot select or route arbitrary behavior.
- The profile used by a dictation is the one captured at recording start.
- The UI navigation and all profile labels reflect real current state.
- Dictionary persistence and all target, password, clipboard, no-Enter, shutdown, and audio-privacy protections remain unchanged.
- No package, network, Ollama, database, history, or settings mutation is introduced.
- Focused tests, Release build, complete suite, reviews, final Judge, and user smoke pass before closure.

## Rollback

- Restore the Professional French profile as the single hard-coded selection.
- Remove only the Phase 06A catalog additions, router, session selection, Profiles page, navigation, tests, and evidence.
- Preserve the accepted Phase 05B dictionary database and all earlier baselines.
- Rebuild and rerun the complete suite.
