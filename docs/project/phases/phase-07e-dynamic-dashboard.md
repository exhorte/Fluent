# Phase 07E — Tableau de bord dynamique

Statut : TECHNICALLY_READY_FOR_USER_CLOSURE — verdict de clôture Judge ALLOW ; autorité utilisateur explicite requise

## Objectif

Faire évoluer l'Overview en tableau de bord local fondé exclusivement sur des sources de données réelles, sans inventer d'historique, de statistiques Cloud ou de comptes.

## Contrat prévu

- Définir chaque métrique, sa source, sa durée de vie et ses états chargement/vide/erreur.
- Exposer les états réels de dictée, dictionnaire, profil, authentification et disponibilité Cloud.
- Ajouter des tests de calcul et de présentation, y compris données absentes et erreurs.
- Préserver le local-first, l'absence de télémétrie et l'absence de texte dicté dans les métriques.

## Dépendances et exclus

Cette phase dépend de 07D seulement pour des statuts Cloud véridiques ; elle ne crée ni stockage d'historique ni paramètres persistants, qui appartiennent à 07F et 07G.

Le tableau de bord ne déduit jamais la disponibilité d’un backend, de Gemini ou d’un fournisseur depuis une autorisation locale. Toute valeur affichée doit provenir d’une source locale documentée et rester éphémère.

La vérification déterministe est PASS : 28/28 tests ciblés, build Release 0/0, suite complète 346/346 et hooks 33/33. Le smoke visuel défini dans les preuves est PASS, rapporté explicitement par l’utilisateur. Le verdict technique de clôture est ALLOW ; la clôture reste soumise à une autorité utilisateur explicite.
