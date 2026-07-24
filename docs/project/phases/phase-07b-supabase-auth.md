# Phase 07B — Authentification native Fluent avec Supabase

Statut : CLOSED_USER_ACCEPTED_TECHNICALLY_COMPLETE — clôturée le 2026-07-22 sous autorité utilisateur après verdict final ALLOW

## Objectif

Permettre à Fluent (WPF Windows) de se connecter avec Google par Supabase Auth, au moyen du navigateur système et du flux OAuth PKCE S256, sans WebView et sans secret embarqué. Une session authentifiée pourra déverrouiller le chemin Cloud déjà livré en 06B, seulement après activation et consentement explicites pour la session. Le mode local reste inchangé et prioritaire.

## Inclus

- Connexion Google native avec navigateur système, callback loopback éphémère `http://127.0.0.1:<port>/callback`, PKCE S256, état OAuth du fournisseur possédé par Supabase, délai, annulation et rejet des callbacks invalides.
- Échange de code et rafraîchissement auprès de Supabase Auth avec les seules valeurs publiques `FLUENT_SUPABASE_URL` et `FLUENT_SUPABASE_PUBLISHABLE_KEY` fournies par l’environnement du processus ; le Desktop ne lit jamais `.env`.
- Session applicative : access token en mémoire uniquement ; refresh token dans Windows Credential Manager, suppression sûre à la déconnexion ou après rejet définitif ; démarrage avec tentative de rafraîchissement single-flight et distinction explicite entre session expirée et service indisponible.
- Interface de profil : connexion Google, état de chargement/annulation, déconnexion, nom et e-mail non sensibles, et état Cloud véridique. Le consentement Cloud reste distinct, session-only et inchangé.
- Validation serveur cryptographique des JWT Supabase asymétriques par découverte OIDC/JWKS mise en cache et actualisable : signature, algorithmes autorisés RS256/ES256, issuer, audience `authenticated`, expiration, `nbf` et sujet obligatoire. Toute ambiguïté échoue fermée.
- Limitation du backend par identifiant utilisateur validé, avec réponses distinctes 401, 403, 429 et 503, sans contenu utilisateur ni token dans les journaux.
- Tests unitaires, d’intégration et de présentation pour PKCE, callback, échange/refresh, persistance sûre, session, garde Cloud, JWT/JWKS, limitation, UI et scans de sécurité.

## Exclus

- WebView, mot de passe local, gestion de compte, synchronisation, télémétrie, déploiement et appel réel au fournisseur de réécriture.
- Secret OAuth, service-role, JWT secret, clé de fournisseur ou clé DeepSeek dans le Desktop, le backend, le dépôt ou les tests.
- Lecture, écriture, affichage ou chargement automatique de `.env` ; le fichier racine existant demeure hors périmètre.
- Activation, sélection ou appel de DeepSeek V4 Pro. Gemini reste le seul fournisseur Cloud potentiel et il reste inaccessible sans toutes les gardes existantes.
- Modification du pipeline local de transcription, dictionnaire, réécriture locale, insertion, capsule, hotkey ou protections de cible.
- Configuration ou mutation d’un projet Supabase, Google Cloud ou GitHub ; les actions de compte et de déploiement exigent l’autorisation et les accès de l’utilisateur.

## Préconditions externes pour le smoke réel

1. Le projet Supabase utilise des clés de signature asymétriques RS256 ou ES256 et expose son JWKS.
2. Google est activé dans Supabase et son callback Google est celui fourni par la console Supabase.
3. La liste Supabase des URL de redirection autorise le callback loopback éphémère de Fluent, par exemple `http://127.0.0.1:*/callback` selon la configuration validée dans la console.
4. Le processus Desktop reçoit seulement `FLUENT_SUPABASE_URL` et `FLUENT_SUPABASE_PUBLISHABLE_KEY`; le backend reçoit seulement `FLUENT_BACKEND_SUPABASE_ISSUER` et `FLUENT_BACKEND_SUPABASE_AUDIENCE`. Aucune de ces valeurs ne sera copiée dans le dépôt.

## Frontières de confiance et paramètres figés

### Configuration Desktop publique

- `FLUENT_SUPABASE_URL` doit être une URI `https` absolue, sans information utilisateur, query, fragment ou port non standard, dont l’hôte se termine exactement par `.supabase.co`.
- `FLUENT_SUPABASE_PUBLISHABLE_KEY` est une valeur publique non vide ; elle ne donne aucun accès privilégié et n’est jamais affichée ni journalisée.
- Sans ces deux valeurs valides, le bouton de connexion est désactivé avec l’état explicite « Authentification non configurée ». Aucun navigateur, listener ni appel réseau n’est lancé.

