# Phase 08B — Dictionnaire/glossaire étendu · Slice 2 (câblage + UI) — 2026-07-23

Gouvernance : ADR-0007. Tâche `FV-P08B-T022`, tier R1. Décidé et exécuté par le PROJECT_DIRECTOR.

## Livré

- **`PersistentPersonalDictionary.ApplyImportAsync(entries, ct)`** : applique les entrées d'un plan d'import (ajouts + mises à jour) via le **chemin d'upsert existant, validé, sérialisé (gate) et persisté** ; retourne le nombre appliqué. Aucune logique de validation dupliquée.
- **Page Dictionnaire** ([DictionaryPage.xaml](../../src/Fluent.App/Views/DictionaryPage.xaml)) : carte **Importer / Exporter** —
  - **Exporter…** : `SaveFileDialog` (.json) → écrit `DictionaryExchange.Export(snapshot)` localement.
  - **Importer…** : `OpenFileDialog` → lit le fichier → `DictionaryExchange.Plan(content, snapshot, policy)` → `ApplyImportAsync` → rafraîchit → **résumé** (ajouts, mises à jour, conflits ignorés, rejetés, doublons, au-delà de la capacité).
  - Case **« Écraser les entrées existantes en cas de conflit »** (décochée par défaut = comportement sûr `SkipExisting`).
  - Panneau désactivé pendant le chargement/une mutation.
- **Sécurité/UX** : fichiers **locaux** uniquement, aucune donnée externe ; contenu importé validé et jamais exécuté ; erreurs fichier affichées en messages génériques non techniques (aligné 07H).

## Vérification réelle

- `dotnet build Fluent.sln -c Release` : **0 avertissement, 0 erreur** (WPF inclus).
- Suite complète : **399/399** (0 échec). La logique métier (parse, validation, conflits, bornage) est couverte par les 8 tests unitaires de slice 1 ; `ApplyImportAsync` réutilise le chemin d'upsert déjà testé.

## Reste (vérification humaine, E-011)

Smoke visuel : exporter le dictionnaire vers un fichier, le réimporter (vérifier « conflits ignorés »), cocher « écraser » et réimporter (vérifier « mises à jour »), importer un fichier invalide (message d'erreur). Non automatisable dans cette session.
