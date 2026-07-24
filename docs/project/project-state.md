# Project State

Status: `GOVERNANCE_ADR_0007_ADOPTED` · `07E/07F/07G/07H/08B CLOSED` · `08A DIFFÉRÉE` · `CONSOLIDATION VÉRIFIÉE` · dépôt en phase avec `origin/main`

Date: 2026-07-23

## Consolidation vérifiée (2026-07-23)

Sur décision utilisateur (consolider plutôt qu'ouvrir 08C), le PROJECT_DIRECTOR a produit un instantané de readiness. **Santé vérifiée** : build Release 0/0, suite .NET **399/399**, suite gouvernance/hooks **45/45** (plancher intact), branche `main` en phase avec `origin/main`, **aucun résidu NyxVoice** dans les sources suivies. Preuve : `docs/project/evidence/consolidation-2026-07-23/readiness-snapshot.md`.

- **07C résolu** : renommage complet dans les sources, arbre aplati propre ; l'ancien statut « BLOQUÉ/À RÉCONCILIER » est caduc.
- **06B/06C** restent `IMPLEMENTED_AWAITING_USER_REVIEW` (hors ligne, aucune activation live).
- **Point d'attention** : 34 fichiers non commités = tout le travail de session ; à commiter sur une branche puis PR **avec l'accord de l'utilisateur** (push direct sur `main` = R3).
- Chemin vers v1 : commit/PR → décisions 06B/06C → revalidation phase 01 → 09 fournisseurs → 10 perf → 11 packaging → 12 readiness. Cloud live (07D/09B) = infra + secret + autorisation distincte.

## Prochaine phase — décision utilisateur (2026-07-23)

Après consolidation : 08C (apprentissage, sensible, cadrage E-001), ou packaging (10–12), ou décisions 06B/06C — au choix de l'utilisateur.

## Phase 08B — Dictionnaire / glossaire étendu (active) — 2026-07-23

Le PROJECT_DIRECTOR a clôturé 07H (smoke visuel PASS utilisateur « 07H OK »). Après cadrage E-001, l'utilisateur a décidé de **différer 08A (contexte de réécriture)** — son consommateur principal est le prompt Cloud non live — et de faire **08B — dictionnaire/glossaire étendu** d'abord : 100 % local, immédiatement utile, faible risque sur le chemin de réécriture. Garde-fou : gestion des conflits, import/export sûr, suppression.

**Slice 1 (import/export + conflits) — implémenté et testé.** Composant pur `DictionaryExchange` (`Fluent.Rewrite.Dictionary`) : export JSON déterministe + plan d'import (validation via `PersonalDictionaryValidation` existante, politique de conflits explicite ignorer/écraser, bornage capacité, doublons de fichier, JSON invalide géré, contenu importé traité comme donnée jamais exécuté). Preuve : `docs/project/evidence/phase-08b-dictionary-glossary/slice-1-exchange-component.md`.

**Slice 2 (câblage + UI) — implémenté et testé.** `PersistentPersonalDictionary.ApplyImportAsync` (upsert en lot via le chemin gated/validé/persisté existant) ; carte **Importer / Exporter** dans la page Dictionnaire (SaveFileDialog/OpenFileDialog locaux, case « écraser en cas de conflit » décochée par défaut, résumé du résultat). Fichiers locaux uniquement, contenu validé jamais exécuté. Preuve : `docs/project/evidence/phase-08b-dictionary-glossary/slice-2-wiring-ui.md`.

Build Release **0/0**, suite complète **399/399** (Rewrite.Tests 164 → 172). **Reste avant clôture 08B** : smoke visuel import/export (E-011).

## Phase 08A — Contexte de réécriture (DIFFÉRÉE 2026-07-23, décision utilisateur)

Différée sur décision E-001 de l'utilisateur : le contexte de réécriture ne sert vraiment qu'au chemin Cloud, non live. À reprendre quand le Cloud sera live. Aucune implémentation démarrée.

## Phase 07H — UX & résilience (CLÔTURÉE 2026-07-23)

## Phase 07H — UX & résilience (CLÔTURÉE 2026-07-23)

Le PROJECT_DIRECTOR a clôturé 07G (smoke visuel PASS utilisateur « 07G OK », CG-001..CG-008 ; preuve `docs/project/evidence/phase-07g-settings/closure.md`) et démarre **07H — UX et résilience** : accessibilité, navigation clavier, offline, erreurs et recovery.

**Slice 1 (accessibilité navigation/pages) — implémenté et testé.** `AutomationProperties.Name`/`HelpText` sur les 5 entrées de navigation et les 5 pages ; ordre clavier `TabIndex` 0–4. Test markup déterministe (`AccessibilityMarkupTests`). Preuve : `docs/project/evidence/phase-07h-ux-resilience/slice-1-accessibility.md`.

**Slice 2 (erreurs & récupération) — implémenté et testé.** Composant Core pur `DictationErrorPresenter` (étape d'échec → message français sûr + récupération, sans fuite technique), câblé dans `MainWindow` (suivi d'étape ; le catch fatal ne dumpe plus l'exception brute). Preuve : `docs/project/evidence/phase-07h-ux-resilience/slice-2-error-recovery.md`.

Build Release **0/0**, suite complète **391/391** (Core.Tests 20 → 24, IntegrationTests 77 → 88). **Bilan 07H** : accessibilité + clavier + erreurs/récupération couverts ; offline déjà intrinsèque (local-first, statut Offline auth, repli local exact). Reste avant clôture : smoke visuel UX (E-011) ; polish offline optionnel.

## Phase 07G — Paramètres locaux (CLÔTURÉE 2026-07-23)

Le PROJECT_DIRECTOR a clôturé 07F (smoke visuel PASS rapporté par l'utilisateur, CG-001..CG-008 satisfaits ; preuve `docs/project/evidence/phase-07f-history/closure.md`) et démarre **07G — Paramètres** : préférences locales versionnées et réversibles, aucun secret ni consentement Cloud persistant.

**Slice 1 (store + profil préféré) — implémenté et testé.** Store SQLite `fluent-settings.db` dédié (versionné, mono-ligne) + domaine Core ; le **profil de réécriture préféré**, jusque-là session-only, est désormais persisté localement, restauré au démarrage et sauvegardé au changement (badge « ACTIF · ENREGISTRÉ »). Consentement/paramètres Cloud restent session-only. Preuve : `docs/project/evidence/phase-07g-settings/slice-1-preferred-profile.md`.

**Slice 2 (page Paramètres) — implémenté et testé.** Page **Paramètres** WPF (remplace « À venir ») : profil par défaut (sélecteur persistant, synchronisé avec Profils), résumé Historique + bouton « Gérer l'historique », carte stockage local & confidentialité (aucun audio, aucune télémétrie, Cloud session-only). Navigation latérale réelle. Preuve : `docs/project/evidence/phase-07g-settings/slice-2-settings-page.md`.

Build Release **0/0**, suite complète **376/376** (persistance 31 → 40). **Reste avant clôture 07G** : smoke visuel des pages Paramètres + persistance du profil au redémarrage (vérification humaine E-011).

## Phase 07F — Historique local (CLÔTURÉE 2026-07-23)

Le PROJECT_DIRECTOR a démarré et implémenté 07F (prochaine phase produit automatisable selon la roadmap ; 07D exige une infra externe R2/R3, 07E est clôturée).

- **Slice 1 (persistance)** : domaine Core + politique de capture opt-in + store SQLite `fluent-history.db` dédié (rétention, suppression, effacement, préférence opt-in **OFF par défaut**). Base séparée du dictionnaire `fluent.db`.
- **Slice 2 (UI + capture)** : page Historique WPF (interrupteur opt-in, liste, suppression unitaire, tout effacer, recherche), navigation latérale réelle, capture dans `MainWindow` après dictée délivrée (pas de capture sur cible mot de passe/bloquée), textes du dashboard corrigés pour l'honnêteté.

Aucun audio, secret ou donnée externe (P-003/P-004). Vérification : build Release **0/0**, suite complète **367/367** (0 échec). Une régression de test attendue (libellé Overview) corrigée honnêtement. Preuves : `docs/project/evidence/phase-07f-history/slice-1-implementation.md` et `slice-2-ui-capture.md`.

**Reste avant clôture 07F** : smoke visuel de la page Historique (vérification humaine E-011, non automatisable) — voir étapes ci-dessous / dans la conversation.

> Note dépôt : entre deux tours, l'arbre de travail (renommage Fluent + migration de gouvernance) a été aplati dans un unique commit initial `66034b1` sur `main`, en phase avec `origin/main`. Les fils publication (`FV-P07-T018`) et renommage (07C) sont ainsi résolus.

## Migration de gouvernance ADR-0007 (2026-07-23)

Sous autorisation explicite de l'utilisateur, la gouvernance passe du modèle « Juge-gardien + contrat obligatoire par action » (ADR-0003) au modèle **autonome proportionné au risque** (ADR-0007). Le **PROJECT_DIRECTOR** devient l'autorité exécutive sous l'utilisateur ; le **Development Judge** devient auditeur indépendant (verdicts `ALLOW` / `ALLOW_WITH_DEBT` / `BLOCK_CRITICAL`) ; les niveaux **R0–R3** définissent les autorisations.

- Plancher déterministe **préservé** : lecture de secrets, force-push, reset --hard, clean destructif, suppression récursive, format disque, écriture hors dépôt, bypass de permissions restent **DENY**. Registre et installation machine restent **ASK**. Le push non-forcé de branche de travail devient **R1 (ALLOW)** ; le déploiement externe est **contract-gated**.
- Anti-auto-expansion préservé (P-018) : ni le Director ni le Juge ne peuvent modifier les frontières R0–R3, les hooks ou le plancher ; seul l'utilisateur le peut.
- Vérification réelle : **suite hooks + gouvernance 45/45 PASS**. Preuve : `docs/project/evidence/governance-migration-2026-07-23/hook-tests.json` (dont G-001..G-007, non-force push R1, déploiement contract-gated, `.env` DENY, redaction secret).
- ADR-0007 passé à **Accepted** après cette vérification. ADR-0003 marqué opérationnellement superseded.

**Première action du PROJECT_DIRECTOR** — clôture de la phase **07E** selon les critères CG-001..CG-008 : les preuves préexistaient (verdict Judge ALLOW de clôture consigné, smoke visuel PASS déjà rapporté par l'utilisateur). Le nouveau modèle supprime la seule chose qui manquait, la phrase rituelle ; aucune nouvelle preuve n'est inventée. Gemini **n'est pas** live ; 07D reste ouverte sans autorisation live ; le renommage 07C reste **à réconcilier**.

## Phase 07B clôturée

La Phase **07B — authentification native Supabase** est clôturée le 2026-07-22 sous autorité explicite de l'utilisateur. Le parcours OAuth Google principal et les contrôles de session sont PASS. Aucune valeur OAuth sensible n'est conservée dans les documents.

Les quatre contrôles manuels restants sont PASS : restauration après relance, comportement hors ligne, déconnexion effective après relance et réinitialisation de l'activation/consentement Cloud de session. Le verdict final du Judge est ALLOW et l'utilisateur a explicitement accepté la clôture.

## Phase active réelle

**07E — tableau de bord dynamique local** est **clôturée le 2026-07-23** par le PROJECT_DIRECTOR sous le modèle ADR-0007. Le contrat FV-P07-T016, validé par le Judge, limitait le travail à la présentation éphémère de sources locales réelles : profil, dictionnaire, session et autorisation Cloud. Il n’introduit ni historique, ni persistance, ni contenu dicté, ni identité de compte, ni opération Cloud. Le smoke visuel est PASS (rapporté par l’utilisateur) et le verdict de clôture Judge est ALLOW. Sous CG-001..CG-008, ces preuves suffisent ; la phrase rituelle n’est plus requise.

**07D — préparation desktop Gemini authentifiée sans déploiement** reste ouverte, avec sa tranche locale explicitement acceptée par l’utilisateur. Elle ajoute seulement l'origine publique `FLUENT_BACKEND_URL`, validée comme HTTPS racine et passée au transport Desktop. Sans origine valide, l'activation Cloud est indisponible ; aucune requête, clé, `.env`, backend ou fournisseur n'est utilisé dans cette tranche. Aucune autorisation live n’a été donnée.

## Réalité fonctionnelle vérifiée

- Dictée locale, insertion protégée, profils locaux et dictionnaire SQLite persistant sont implémentés ; le dictionnaire est la seule persistance inspectée dans le code.
- L'Overview est partiellement dynamique ; Dictionnaire et Profils sont des pages réelles.
- Historique et Paramètres sont explicitement `À venir` : aucune page, modèle ou base correspondante n'existe.
- L'authentification Supabase utilise navigateur système, PKCE S256, callback éphémère `127.0.0.1`, access token en mémoire et refresh token Windows Credential Manager.
- Cloud est sécurisé par session + origine backend valide + activation + consentement. Aucun backend n'est déployé par cette phase ; une origine configurée ne rend donc pas Gemini live et le fallback local exact reste obligatoire.
- 06B (Gemini) et 06C (DeepSeek) sont implémentées hors ligne et `IMPLEMENTED_AWAITING_USER_REVIEW`. DeepSeek n'est pas une activation live et aucune conclusion ne dépend du contenu de `.env`.

## Vérification de référence

- Phase 07B : build Release 0/0, suite 301/301 et gardes 33/33.
- Phase 07D : tests ciblés 17/17, build Release 0/0, suite complète 318/318 et gardes 33/33 PASS ; verdict final du Judge ALLOW, puis acceptation utilisateur de la tranche hors ligne. La phase reste ouverte, sans activation live.
- Phase 07E : tests ciblés 28/28, build Release 0/0, suite complète 346/346 et gardes 33/33 PASS ; verdict du Judge ALLOW pour présentation puis pour clôture technique, smoke visuel PASS rapporté par l’utilisateur. En attente d’une décision utilisateur explicite de clôture.

## Santé du dépôt

- Branche `main`, remote `origin` vers `https://github.com/exhorte/Fluent.git`.
- L'arbre comporte 301 changements préexistants de renommage technique : 117 modifiés, 155 supprimés, 29 non suivis. Aucun changement Git n'a été fait pendant l'audit.
- Les chemins source et projets sont déjà nommés Fluent ; seuls des caches `.vs` et des identifiants internes de compatibilité contiennent encore Nyx/NyxVoice.
- La phase 07C de renommage technique reste **BLOQUÉE / À RÉCONCILIER** : son document est obsolète et le renommage n'est pas réconcilié dans Git. Ce point doit être traité par un contrat distinct avant packaging ou publication.

## Prochaine action recommandée

Attendre l’acceptation explicite de clôture de 07E. Une activation Gemini réelle reste une démarche distincte : URL backend contrôlée, déploiement, secret serveur, coût éventuel et smoke externe.

La matrice des pages, les dépendances et les brouillons sont dans `docs/project/evidence/phase-07-planning-audit/`.
