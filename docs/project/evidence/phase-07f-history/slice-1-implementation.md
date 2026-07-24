# Phase 07F — Historique local · Slice 1 (persistance) — 2026-07-23

Gouvernance : ADR-0007. Tâche `FV-P07F-T019`, tier R1. Décidé et exécuté par le PROJECT_DIRECTOR sans demande utilisateur (travail local réversible).

## Livré (slice 1 : fondation persistance, sans UI)

- **Domaine Core** (`src/Fluent.Core/History/`) : `DictationHistoryEntry`, `DictationHistoryLimits`, `DictationHistoryPreferences` (désactivé par défaut), `DictationHistorySnapshot`, `IDictationHistoryStore`, et `DictationHistoryCapturePolicy` (décision pure opt-in + texte valide, sans I/O).
- **Persistance** (`src/Fluent.Persistence/History/SqliteDictationHistoryStore.cs`) : store SQLite dans une **base dédiée `fluent-history.db`** — séparée de `fluent.db` pour ne pas casser la validation mono-table du dictionnaire. Schéma versionné (v1), requêtes paramétrées, gardes de taille en octets, rétention bornée à l'ajout, suppression unitaire, effacement total, préférence opt-in persistée. Rejet sans mutation sur version non supportée / données corrompues.
- **Chemin** : `FluentDataPath.GetDefaultHistoryDatabasePath()` / `GetHistoryDatabasePath(root)`.

## Décisions privacy (dans la vision, scope MVP « Optional history, disabled or erasable by user choice »)

- Opt-in **OFF par défaut** — rien n'est enregistré tant que l'utilisateur n'active pas.
- **Aucun audio** stocké (P-003) ; **aucune donnée externe / télémétrie** (P-004) ; aucun secret.
- Suppression unitaire et effacement total disponibles (règle persistance « Support history deletion »).
- Rétention bornée (500 entrées, les plus récentes conservées).

## Vérification réelle

- `dotnet build Fluent.sln -c Release` : **0 avertissement, 0 erreur** (18 projets).
- Tests ciblés : **Fluent.Core.Tests 20/20**, **Fluent.Persistence.Tests 31/31**.
- Suite complète : **367/367** (0 échec) — 20 + 6 + 6 + 9 + 31 + 164 + 77 + 54. Aucune régression (346 → 367).
- Tests notables : opt-in OFF par défaut + bascule persistée ; roundtrip newest-first ; rétention prune au cap ; suppression unitaire + effacement ; injection SQL neutralisée (schéma/version intacts) ; texte surdimensionné rejeté ; préférence invalide rejetée ; version de schéma 2 rejetée sans mutation (octets inchangés, pas de sidecar) ; annulation sans I/O ; politique de capture Core (disabled/empty/too-long/recorded, profil normalisé).

## Reste à faire (slice 2)

Capture dans le flux de dictée (`MainWindow`) derrière la politique opt-in, et page **Historique** WPF (liste, suppression unitaire, effacement, bascule opt-in) remplaçant l'état « À venir ». Le smoke visuel de la page restera une vérification humaine (E-011).
