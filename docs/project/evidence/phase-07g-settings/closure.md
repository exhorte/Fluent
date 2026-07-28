# Phase 07G — Paramètres locaux · Clôture — 2026-07-23

Gouvernance : ADR-0007. Clôturée par le PROJECT_DIRECTOR selon CG-001..CG-008. Notification, sans phrase rituelle.

## Critères

- CG-001 Critères : store de préférences local versionné, profil préféré persisté/restauré/sauvegardé, page Paramètres fonctionnelle, aucun secret ni consentement Cloud persistant. ✔
- CG-002 Build Release **0/0**. ✔
- CG-003 Tests : suite complète **376/376**. ✔
- CG-004 Preuves : `slice-1-preferred-profile.md`, `slice-2-settings-page.md`, ce document. ✔
- CG-005 Risques : préférence réversible et locale ; base dédiée isolée. ✔
- CG-006 Aucun blocage R3. ✔
- CG-007 Garde de complétion déterministe : contrat actif, aucune anomalie critique, `verification.testsPassed = true`. ✔
- CG-008 Smoke visuel : **PASS rapporté par l'utilisateur** le 2026-07-23 (« 07G OK ») — page Paramètres, changement de profil par défaut cohérent avec Profils, persistance au redémarrage, bouton « Gérer l'historique ». Intégré aux preuves sans seconde confirmation.

## Décision

Phase **07G clôturée**. Le PROJECT_DIRECTOR démarre **07H — UX et résilience** (accessibilité, navigation clavier, offline, erreurs et recovery). Premier slice : accessibilité de la navigation et des pages (AutomationProperties, opérabilité clavier).
