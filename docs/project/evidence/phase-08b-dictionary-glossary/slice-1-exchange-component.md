# Phase 08B — Dictionnaire/glossaire étendu · Slice 1 (import/export + conflits) — 2026-07-23

Gouvernance : ADR-0007. Tâche `FV-P08B-T022`, tier R1. Décidé et exécuté par le PROJECT_DIRECTOR (08A différée sur décision utilisateur E-001).

## Livré — composant pur `Fluent.Rewrite.Dictionary`

- `DictionaryConflictPolicy` (SkipExisting / OverwriteExisting) — les conflits sont toujours résolus **explicitement**.
- `DictionaryImportPlan` (+ `DictionaryImportItem`, `DictionaryImportItemStatus`) : plan d'import avec entrées à appliquer et audit par ligne (ajouté, mis à jour, conflit ignoré, invalide rejeté, doublon de fichier, capacité dépassée) + compteurs.
- `DictionaryExchange` :
  - `Export(entries)` → JSON déterministe (trié par forme prononcée, indenté).
  - `Plan(content, existing, policy)` → parse le JSON, **valide chaque entrée via `PersonalDictionaryValidation` existante**, résout les conflits selon la politique, borne à la capacité max du dictionnaire, gère les doublons de fichier, et signale un JSON invalide sans lever d'exception.
- **Sécurité** : le contenu importé est traité comme **donnée** — jamais exécuté, jamais de contournement de validation. Aucune I/O dans le composant (pur, testable) ; la lecture/écriture de fichier et l'application viendront au slice 2.

## Vérification réelle

- `dotnet build Fluent.sln -c Release` : **0 avertissement, 0 erreur**.
- Suite complète : **399/399** (0 échec). Rewrite.Tests 164 → 172 (+8).
- Tests (`tests/Fluent.Rewrite.Tests/DictionaryExchangeTests.cs`) : round-trip export→import sur dictionnaire vide ; politique ignorer (conflit conservé) ; politique écraser (mise à jour) ; entrées invalides rejetées par la validation existante ; doublon de fichier (première occurrence conservée) ; import borné à la capacité max ; JSON invalide → plan non parsé ; contenu hostile préservé comme donnée (jamais exécuté).

## Reste (slice 2)

Câbler l'application du plan dans `PersistentPersonalDictionary` (upsert en lot) et ajouter les boutons **Importer / Exporter** dans la page Dictionnaire (choix de fichier, sélection de politique de conflits, résumé du résultat). Smoke visuel = vérification humaine E-011.
