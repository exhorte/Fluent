# ADR-0006: Authentification native Fluent avec Supabase

Statut: Accepted — smoke OAuth principal réussi ; contrôles de session 07B restants

Date: 2026-07-21

## Contexte

La Phase 06B dispose déjà d’un chemin Cloud optionnel, mais celui-ci reste inatteignable sans authentification. Fluent est une application WPF native et ne doit ni embarquer une WebView, ni contenir un secret OAuth, un service-role Supabase, une clé JWT symétrique ou une clé de fournisseur.

## Décision

- Fluent utilise Supabase Auth et Google via le navigateur système, avec Authorization Code + PKCE S256. Chaque tentative crée en mémoire un verifier et un challenge aléatoires. Fluent n’ajoute aucun paramètre `state` à l’endpoint social `/auth/v1/authorize` : Supabase possède et valide l’état OAuth de son aller-retour avec Google.
- Un `TcpListener` lié uniquement à `127.0.0.1` et au port système `0` reçoit une seule requête `GET /callback` bornée. La redirection produite est `http://127.0.0.1:<port>/callback`; aucun navigateur intégré, port fixe, `localhost`, adresse LAN ou IPv6 n’est utilisé.
- Le Desktop n’accepte que `FLUENT_SUPABASE_URL` (origine HTTPS `*.supabase.co`) et `FLUENT_SUPABASE_PUBLISHABLE_KEY` depuis l’environnement du processus. Il ne charge pas `.env`.
- L’access token reste en mémoire. Le refresh token est le seul secret de session persistant et est stocké dans Windows Credential Manager sous l’utilisateur Windows courant. Une erreur définitive le supprime; une indisponibilité réseau le conserve mais ne crée aucune session Cloud valide.
- Le backend remplace le token partagé de Phase 06B par un validateur Supabase asymétrique. Il requiert `FLUENT_BACKEND_SUPABASE_ISSUER` et `FLUENT_BACKEND_SUPABASE_AUDIENCE=authenticated`, construit la découverte OIDC et le JWKS à partir de cet issuer, vérifie l’égalité des métadonnées, n’autorise que RS256/ES256, valide signature/issuer/audience/exp/nbf/sub/rôle et actualise une fois le JWKS à la rotation d’un `kid`.
- Le quota backend est partitionné seulement par `sub` validé. L’ordre de `POST /v1/rewrite` est fixe : verifier indisponible 503, JWT absent ou invalide 401, rôle refusé 403, quota 429, requête invalide 400, fournisseur indisponible 503, succès 200.
- Le Cloud reste conditionné par la session authentifiée, l’activation, le consentement session-only, le backend disponible et Gemini activé. DeepSeek reste désactivé et non sélectionnable.

## Conséquences

Cette décision évite tout secret OAuth dans le client et permet au backend de vérifier les sessions sans connaître de secret JWT partagé. Le stockage de refresh token reste lié au compte Windows. Le premier smoke réel du 22 juillet 2026 a échoué avec `bad_oauth_state` parce que Fluent remplaçait l’état interne Supabase par un `state` applicatif. Le correctif 07B-R1 retire ce conflit tout en conservant PKCE et le callback loopback exact. Le retest utilisateur a ensuite confirmé le parcours principal : callback loopback atteint, UI connectée, Cloud inactif et action de déconnexion disponible. La restauration après relance, le comportement hors ligne, la déconnexion effective et le reset du consentement de session restent requis avant clôture de 07B.

## Réversibilité

Supprimer la session et revenir à l’état non authentifié laisse le pipeline local intact. Le chemin Cloud revient alors immédiatement au fallback local exact. Aucune donnée du dictionnaire historique n’est déplacée ou migrée.
