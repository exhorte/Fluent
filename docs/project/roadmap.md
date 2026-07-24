# Roadmap Fluent

## Socle livré

| Phase | Statut | Réalité |
| --- | --- | --- |
| 00 | Livrée | Harness, règles, preuves et tests de garde. |
| 01 | Implémentée | Spike Windows ; vérification multi-applications à reprendre avant packaging. |
| 02 | Acceptée | Dictée française locale, Whisper, protections d'insertion. |
| 03 | Acceptée | Capsule et Overview honnête. |
| 04A | Livrée | Réécriture française déterministe sûre. |
| 05A / 05B | Acceptées | Dictionnaire session puis SQLite local persistant. |
| 06A | Clôturée | Profils locaux session-only. |
| 06B | `IMPLEMENTED_AWAITING_USER_REVIEW` | Gemini optionnel hors ligne, backend non déployé. |
| 06C | `IMPLEMENTED_AWAITING_USER_REVIEW` | DeepSeek préparé hors ligne, aucune activation live. |
| 07A | Implémentée à réconcilier | Marque publique Fluent présente ; documentation de renommage à fiabiliser. |

## Phase actuelle

### 07B — Authentification native Supabase

Statut : `CLOSED_USER_ACCEPTED_TECHNICALLY_COMPLETE`.

Le smoke OAuth Google principal et les quatre contrôles de session sont réussis d'après les résultats manuels utilisateur. Le verdict final du Judge est ALLOW et l'utilisateur a explicitement clôturé 07B le 2026-07-22. Voir les preuves dans `docs/project/evidence/phase-07b-supabase-auth/`.

### 07C — Renommage technique intégral

Statut : `RÉSOLU 2026-07-23`.

Le renommage NyxVoice→Fluent est complet dans les sources suivies (aucun résidu `NyxVoice` dans `src/`/`tests/`) et l'arbre de travail a été aplati en un commit propre sur `main`, en phase avec `origin/main`. L'ancien blocage « à réconcilier » est caduc. Vérifié dans `docs/project/evidence/consolidation-2026-07-23/readiness-snapshot.md`.

## Prochaines phases

La numérotation conserve 07C pour le renommage technique existant. Les identifiants suivants sont donc décalés d'une lettre.

1. **07D — Réécriture Gemini authentifiée** : la tranche locale de configuration d'origine backend HTTPS et de gate UI est `IMPLEMENTED_OFFLINE_SLICE_USER_ACCEPTED_LIVE_AUTHORIZATION_REQUIRED`, avec verdict final du Judge ALLOW et acceptation utilisateur. Le backend déployé, le secret serveur et un smoke Gemini réel restent hors périmètre et exigent une autorisation externe distincte.
2. **07E — Tableau de bord dynamique** : `TECHNICALLY_READY_FOR_USER_CLOSURE` sous contrat FV-P07-T016 ; métriques locales réelles, états vides/erreurs, aucune donnée inventée. Le smoke visuel est PASS, rapporté par l’utilisateur, et le verdict technique de clôture est ALLOW ; la clôture reste à valider par l’utilisateur.
3. **07F — Historique local fonctionnel** : **CLÔTURÉE 2026-07-23** (ADR-0007). Opt-in OFF par défaut, SQLite `fluent-history.db` dédié, recherche, rétention, suppression/effacement, page Historique WPF. Build Release 0/0, suite 367/367, smoke visuel PASS utilisateur. Preuves `docs/project/evidence/phase-07f-history/`.
4. **07G — Paramètres fonctionnels** : **CLÔTURÉE 2026-07-23** (ADR-0007). Store de paramètres SQLite `fluent-settings.db` dédié, profil préféré persistant, page Paramètres WPF (profil par défaut, résumé historique, stockage local). Build 0/0, suite 376/376, smoke visuel PASS utilisateur. Preuves `docs/project/evidence/phase-07g-settings/`.
5. **07H — UX et résilience** : **CLÔTURÉE 2026-07-23** (ADR-0007). Accessibilité (AutomationProperties + TabIndex), navigation clavier, erreurs & récupération (DictationErrorPresenter) ; offline intrinsèque. Build 0/0, suite 391/391, smoke visuel PASS utilisateur. Preuves `docs/project/evidence/phase-07h-ux-resilience/`.
6. **08A — contexte de réécriture** : **DIFFÉRÉE 2026-07-23** (décision utilisateur E-001 ; consommateur principal = Cloud non live). À reprendre quand le Cloud sera live.
7. **08B — dictionnaire/glossaire étendu** : **CLÔTURÉE 2026-07-23** (ADR-0007). Import/export JSON local sûr, résolution de conflits explicite, validation réutilisée, bornage, suppression. Build 0/0, suite 399/399, smoke visuel PASS utilisateur. Preuves `docs/project/evidence/phase-08b-dictionary-glossary/`. **08C** (apprentissage opt-in) suit — décision produit E-001 en attente.
8. **09A–09C** : registre fournisseurs, activation DeepSeek live sous autorité séparée, routage explicable.
9. **10–12** : performance mesurée, packaging, readiness finale.

Le détail des dépendances et risques est dans `docs/project/evidence/phase-07-planning-audit/dependency-roadmap.md`.
