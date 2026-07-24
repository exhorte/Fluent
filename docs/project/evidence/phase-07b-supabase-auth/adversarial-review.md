# Revue adversariale — Phase 07B

Résultat : PASS pour les scénarios vérifiables localement.

| Scénario | Défense / preuve |
| --- | --- |
| State CSRF erroné ou rejoué | State aléatoire, comparaison constante, aucun échange si mismatch; tests PKCE et callback. |
| Callback malformé, chemin erroné, dupliqué ou tardif | Listener loopback unique, `/callback` exact, en-têtes 8 KiB, fermeture terminale; le callback est à usage unique. |
| Annulation / timeout | `CancellationTokenSource` 120 s pour le flux et 5 s par connexion; annulation n’échange aucun code. |
| Token HS256 / `none` / algorithme inattendu | Allow-list RS256/ES256 avant récupération JWKS; test HS256 vert. |
| Signature ou issuer/audience/exp/nbf/sub invalides | `TokenValidationParameters` et tests RS256 valides, issuer, audience, expiration, nbf et sub absent. |
| Rôle non autorisé | JWT signé mais rôle non `authenticated` retourne 403; test vert. |
| Rotation ou indisponibilité JWKS | `RequestRefresh` puis une seule retentative à `kid` manquant; cache/metadata invalide échoue fermé en 503, sans accepter le token. |
| Partition de quota manipulable | Le rate limiter prend seulement un UUID `sub` déjà validé; test de partition entre deux utilisateurs. |
| Authentification seule | Le garde Cloud exige aussi activation, consentement, backend disponible et Gemini; le backend non déployé maintient Local. |
| Fuite de token / secret | Access token non persistant, refresh token Credential Manager, scans source et aucun logger Auth. |
| Activation DeepSeek | Tests et implémentations existants gardent DeepSeek désactivé et non résolu. |

Résidu : la rotation réelle de clés, la Redirect URL dynamique et le parcours Google doivent être vérifiés manuellement seulement après autorisation et configuration de compte par l’utilisateur.
