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

Statut : `BLOQUÉ / À RÉCONCILIER`.

Les chemins physiques Fluent existent, mais l'arbre Git est très sale et le document historique est incohérent. Aucun traitement n'est autorisé dans la phase 07B ni dans l'audit de planification. Cette phase devra être stabilisée avant packaging, publication ou tout commit atomique.

## Prochaines phases

La numérotation conserve 07C pour le renommage technique existant. Les identifiants suivants sont donc décalés d'une lettre.

1. **07D — Réécriture Gemini authentifiée** : la tranche locale de configuration d'origine backend HTTPS et de gate UI est `IMPLEMENTED_OFFLINE_SLICE_USER_ACCEPTED_LIVE_AUTHORIZATION_REQUIRED`, avec verdict final du Judge ALLOW et acceptation utilisateur. Le backend déployé, le secret serveur et un smoke Gemini réel restent hors périmètre et exigent une autorisation externe distincte.
2. **07E — Tableau de bord dynamique** : `TECHNICALLY_READY_FOR_USER_CLOSURE` sous contrat FV-P07-T016 ; métriques locales réelles, états vides/erreurs, aucune donnée inventée. Le smoke visuel est PASS, rapporté par l’utilisateur, et le verdict technique de clôture est ALLOW ; la clôture reste à valider par l’utilisateur.
3. **07F — Historique local fonctionnel** : décision privacy, opt-in, SQLite, recherche, rétention et suppression.
4. **07G — Paramètres fonctionnels** : préférences locales versionnées et réversibles ; aucun secret ni consentement Cloud persistant.
5. **07H — UX et résilience** : accessibilité, navigation clavier, offline, erreurs et recovery.
6. **08A–08C** : contexte de réécriture, dictionnaire/glossaire étendu et apprentissage opt-in.
7. **09A–09C** : registre fournisseurs, activation DeepSeek live sous autorité séparée, routage explicable.
8. **10–12** : performance mesurée, packaging, readiness finale.

Le détail des dépendances et risques est dans `docs/project/evidence/phase-07-planning-audit/dependency-roadmap.md`.
