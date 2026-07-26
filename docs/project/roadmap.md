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
| 07B | Clôturée | Authentification native Supabase (Google OAuth PKCE). |
| 07E | Clôturée | Dashboard dynamique local. |
| 07F | Clôturée | Historique local (opt-in OFF par défaut). |
| 07G | Clôturée | Paramètres locaux + profil préféré persistant. |
| 07H | Clôturée | UX & résilience (accessibilité, erreurs, recovery). |
| 08B | Clôturée | Dictionnaire/glossaire étendu (import/export JSON). |

## Livrable UI + i18n (2026-07-23/26)

- **UI monochrome** : thème noir & blanc, en-tête épuré, puce avatar, navigation « Home ».
- **Page Subscription** : plan Local·Gratuit, CTA « Passer à Pro » (sans paiement).
- **i18n English/Français** : défaut anglais, persisté, increment 1 (texte statique) + increment 2 partiel (Dashboard + Profils). Increment 3 (chaînes code) restant.
- Build 0/0, suite **401/401**.

## Phases en attente

- **06B** (Gemini) : `IMPLEMENTED_AWAITING_USER_REVIEW` — hors ligne, backend non déployé.
- **06C** (DeepSeek) : `IMPLEMENTED_AWAITING_USER_REVIEW` — préparé hors ligne, aucune activation live.
- **07C** (renommage) : **RÉSOLU** (aucun résidu NyxVoice dans les sources suivies).
- **07D** (backend Cloud) : `IMPLEMENTED_OFFLINE_SLICE_USER_ACCEPTED` — activation live = infra + secret + autorisation distincte.
- **08A** (contexte de réécriture) : **DIFFÉRÉE** (décision utilisateur E-001).

## Prochaines phases

1. **i18n increment 3** — chaînes générées en code : statuts auth/cloud Profils, messages runtime dictée, états vides, DictationErrorPresenter.
2. **08C** — apprentissage opt-in (décision produit E-001 en attente).
3. **09A–09C** — registre fournisseurs, activation DeepSeek live, routage explicable.
4. **10** — performance mesurée.
5. **11** — packaging (dotnet publish, artifact signé).
6. **12** — readiness finale v1.

Le détail des dépendances et risques est dans `docs/project/evidence/phase-07-planning-audit/dependency-roadmap.md`.
