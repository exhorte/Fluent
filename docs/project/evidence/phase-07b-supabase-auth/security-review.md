# Revue sécurité et confidentialité — Phase 07B

Résultat : PASS pour l’implémentation hors ligne, avec smoke externe explicitement non exécuté.

## Contrôles

- Le Desktop n’embarque aucune WebView et lance seulement le navigateur système.
- `SupabasePublicConfiguration` refuse les schémas non HTTPS, les hôtes non `*.supabase.co`, les chemins, ports, queries, fragments et userinfo non attendus. Sans configuration, le navigateur et le listener ne démarrent pas.
- Le listener est lié à `IPAddress.Loopback` avec port `0`, accepte une unique requête `GET /callback`, borne les en-têtes à 8 KiB, impose un timeout de connexion et est fermé après le callback terminal.
- Le verifier PKCE, le challenge et le state sont aléatoires; la comparaison du state utilise `CryptographicOperations.FixedTimeEquals`.
- `RefreshTokenStore` n’écrit qu’un refresh token dans Windows Credential Manager; aucun champ `AccessToken` n’y existe. Les accès token restent dans `SupabaseAuthenticationState` en mémoire et expirent avant un appel Cloud.
- Les sources Auth ne contiennent ni `Console`, ni `ILogger`, ni lecture de fichier `.env`, ni clé Gemini/DeepSeek. Les scans d’intégration sont verts.
- Le backend exige un issuer/audience fournis par configuration de processus, lie metadata et JWKS à cet issuer, force HTTPS, limite le timeout, rejette `none` et HS256, et ne limite qu’après un `sub` UUID validé.
- Le token statique `FLUENT_BACKEND_TOKEN` et `BackendAuthenticator` sont absents des sources produit; les références Gemini existantes restent strictement backend/server-side.

## Limites connues et sûres

- Aucune URL de backend n’est configurée ou déployée. Même une session authentifiée et consentante ne peut pas déclencher une réécriture Cloud : le contexte conserve Local.
- La console Supabase, Google et la Redirect URL dynamique n’ont pas été modifiées. Le comportement réel reste une checklist utilisateur ultérieure.
