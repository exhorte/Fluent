# User Flows

## Launch

L’utilisateur démarre Fluent localement. L’application initialise la configuration, la disponibilité du modèle, l’état du dictionnaire et l’état de la zone de notification sans exiger d’authentification.

## Recording

L’utilisateur place le curseur dans un champ de texte et appuie sur Ctrl+Space. Fluent verrouille la cible courante et démarre l’enregistrement avec une capsule non activante.

## Stop

The user presses Ctrl+Space again. Recording stops and the audio buffer is sent to local transcription without saving audio by default.

## Transcription And Transformation

The transcript is processed by the dictionary, selected profile, and output validator. The validator checks preservation invariants before insertion.

## Insertion

Si la cible initiale est toujours valide et autorisée, Fluent insère le texte sans envoyer Entrée ni exécuter de commande.

## Cancellation

The user can cancel before insertion. No text is inserted.

## Target Lost

Si la cible disparaît ou change, Fluent copie le résultat dans le presse-papiers et l’indique explicitement au lieu de coller dans une nouvelle cible.

## Forbidden Field

If the target is a password field or cannot be safely identified, insertion is blocked.

## Model Error

The app shows a recoverable error and preserves user control. It does not invent a result.

## Overview Dashboard

La Vue d’ensemble présente uniquement des états locaux courants : profil, état global de dictée, dictionnaire, session et autorisation Cloud. Elle n’enregistre pas d’activité, n’affiche ni texte dicté, ni contenu du dictionnaire, ni identité de compte, et ne déclenche aucune action. Un statut Cloud « Autorisé localement » décrit les gardes de session, d’activation et de consentement ; il ne prouve pas qu’un backend ou fournisseur est joignable.

## Elevated Application

Si la cible s’exécute à un niveau d’intégrité avec lequel Fluent ne peut pas interagir de manière sûre, l’insertion est bloquée ou exige une décision de conception explicite ultérieure.

# Phase 07B — connexion et réécriture Cloud

1. Dans **Profils**, Fluent affiche son état de connexion sans afficher de jeton.
2. L’utilisateur choisit **Se connecter avec Google**. Fluent ouvre le navigateur système et attend un callback local éphémère; l’utilisateur peut annuler.
3. Après une connexion réussie, Fluent affiche uniquement le nom et l’e-mail retournés par Supabase. L’utilisateur peut se déconnecter à tout moment.
4. La connexion ne modifie pas le mode de dictée. L’utilisateur doit toujours activer Cloud et accepter séparément l’envoi du texte transcrit.
5. L’utilisateur peut choisir Gemini (par défaut) ou DeepSeek V4 Pro pour la session ; ce choix n’active pas Cloud et ne vaut jamais consentement.
6. Sans origine backend publique valide, avec une session expirée, hors ligne, sans consentement ou sans fournisseur disponible, Fluent explique le statut et utilise exclusivement la réécriture locale. Une origine configurée n'est pas une preuve que le backend est déployé ou que Gemini est activé.
