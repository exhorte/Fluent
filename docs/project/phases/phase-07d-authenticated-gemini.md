# Phase 07D — Réécriture Gemini authentifiée

Statut : IMPLEMENTED_OFFLINE_SLICE_USER_ACCEPTED_LIVE_AUTHORIZATION_REQUIRED — tranche locale hors ligne validée par le Judge et acceptée par l’utilisateur ; aucune activation live

## Objectif

Préparer le Desktop à connaître, sans secret, l'origine publique HTTPS d'un backend Fluent. Cette tranche ne déploie pas ce backend et ne fait aucun appel Gemini ; elle conserve le mode Local tant que toutes les gardes ne sont pas satisfaites.

## Livré dans la tranche hors ligne

- `FLUENT_BACKEND_URL` est lu uniquement dans l'environnement du processus Desktop, jamais depuis `.env`.
- Seule une origine HTTPS absolue à la racine est admise ; userinfo, query, fragment, port non standard et chemin sont refusés.
- L'origine validée alimente `CloudBackendOptions` ; sans elle, `BaseAddress` est nulle.
- L'activation Cloud dans Profils exige désormais à la fois une session authentifiée et une origine backend configurée. L'activation, le consentement et le choix de fournisseur restent session-only.
- Les tests utilisent seulement des valeurs de configuration synthétiques ; aucun réseau, backend, navigateur ou fournisseur n'est démarré.
- Le Development Judge a rendu ALLOW pour présentation à l'utilisateur après build Release 0/0, suite complète 318/318 et gardes 33/33.
- L’utilisateur a accepté cette tranche hors ligne le 2026-07-22. Cette acceptation ne clôt pas la phase 07D et n’autorise aucune opération live.

## Prérequis pour la suite live

- Phase 07B est clôturée après ses vérifications de session.
- Phase 06B revue par l'utilisateur.
- Autorisation utilisateur distincte pour le déploiement, la configuration serveur, la clé Gemini et tout coût potentiel.
- Backend avec validation JWT Supabase asymétrique opérationnelle.

## Contrat live futur, non autorisé par cette tranche

- Configurer le Desktop uniquement avec une URL backend publique et contrôlée ; jamais de clé fournisseur, clé Supabase privilégiée ou modèle.
- Garder Local comme défaut et calculer le résultat local avant tout appel Cloud.
- Exiger simultanément session valide, Cloud activé, consentement de session et Gemini sélectionné.
- Vérifier un smoke contrôlé : succès Gemini, 401, 403, 429, 503, délai et réponse invalide retombent tous sur le texte local exact.
- Ne journaliser aucun texte de dictée, token, secret ou réponse fournisseur brute.

## Exclus

DeepSeek live, routage automatique entre fournisseurs, historique, synchronisation, paiement, télémétrie, persistance du consentement, déploiement et appel Gemini réel.
