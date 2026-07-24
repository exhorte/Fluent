# Cause racine — incident OAuth Supabase/Google du 22 juillet 2026

## Symptôme confirmé

Le navigateur système a ouvert Google et accepté le compte de test. Au retour, Supabase a produit `error_code=bad_oauth_state`, puis utilisé la Site URL `http://localhost:3000`. Fluent n’a reçu aucun callback sur son listener `http://127.0.0.1:<port>/callback` et est resté en `SigningIn`.

Le premier smoke est **ÉCHOUÉ**. Aucun nouveau parcours réel n’a été effectué pendant le correctif hors ligne ; le retest utilisateur ultérieur est **PASS** pour le parcours OAuth principal, sans conservation du code d’autorisation.

## Cause

Avant 07B-R1, `SupabaseAuthenticationState.BuildGoogleAuthorizationUri` ajoutait un `state` généré par `PkceAuthorization` à `GET https://<project>.supabase.co/auth/v1/authorize`. Or cette route initie la connexion sociale Supabase vers Google : Supabase crée et valide son propre état pour cet aller-retour. Le `state` Fluent remplaçait donc celui attendu par Supabase.

La conclusion causale est une inférence étayée par trois éléments concordants :

1. le code Fluent ajoutait effectivement le paramètre conflictuel ;
2. Google était atteint via Supabase, puis Supabase rejetait précisément l’état avec `bad_oauth_state` avant le callback Desktop ;
3. la documentation Supabase décrit le flux social via son endpoint Auth, le PKCE séparé par verifier/challenge, `redirect_to` comme redirection applicative et `bad_oauth_state` comme échec de validation de l’état OAuth.

`localhost:3000` n’était pas construit par Fluent. C’était la Site URL Supabase de fallback observée après l’échec. Le pare-feu et le listener loopback n’étaient donc pas la cause initiale.

## Contrat corrigé

L’URL Desktop contient désormais exactement :

- origine HTTPS du projet Supabase configuré ;
- chemin `/auth/v1/authorize` ;
- `provider=google` ;
- `redirect_to=http://127.0.0.1:<port>/callback`, encodé une fois ;
- `flow_type=pkce` ;
- `code_challenge=<SHA-256 Base64URL>` ;
- `code_challenge_method=s256`.

Aucun `state` applicatif n’est généré, envoyé, comparé ou journalisé. Le navigateur n’est jamais dirigé directement vers Google. Le callback Google externe reste `https://braqfrcbnxthxkbwcpzd.supabase.co/auth/v1/callback`; il n’est pas modifié par le Desktop.

## Limite honnête

Un processus Desktop lié uniquement à son port loopback ne peut pas lire une page envoyée à une autre destination telle que `localhost:3000`. Il ne faut pas élargir l’écoute ni intercepter le navigateur. Si aucun callback n’atteint Fluent, le timeout total borné termine maintenant la tentative en échec générique et réactive l’interface.

## Sources officielles vérifiées

- https://supabase.com/docs/guides/auth/social-login/auth-google
- https://supabase.com/docs/guides/auth/sessions/pkce-flow
- https://supabase.com/docs/guides/auth/redirect-urls
- https://supabase.com/docs/guides/auth/debugging/error-codes
