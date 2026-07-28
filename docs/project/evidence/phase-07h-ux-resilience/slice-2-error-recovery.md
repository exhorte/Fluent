# Phase 07H — UX & résilience · Slice 2 (erreurs & récupération) — 2026-07-23

Gouvernance : ADR-0007. Tâche `FV-P07H-T021`, tier R1. Décidé et exécuté par le PROJECT_DIRECTOR.

## Livré

- **Composant Core pur** (`src/Fluent.Core/Diagnostics/`) : `DictationFailureStage` (Microphone, Transcription, Rewriting, Insertion, Unknown), `UserFacingMessage` (message + piste de récupération), `DictationErrorPresenter.Describe(stage)` — messages français sûrs, non techniques, avec récupération. Aucun détail d'exception, aucune fuite technique.
- **Câblage `MainWindow`** : suivi de l'étape de dictée (`_dictationStage`) mis à jour à chaque phase (micro → transcription → réécriture → insertion). Le catch fatal de `HandleHotKey` remplace l'ancien dump brut `"Dictation failed: {ex.Message}"` par le message sûr correspondant à l'étape ; le détail technique va uniquement dans le journal de debug, jamais dans l'UI.

## Valeur

- **UX** : l'utilisateur reçoit un message clair et une action de récupération concrète (ex. « collez-le avec Ctrl+V », « laissez le modèle finir de se préparer »).
- **Sécurité/confidentialité** : plus d'exposition de messages d'exception bruts dans l'interface (aligné sur la règle « logs minimaux et désensibilisés »).

## Vérification réelle

- `dotnet build Fluent.sln -c Release` : **0 avertissement, 0 erreur**.
- Suite complète : **391/391** (0 échec). Core.Tests 20 → 24 (+4).
- Tests (`tests/Fluent.Core.Tests/Diagnostics/DictationErrorPresenterTests.cs`) : chaque étape a un message + une récupération non vides ; **exhaustif** sur l'enum ; **aucune fuite** de jetons techniques/sensibles (Exception, System., 0x, HRESULT, null, Sqlite, Http, Token…) ; fallback Unknown ; la récupération d'insertion mentionne le presse-papiers (Ctrl+V).

## Bilan 07H

Les quatre thèmes de 07H sont couverts : **accessibilité** (slice 1), **navigation clavier** (slice 1, TabIndex + Buttons), **erreurs & récupération** (slice 2), **offline** déjà intrinsèque (architecture local-first, statut Offline de l'authentification, repli local exact). Reste, avant clôture 07H, le smoke visuel des points UX (E-011). Un polish offline dédié est optionnel et de faible valeur ajoutée.
