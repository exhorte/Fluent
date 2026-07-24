# Phase 07B — implémentation Supabase native

## Livré

- Authentification Google Supabase par navigateur système avec PKCE S256, state à comparaison constante et callback TCP unique lié à `127.0.0.1` sur un port éphémère.
- Échange et rafraîchissement Supabase derrière une interface testable. Les valeurs Desktop sont limitées à l’URL Supabase publique et à sa clé publishable dans l’environnement du processus; `.env` n’est ni lu ni modifié.
- Access token limité à la mémoire; refresh token protégé dans Windows Credential Manager. Démarrage avec refresh single-flight, suppression après rejet définitif et distinction hors ligne / session expirée.
- Carte WPF de connexion : connexion Google, annulation, déconnexion, nom/e-mail, états configuration, chargement, hors ligne et expiration. Le consentement Cloud reste indépendant et session-only.
- Backend migré du token partagé vers la validation JWT Supabase asymétrique OIDC/JWKS, avec issuer/audience de confiance obligatoires, RS256/ES256 seulement, actualisation de clé à `kid` inconnu et limitation par `sub` validé.
- Le backend est toujours non déployé. L’interface montre donc honnêtement que le service Cloud est indisponible et le `RewriteOrchestrator` reste local sans adresse backend.

## Garanties maintenues

- Local reste le défaut : aucun texte ne part au Cloud sans session valide, activation, consentement, backend disponible et Gemini activé.
- DeepSeek V4 Pro reste désactivé, non sélectionnable et non appelé.
- Audio, transcription, dictionnaire, insertion, hotkey, capsule et données `%LOCALAPPDATA%\Fluent` ne sont pas modifiés.
- Aucun secret OAuth, service-role, JWT secret, clé Gemini/DeepSeek, code, verifier, token ou contenu de dictée n’est affiché ou journalisé.

## Vérification

- `dotnet restore`: PASS.
- Build Release: 0 avertissement, 0 erreur.
- Suite Release complète: 271 réussis, 0 échec, 0 ignoré.
- Hooks de gouvernance: 33/33 réussis.
- Development Judge final : ALLOW pour présentation en `IMPLEMENTED_AWAITING_USER_REVIEW` uniquement ; la phase n'est pas clôturée.
- Aucun appel Supabase, Google, Gemini ou DeepSeek réel, aucune configuration de compte, aucun déploiement et aucune publication Git n’ont été exécutés.
