# Phase 07B-R1 — Réparation OAuth Supabase/Google

Statut : CLOSED_WITH_PHASE_07B — clôturé le 2026-07-22 avec la Phase 07B sous autorité utilisateur

## Incident observé

Le smoke réel du 22 juillet 2026 a atteint Google puis échoué côté Supabase avec `bad_oauth_state`. Supabase a redirigé le navigateur vers la Site URL de fallback `http://localhost:3000`, tandis que Fluent est resté en état `SigningIn` en attendant son callback loopback.

## Cause racine

Fluent ajoute actuellement un paramètre `state` applicatif à `GET /auth/v1/authorize`. Dans le flux de connexion sociale Supabase, ce paramètre est réservé et géré par Supabase pendant l’aller-retour avec Google. La valeur aléatoire Fluent remplace donc l’état OAuth interne attendu par Supabase ; Google la renvoie, Supabase ne peut pas la valider et répond `bad_oauth_state` avant le callback Desktop.

L’URL `redirect_to` loopback, l’endpoint Supabase et le passage effectif par Google sont cohérents avec le smoke. Le fallback `localhost:3000` est la conséquence documentée d’un échec avant une redirection applicative utilisable, pas la cause initiale.

## Correctif borné

- Retirer la génération, l’envoi et la validation du `state` applicatif dans ce flux social Supabase.
- Conserver PKCE S256 : verifier aléatoire en mémoire, challenge SHA-256 Base64URL, `flow_type=pkce`, `code_challenge` et `code_challenge_method=s256`.
- Construire uniquement l’URL `https://<project>.supabase.co/auth/v1/authorize` avec `provider=google` et le `redirect_to` loopback exact, encodé une seule fois.
- Normaliser tout callback d’erreur reçu vers un état `Failed` désensibilisé, sans conserver ni afficher `error_description`, code, verifier, token ou state brut.
- Finaliser chaque tentative avant de notifier l’UI : listener arrêté/disposé, source d’annulation libérée, état hors `SigningIn`, bouton de connexion réutilisable et annulation idempotente.
- Conserver un timeout total borné. Si Supabase ou le navigateur redirige vers une destination que Fluent n’écoute pas, le Desktop ne peut pas observer cette page ; le timeout doit alors terminer la tentative en `Failed` et permettre un nouvel essai.

## Critères d’acceptation

1. Aucun paramètre `state` n’est envoyé à `/auth/v1/authorize` et aucun state applicatif n’est généré ou comparé.
2. L’URL authorize contient exactement le projet Supabase, `/auth/v1/authorize`, `provider=google`, le `redirect_to` loopback exact, `flow_type=pkce`, un challenge correct et `code_challenge_method=s256`, sans double encodage.
3. Un callback `bad_oauth_state` reçu est normalisé sans contenu brut ; aucune tentative d’échange de code n’a lieu.
4. Erreur, exception, annulation et timeout ferment/disposent le listener et quittent toujours `SigningIn`.
5. Après échec, l’UI présente une erreur générique, réactive « Se connecter » et permet une nouvelle tentative ; « Annuler » reste idempotent.
6. Aucun token, code, verifier, state ou message fournisseur brut n’est journalisé.
7. Les tests ciblés, build Release, suite complète, diff et revue finale passent. Le retest utilisateur confirme le retour loopback, l’état connecté, l’absence d’activation Cloud automatique et le bouton de déconnexion. Les scénarios de session restants de la checklist ne sont pas encore validés.

## Sources officielles

- https://supabase.com/docs/guides/auth/social-login/auth-google
- https://supabase.com/docs/guides/auth/sessions/pkce-flow
- https://supabase.com/docs/guides/auth/redirect-urls
- https://supabase.com/docs/guides/auth/debugging/error-codes
