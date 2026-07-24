# Audit factuel Fluent — 2026-07-22

## Périmètre et méthode

Audit documentaire et en lecture seule du dépôt Fluent. Aucun secret, fichier `.env`, jeton, code OAuth, compte externe, base locale utilisateur ou configuration Supabase/Google n'a été lu ou modifié. Aucun code, test produit, package, déploiement ni état Git n'a été modifié.

## État Git observé

- Dépôt : `C:\SECOND_BRAIN\PROJECTS\Fluent\Fluent`
- Branche : `main`
- HEAD : `886ea85adb70975f4d5f44e9d6bc71b4a7e60dbd` — `Add phases 03-06B: visual foundation, safe rewriting, dictionary, profiles, optional cloud engine`
- Remote : `origin` vers `https://github.com/exhorte/Fluent.git` (fetch et push)
- Arbre de travail déjà non propre avant cet audit : 301 entrées (`117` modifiées, `155` supprimées, `29` non suivies).

Ces changements correspondent principalement au renommage physique `NyxVoice` vers `Fluent` : les anciens chemins suivis sont vus comme supprimés et les nouveaux chemins Fluent comme non suivis. Aucun ajout, suppression, staging, commit, reset, clean, push ou réécriture Git n'a été exécuté pendant cet audit.

Le contrôle `git diff --check` ne signale pas de défaut de whitespace bloquant ; Git avertit seulement que certains fichiers passeront de LF à CRLF lorsqu'il les réécrira. Cet avertissement n'est pas corrigé ici.

## Solution et renommage réellement présents

- `Fluent.sln` et 18 projets `Fluent.*` existent dans l'arbre de travail (9 source, 8 tests, 1 harness).
- Aucun chemin de source, test ou outil actif ne porte `Nyx`/`NyxVoice` hors artefacts générés et `.vs`.
- Les seuls dossiers nommés `NyxVoice` encore présents sont des caches Visual Studio sous `.vs/`; ils ne sont ni source ni versionnés.
- Des identifiants internes `Nyx.*` subsistent dans les clés de ressources WPF, le nom de collation SQLite `NYX_ORDINAL_IGNORE_CASE`, quelques fixtures de test et un nom de contrôle visuel. Ils ne sont pas des noms de fichiers publics et certains sont des identifiants de compatibilité interne ; ils ne doivent pas être renommés par remplacement global sans contrat de migration et tests.

Le document de phase 07C annonce encore un blocage de renommage de dossiers. Cette assertion est périmée par rapport à l'arbre physique courant, mais la phase ne peut pas être clôturée : le renommage n'est pas réconcilié dans Git et le document lui-même contient des substitutions erronées (`Fluent` vers `Fluent`). Il reste donc **BLOQUÉ / À RÉCONCILIER**, sans intervention dans cette tâche.

## État produit et phase réellement active

La phase produit active est **07B — Authentification native Supabase**. Le parcours OAuth principal a été rapporté réussi par l'utilisateur après le correctif 07B-R1 : confirmation Google, retour vers le callback loopback Fluent, sortie de l'état de connexion, utilisateur connecté, Cloud non activé et action de déconnexion visible. Aucun code d'autorisation n'est conservé dans cette preuve.

La phase 07B n'est pas clôturée. Les contrôles manuels suivants restent distincts et non validés : rafraîchissement après relance, état hors ligne, déconnexion effective après relance et réinitialisation du consentement Cloud de session.

La phase 06B (Cloud optionnel) et la préparation 06C (DeepSeek) sont implémentées hors ligne et attendent leur propre revue. Le backend n'est pas déployé et le Desktop ne fournit actuellement aucune `BaseAddress` backend ; même connecté, Fluent reste donc local par conception.

## Écarts documentaires trouvés

| Élément | Constat factuel | Écart à corriger |
| --- | --- | --- |
| README | Il annonce 07A et une prochaine phase 01. | État obsolète. |
| ADR-0006 | Il exige un nouveau retest OAuth. | Le retest principal a depuis réussi ; les contrôles de session restent ouverts. |
| `project-state.md` | Il mélange des preuves anciennes (271/294 tests), le premier échec et l'état actuel. | Réduire à l'état vérifié actuel et garder le détail dans les preuves. |
| Roadmap | 07B y annonce encore le smoke à venir et 06C comme simple préparation désactivée. | Réconcilier avec 301 tests et les preuves 06C/07B. |
| Phase 07C technique | Son texte utilise des substitutions de renommage incohérentes. | Marquer l'écart, ne pas le réparer dans cette tâche. |

## Dette et risques ouverts

1. Le renommage technique non réconcilié rend tout futur diff, commit ou packaging difficile à auditer.
2. Le grand `MainWindow` contient encore de l'orchestration métier malgré la règle MVVM ; cette dette doit être traitée par une phase dédiée, pas mélangée aux pages fonctionnelles.
3. L'application n'a ni repository, ni modèle de données, ni page pour l'historique ; aucune donnée de dictée n'est actuellement persistée.
4. Les paramètres n'ont pas de modèle persistant ; le consentement et l'activation Cloud sont volontairement limités à la session.
5. L'authentification OAuth est validée pour le parcours principal mais pas encore pour la restauration, la perte réseau et la suppression de session.
6. Le transport Cloud est construit mais injoignable dans l'application actuelle car aucune URL backend n'est fournie et aucun backend n'est déployé.

## Sources inspectées

- `CLAUDE.md`, règles, hooks, constitution et politiques du Judge.
- README, vision, exigences, confidentialité, flux utilisateur, profils vocaux, architecture, ADR-0001 à ADR-0006, qualité et stratégie de tests.
- Tous les documents de phase 00 à 07C, les contrats et verdicts disponibles dans `docs/project/evidence`.
- Solution, projets, pages WPF, auth Supabase, transport Cloud, backend et persistance SQLite, uniquement en lecture.
- État Git, build, tests et tests de garde consignés dans `verification-summary.md`.
