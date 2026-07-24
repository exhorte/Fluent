# Phase 07F — Historique fonctionnel local

Statut : BROUILLON — non active ; aucun contrat approuvé

## Objectif

Introduire un historique local, transparent, contrôlable et supprimable, sans audio et sans transmission Cloud implicite.

## Décisions nécessaires avant activation

- Contenu conservé, durée de rétention, taille maximale et statut par défaut.
- Consentement explicite avant toute persistance de texte de dictée.
- Modèle SQLite, version de schéma, migrations et plan de suppression unitaire/totale.
- UX de recherche, vide, erreur, export éventuel et effacement irréversible.

## Contrat prévu

Prévoir tests de migration sur bases temporaires, chiffrement ou protection si justifiée par une ADR, absence d'audio, et vérification qu'une désactivation arrête les nouvelles écritures. Aucune base utilisateur réelle ne sera migrée sans autorité distincte.