### Configuration backend de confiance

- Le backend lit uniquement `FLUENT_BACKEND_SUPABASE_ISSUER` et `FLUENT_BACKEND_SUPABASE_AUDIENCE` dans l’environnement du processus. Les deux sont obligatoires ; l’absence ou une valeur invalide rend l’authentification indisponible et l’endpoint renvoie 503 sans tenter de deviner une valeur à partir d’un Bearer token.
- L’issuer doit être l’URI exacte `https://<project-ref>.supabase.co/auth/v1`, sans query, fragment, userinfo ni port non standard. L’audience requise est exactement `authenticated` pour cette phase.
- L’URL de métadonnées OIDC et le JWKS sont construits exclusivement depuis cet issuer de confiance. Après chargement, le metadata `issuer` et `jwks_uri` doivent être égaux respectivement à l’issuer configuré et à `<issuer>/.well-known/jwks.json`; toute divergence est refusée. Aucune URL de découverte ou de JWKS n’est lue depuis un claim, un en-tête ou une réponse non liée à cet issuer.
- Le client de métadonnées impose HTTPS, un timeout de 5 secondes, cache les clés au plus 12 heures et, lorsqu’un `kid` est inconnu, demande un seul rafraîchissement puis retente la validation une seule fois. En cas de cache absent, de timeout, de réponse invalide ou de clé toujours inconnue, le backend échoue fermé.
- Seuls `RS256` et `ES256` sont acceptés. `none`, HS256, toute confusion d’algorithme, tout token non signé, ainsi que les valeurs invalides de `iss`, `aud`, `exp`, `nbf` ou `sub`, donnent 401. Aucun secret JWT symétrique n’est accepté ou configuré.

### Callback loopback et redirection

- Avant l’ouverture du navigateur, Fluent lie un `TcpListener` uniquement sur `IPAddress.Loopback` (`127.0.0.1`) avec le port `0` afin que Windows attribue le port éphémère ; l’URI exacte est ensuite `http://127.0.0.1:<port>/callback`. `localhost`, `0.0.0.0`, une adresse IPv6 ou une adresse LAN ne sont pas utilisés.
- La configuration Supabase doit autoriser le modèle dynamique exact `http://127.0.0.1:*/callback` dans les Redirect URLs avant un smoke réel. Cette modification externe n’est pas faite dans cette phase. Si Supabase refuse le retour, Fluent n’échange aucun code, affiche l’échec non sensible et reste local ; il ne bascule jamais sur un port fixe.
- Le listener n’accepte qu’une requête `GET` pour `/callback`, avec 8 KiB maximum d’en-têtes, aucun corps, un timeout de connexion de 5 secondes et un délai total de connexion de 120 secondes. Une erreur fournisseur, un chemin invalide, une duplication, une annulation ou un timeout arrête le listener sans échange de code. Après le premier callback terminal, le socket est fermé ; les callbacks tardifs ou rejoués sont ignorés.

### Backend et réponses

Le seul endpoint concerné est `POST /v1/rewrite`; il remplace l’authentification statique de Phase 06B et n’accepte plus `FLUENT_BACKEND_TOKEN`. Son ordre est fixé : configuration/verifier indisponible → 503 ; en-tête absent ou JWT invalide → 401 ; JWT cryptographiquement valide mais rôle différent de `authenticated` → 403 ; acquisition du quota du `sub` validé sans file d’attente → 429 ; requête de réécriture invalide → 400 ; Gemini indisponible → 503 ; succès → 200. Aucune limitation ne dépend d’un en-tête non validé, d’une adresse IP ou du token brut.

### Dépendance autorisée

Seule la référence directe `Microsoft.IdentityModel.Protocols.OpenIdConnect` version `8.19.2` est ajoutée à `src/Fluent.Backend/Fluent.Backend.csproj`, avec la version centralisée dans `Directory.Packages.props`. Ses dépendances transitives `Microsoft.IdentityModel.Protocols`, `Microsoft.IdentityModel.Tokens` et `System.IdentityModel.Tokens.Jwt` sont utilisées uniquement pour la validation OIDC/JWKS ; aucune bibliothèque Supabase, navigateur embarqué ou SDK de secret n’est ajoutée.

## Contrat de comportement

