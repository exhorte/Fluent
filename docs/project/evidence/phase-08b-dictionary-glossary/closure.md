# Phase 08B — Dictionnaire/glossaire étendu · Clôture — 2026-07-23

Gouvernance : ADR-0007. Clôturée par le PROJECT_DIRECTOR selon CG-001..CG-008. Notification, sans phrase rituelle.

## Critères

- CG-001 Critères : import/export sûr, résolution de conflits explicite, validation réutilisée, bornage capacité, suppression déjà présente. ✔
- CG-002 Build Release **0/0**. ✔
- CG-003 Tests : suite complète **399/399** (Rewrite.Tests 164 → 172, +8 tests d'échange). ✔
- CG-004 Preuves : `slice-1-exchange-component.md`, `slice-2-wiring-ui.md`, ce document. ✔
- CG-005 Risques : contenu importé traité comme donnée (jamais exécuté), validé, borné ; fichiers locaux uniquement. ✔
- CG-006 Aucun blocage R3. ✔
- CG-007 Garde de complétion déterministe : contrat actif, aucune anomalie critique, `verification.testsPassed = true`. ✔
- CG-008 Smoke visuel : **PASS rapporté par l'utilisateur** le 2026-07-23 (« 08B OK ») — export, import ignorer/écraser, fichier invalide géré. Intégré aux preuves.

## Décision

Phase **08B clôturée**. La suite roadmap est **08C — apprentissage opt-in** (garde-fou : opt-in, explicabilité, jamais d'apprentissage silencieux sur les dictées). Comme 08C touche le comportement d'apprentissage sur des données utilisateur, une décision produit fondamentale (E-001) est soumise à l'utilisateur avant implémentation.
