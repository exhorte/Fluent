# Project State

Status: `GOVERNANCE_ADR_0007_ADOPTED` · `PHASES 01–08B CLÔTURÉES` · `UI MONOCHROME + SUBSCRIPTION + i18n LIVRÉ` · Branche `chore/fluent-v1-consolidation`, PR #1 ouverte vers `main`

Date: 2026-07-26

## État vérifié (2026-07-26)

**Build Release** : 0 avertissement, 0 erreur.
**Suite complète** : **401/401** tests réussis.
**Changement de langue** (English/Français) : fonctionnel, vérifié manuellement.

## Dernier livrable — UI monochrome + Abonnement + i18n (2026-07-23/26)

Sur la branche `chore/fluent-v1-consolidation` (commit `ed1891b`, postérieur à la PR #1) :

- **Thème noir & blanc** : accents cyan/bleu → blanc/gris ; logo inversé ; navigation sans bordure fixe (outline blanc-transparent au survol, outline discret sur l'élément actif).
- **En-tête épuré** : logo seul ; infos profil/moteur déplacées vers une carte « Session et moteur » dans Paramètres.
- **Profil utilisateur en bas de la barre** : puce avatar (2 lettres) + prénom + initiale, cliquable vers Profils ; lien « Upgrade plan » vers Abonnement.
- **Page & navigation « Subscription »** : plan Local·Gratuit, compte, avantages Pro, CTA « Passer à Pro » (sans paiement).
- **« Profils » retiré de la nav** : accessible via la puce utilisateur.
- **« Vue d'ensemble » → « Home »**.
- **Internationalisation** : réglage Langage dans Paramètres (English/Français, défaut anglais), persisté via store de paramètres schéma v2 (colonne `language`, migration v1→v2). Mécanisme `Localizer` avec liaison `{Binding [clé], Source={StaticResource Loc}}`. **Increment 1** : tout le texte statique des pages (Home/History/Dictionary/Subscription/Settings + nav) traduit en/fr. **Increment 2 (partiel)** : `DashboardStatusPresenter` bilingue + texte statique page Profils.

### Reste à traduire (i18n increment 3)

Chaînes générées en code :
- Statuts auth/cloud de la page Profils
- Messages runtime de dictée (`MainWindow`)
- États vides et statuts Dictionnaire/Historique/Paramètres
- `DictationErrorPresenter` (Core)
- Noms d'accessibilité de la nav (laissés statiques FR pour ne pas casser les tests markup)

## Phases clôturées

| Phase | Statut | Réalité |
| --- | --- | --- |
| 00 | Livrée | Harness, règles, preuves et tests de garde. |
| 01 | Implémentée | Spike Windows ; vérification multi-applications préparée. |
| 02 | Acceptée | Dictée française locale, Whisper, protections d'insertion. |
| 03 | Acceptée | Capsule et Overview honnête. |
| 04A | Livrée | Réécriture française déterministe sûre. |
| 05A/05B | Acceptées | Dictionnaire session puis SQLite local persistant. |
| 06A | Clôturée | Profils locaux session-only. |
| 07B | Clôturée | Authentification native Supabase (Google OAuth PKCE). |
| 07C | Résolue | Renommage technique NyxVoice→Fluent (aucun résidu dans les sources). |
| 07E | Clôturée | Dashboard dynamique local. |
| 07F | Clôturée | Historique local (opt-in OFF par défaut). |
| 07G | Clôturée | Paramètres locaux + profil préféré persistant. |
| 07H | Clôturée | UX & résilience (accessibilité, erreurs, recovery). |
| 08B | Clôturée | Dictionnaire/glossaire étendu (import/export JSON). |

## Phases en attente

- **06B** (Gemini) : `IMPLEMENTED_AWAITING_USER_REVIEW` — hors ligne, backend non déployé.
- **06C** (DeepSeek) : `IMPLEMENTED_AWAITING_USER_REVIEW` — préparé hors ligne, aucune activation live.
- **07D** (backend Cloud) : `IMPLEMENTED_OFFLINE_SLICE_USER_ACCEPTED` — origine backend HTTPS + gate UI validés ; activation live = infra + secret + autorisation distincte.
- **08A** (contexte de réécriture) : **DIFFÉRÉE** (décision utilisateur E-001 ; consommateur = Cloud non live).

## Prochaines phases

1. **i18n increment 3** — traduire les chaînes générées en code restantes (cf. ci-dessus).
2. **08C** — apprentissage opt-in (décision produit E-001 en attente).
3. **09A–09C** — registre fournisseurs, activation DeepSeek live, routage explicable.
4. **10–12** — performance mesurée, packaging, readiness finale v1.

## Santé du dépôt

- Branche de travail : `chore/fluent-v1-consolidation`.
- Branche `main` intacte, en phase avec `origin/main`.
- PR #1 brouillon ouverte vers `main`.
- Commit le plus récent : `ed1891b feat(ui): monochrome redesign, Subscription page, and i18n (English default)`.
- 3 fichiers non suivis : `docs/templates/fluent.png`, `docs/templates/fluentx.png`, `docs/templates/model.png`.
- Aucun secret, aucun `.env`, aucune donnée sensible.
- Licence : All rights reserved.
