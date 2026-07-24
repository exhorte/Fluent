# Phase 07C — Renommage technique intégral vers Fluent

Statut : BLOQUÉ — AUTORISATION SYSTÈME REQUISE POUR LES DOSSIERS RESTANTS

## Objectif

Achever le renommage de Fluent vers Fluent dans le dépôt, la solution .NET, les projets, les espaces de noms, les ressources, les tests, la documentation et les données locales de l’utilisateur, sans modifier le comportement fonctionnel, les secrets, GitHub, Supabase ou les contenus de données.

## Inclus

- Renommer `Fluent.sln`, chaque projet, dossier et fichier versionné qui contient `Fluent`, ainsi que les dossiers racines du dépôt vers leurs équivalents Fluent.
- Remplacer les identifiants textuels `Fluent`, `fluent` et `FLUENT` par les formes Fluent correspondantes dans les sources, tests, fichiers projet, documentation, scripts et historique de session, hors secrets et fichiers générés.
- Migrer la racine locale existante `%LOCALAPPDATA%\Fluent` vers `%LOCALAPPDATA%\Fluent`, puis `fluent.db` vers `fluent.db`, par déplacement réversible seulement si la destination est absente, que Fluent/Fluent ne sont pas en cours d’exécution et après empreinte de contrôle. L’utilisateur a explicitement confirmé cette migration le 22 juillet 2026.
- Mettre à jour `docs/history_session.md` avec la trace de cette migration, sans inventer de résultat de test.

## Exclus

- Lire, modifier, imprimer, versionner ou charger `.env`, `.env.*`, clés, jetons, certificats ou identifiants.
- Modifier `.git`, le remote GitHub, les intégrations Supabase/Google/DeepSeek, les comptes, les déploiements ou les données serveur.
- Modifier le contenu de la base SQLite, de modèles Whisper, d’audio ou de toute donnée utilisateur ; seuls les noms et dossiers locaux sont déplacés.
- Toucher `bin`, `obj`, caches IDE, packages ou artefacts générés : ils sont régénérés par le build.

## Critères d’acceptation

1. Le dépôt est accessible sous `C:\SECOND_BRAIN\PROJECTS\Fluent\Fluent` et la solution devient `Fluent.sln`.
2. Tous les projets, dossiers, fichiers, namespaces, assembly names, imports et références de solution passent à Fluent.
3. `rg -i "Fluent"` ne retourne aucun fichier texte versionné ou source dans le dépôt, hors `.git`, `bin` et `obj`.
4. `%LOCALAPPDATA%\Fluent\fluent.db` garde la même empreinte SHA-256 que la base source avant déplacement, et le dossier `Models` reste présent ; aucune destination préexistante n’est écrasée.
5. Restore, build Release et suite de tests de `Fluent.sln` passent sans avertissement ni erreur de compilation.
6. L’historique de session, les ADR, l’état du projet, la roadmap et les preuves décrivent la réalité Fluent.
7. Aucune configuration Supabase/Google/DeepSeek, aucun secret, aucun appel fournisseur ni aucune opération Git n’est exécuté.

## Réversibilité

Avant toute publication Git, les déplacements de dossiers et fichiers sont renversables par la table de correspondance inverse. La migration de données locales suit une machine d’état explicite et reste un renommage sur le même volume ; aucun contenu n’est supprimé ni réécrit.

## Protocole de migration des données locales

1. **Préparation** — Relever un manifeste complet de `%LOCALAPPDATA%\Fluent` (chemins, tailles, dates), incluant explicitement `fluent.db`, `fluent.db-wal`, `fluent.db-shm` s’ils existent et `Models`. Vérifier que la source existe, que `%LOCALAPPDATA%\Fluent` est absente, que les deux chemins sont sur le même volume, et qu’aucun processus `Fluent` ou `Fluent` ne s’exécute.
2. **Préservation** — Calculer SHA-256 de tous les fichiers de base SQLite présents sans ouvrir ni modifier leur contenu. Si une vérification, un verrou ou une précondition échoue, ne rien déplacer et signaler l’état.
3. **Renommage du dossier** — Exécuter uniquement `Rename-Item` de `Fluent` vers `Fluent` dans `%LOCALAPPDATA%`. En cas d’échec, le répertoire source reste la référence et aucune seconde tentative destructive n’est menée.
4. **Renommage de la base** — Exécuter uniquement `Rename-Item` de `fluent.db` vers `fluent.db` (et de ses sidecars éventuels vers les noms Fluent correspondants) dans le dossier nouvellement renommé.
5. **Vérification** — Relever le manifeste post-migration, vérifier les empreintes, la présence de `Models`, la disparition de la source et l’absence de destination préexistante écrasée.
6. **Récupération d’interruption** — Si le dossier a été renommé mais que le renommage de la base ou la vérification échoue, arrêter immédiatement, conserver les manifestes, puis renommer seulement les éléments déjà renommés en sens inverse lorsque leurs destinations inverses sont libres. Si cette restauration est impossible, ne rien écraser et demander une intervention utilisateur.

## Blocage de l’exécution

La migration des données et le renommage des fichiers ont réussi. Les 18 dossiers techniques restants sont protégés par une autorisation Windows : après retrait contrôlé puis restauration de leur attribut ReadOnly, `Rename-Item` retourne `Access denied` avant tout déplacement. Aucune ACL, propriété de fichier, contenu, donnée utilisateur, secret ou élément Git n’a été modifié. Une session PowerShell élevée de l’utilisateur, ou l’attribution explicite du droit de suppression/renommage à son compte sur le dépôt, est requise avant reprise.
