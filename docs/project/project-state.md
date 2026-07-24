# Project State

Status: `GOVERNANCE_ADR_0007_ADOPTED` · `PHASE_07E_CLOSED` · active task `FV-P07-T018` (publication) inchangée

Date: 2026-07-23

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