- Sans configuration publique valide, sans session authentifiée, sans Cloud activé, sans consentement, sans backend disponible ou sans Gemini activé, la dictée reste intégralement locale et aucun texte n’est envoyé au Cloud.
- La connexion utilise une instance PKCE par tentative. Le verifier et le challenge restent transitoires en mémoire. Fluent n’émet, ne compare et ne journalise aucun `state` applicatif dans le flux social Supabase ; Supabase conserve la responsabilité de son état OAuth interne.
- Le callback écoute seulement `127.0.0.1`, sur un port attribué par le système, pour le seul chemin `/callback`, une seule requête attendue, puis le listener est arrêté.
- L’access token est uniquement en mémoire. Seul le refresh token Supabase peut persister dans Windows Credential Manager sous l’utilisateur Windows courant ; il n’est jamais journalisé ni affiché.
- Si un refresh échoue pour une raison d’authentification, le refresh token est supprimé et la session devient expirée. Si le service est indisponible, le refresh token est conservé mais aucune session Cloud n’est considérée valide.
- Le backend ne fait confiance à aucune simple présence de Bearer token : il valide cryptographiquement le JWT avec un JWKS de l’issuer configuré, rejette HS256 et tout algorithme non autorisé, vérifie `iss`, `aud`, `exp`, `nbf` et `sub`, puis limite par `sub` validé.
- Les tokens, le texte de dictée, l’audio, les refresh tokens, les clés, les code verifiers et les codes d’autorisation ne sont ni écrits dans les logs, ni affichés dans l’UI, ni inclus dans l’observabilité.

## Acceptation

1. Les tests PKCE prouvent un verifier aléatoire, un challenge SHA-256 Base64URL, l’absence de `state` applicatif et l’échec de tout callback invalide, tardif ou annulé.
2. Les tests de session prouvent le single-flight, l’absence de persistance de l’access token, le stockage abstrait du seul refresh token, l’effacement après rejet définitif et la distinction offline/expiré.
3. Les tests d’orchestration prouvent que l’authentification seule ne déclenche jamais le Cloud et que DeepSeek n’est jamais appelé.
4. Les tests backend prouvent les rejets pour token absent, signature/issuer/audience/algorithme/expiration/`nbf`/sujet invalides, l’acceptation d’un JWT valide, la clé JWKS inconnue avec actualisation, les 401/403/429 et la limitation par utilisateur validé.
5. Les tests de présentation prouvent les états connexion, chargement, annulation, connecté, offline, expiré, déconnexion et le maintien du consentement Cloud séparé.
6. Build Release propre et suite complète verte ; scans garantissent l’absence de secret, de WebView, de DeepSeek actif et de persistance d’access token.
7. Le premier smoke Supabase/Google réel du 22 juillet 2026 a échoué avec `bad_oauth_state` et redirection de fallback vers `http://localhost:3000`. Après 07B-R1, le retest utilisateur a réussi : retour loopback, sortie de chargement, utilisateur connecté, Cloud non activé automatiquement et bouton de déconnexion disponible. Les contrôles de rafraîchissement après relance, mode hors ligne et déconnexion effective restent manuels.
8. Les tests adversariaux couvrent l’absence de `state` applicatif, les challenges renouvelés, callback dupliqué/tardif/annulé, timeout, URL de découverte/JWKS non liée, rotation de `kid`, indisponibilité de cache, algorithmes `none`/HS256/confus, issuer/audience/`nbf`/`exp`/`sub` invalides, effacement de refresh token, redaction des logs et impossibilité Cloud sans toutes les gardes.

La clôture exige en plus les résultats utilisateurs consignés par le brouillon `docs/project/evidence/phase-07b-supabase-auth/phase-07b-closure-contract.draft.json`. La réussite du parcours OAuth principal ne remplace pas ces contrôles.

## Clôture

Le 2026-07-22, les quatre contrôles manuels de session ont été rapportés PASS, le Development Judge a rendu le verdict technique final ALLOW, puis l'utilisateur a explicitement validé la clôture. Les preuves sont `manual-session-closure-result.md`, `phase-07b-closure-judge-verdict.json` et `phase-07b-user-closure-acceptance.md` dans le dossier d'évidence de cette phase.

## Réversibilité

- Désactiver ou retirer le service d’authentification ramène immédiatement l’état à non authentifié ; le `RewriteOrchestrator` garde alors le mode local.
- Supprimer le refresh token stocké ne touche ni dictionnaire, ni audio, ni transcription, ni historique.
- Aucun changement de données existantes sous `%LOCALAPPDATA%\Fluent` n’est prévu.
