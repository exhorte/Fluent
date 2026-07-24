# Phase 07A — Rebranding produit Fluent

Statut : IMPLÉMENTÉ — en attente de la confirmation utilisateur

## Objectif

Faire de **Fluent** le nom produit visible de l'application de dictée Windows et de son binaire, sans modifier le comportement de dictée, les protections de sécurité, les fournisseurs Cloud ou les données locales existantes.

## Inclus

- Remplacer `Fluent` par `Fluent` dans les surfaces WPF visibles : titre de fenêtre, en-tête du tableau de bord et capsule flottante.
- Remplacer Fluent par Fluent dans le message d’état public affiché lorsqu’une dictée est déjà en cours de traitement.
- Produire l'exécutable sous le nom `Fluent.exe`, avec les métadonnées produit correspondantes.
- Mettre à jour les documents produit actuels et le README pour employer Fluent.
- Vérifier que la solution Release et la suite complète de tests restent vertes.

## Compatibilité explicitement préservée

- Les espaces de noms, dossiers et noms de projets internes `Fluent.*` restent inchangés dans cette tranche.
- Le répertoire de données local historique `%LOCALAPPDATA%\Fluent` reste inchangé : aucune migration ni perte du dictionnaire existant.
- Le dépôt Git, son remote GitHub, le nom de la solution et la phase 06B restent inchangés.

## Exclus

- Activation de DeepSeek, authentification, clés API, déploiement ou appel Cloud réel.
- Nouvelles dépendances, modification des règles de sécurité, changement de hotkey ou de pipeline de dictée.
- Renommage physique du dépôt, des répertoires, des namespaces ou de la base locale.

## Critères d'acceptation

1. Les trois surfaces WPF visibles affichent Fluent.
2. Le build Release génère `Fluent.exe` sans avertissement ni erreur.
3. Les données de dictionnaire Fluent existantes restent compatibles, sans opération sur les données réelles.
4. Tous les tests existants passent et aucun comportement fonctionnel n'est modifié.

## Vérification prévue

- Recherche ciblée des anciennes chaînes visibles dans `src/Fluent.App`.
- `dotnet build Fluent.sln -c Release -m:1 --disable-build-servers`.
- `dotnet test Fluent.sln -c Release --no-build -m:1 --disable-build-servers`.
