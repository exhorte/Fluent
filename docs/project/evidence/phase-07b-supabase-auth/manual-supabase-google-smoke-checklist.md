# Checklist manuelle Supabase / Google — à exécuter uniquement avec autorisation utilisateur

Statut : PARCOURS OAUTH PRINCIPAL RÉUSSI — checklist manuelle restante. La configuration Google et la Redirect URL ont été confirmées par l’utilisateur le 22 juillet 2026. Le premier parcours réel a échoué avec `bad_oauth_state`, puis le retest après correctif 07B-R1 a réussi. Cette checklist ne crée, ne modifie ni ne révèle aucune valeur.

## Incident du 22 juillet 2026

- [x] Le navigateur système s’est ouvert et Google a accepté le compte de test.
- [x] Supabase a ensuite renvoyé `bad_oauth_state` vers la Site URL de fallback `http://localhost:3000`.
- [x] Fluent est resté bloqué en `SigningIn` parce que son listener loopback n’a reçu aucun callback.
- [x] Le code a été corrigé pour ne plus envoyer de `state` applicatif, normaliser les erreurs, libérer le listener et borner l’attente.
- [x] Le retest utilisateur a confirmé le retour sur `http://127.0.0.1:<port>/callback`, la sortie de l’état de chargement et la connexion effective.

## Retest réussi du 22 juillet 2026

- [x] Google a confirmé le compte de test.
- [x] Le navigateur est revenu vers le callback loopback `127.0.0.1` de Fluent ; aucun code d’autorisation n’est consigné ici.
- [x] Fluent a quitté « Connexion en cours » et a affiché l’utilisateur connecté.
- [x] Le Cloud ne s’est pas activé automatiquement.
- [x] Le bouton « Se déconnecter » est disponible.
- [x] Les contrôles restants de rafraîchissement après relance, mode hors ligne, déconnexion effective et réinitialisation du consentement Cloud de session ont été rapportés PASS par l'utilisateur le 2026-07-22.

## Préconditions externes à confirmer par l’utilisateur

- [x] Le projet Supabase Fluent est disponible à `https://braqfrcbnxthxkbwcpzd.supabase.co`.
- [x] Une configuration publique valide a été présente dans le processus Desktop pour le smoke OAuth principal ; sa valeur n'est ni copiée ni conservée dans le dépôt ou dans cette checklist.
- [ ] Les clés de signature JWT Supabase sont asymétriques (RS256 ou ES256), avec JWKS exposé.
- [x] Google est activé dans Supabase, avec un client OAuth Web, un compte de test et uniquement le callback Supabase `https://braqfrcbnxthxkbwcpzd.supabase.co/auth/v1/callback`.
- [x] Les Redirect URLs Supabase autorisent le modèle dynamique `http://127.0.0.1:*/callback`.
- [ ] Le Desktop reçoit `FLUENT_SUPABASE_URL` et `FLUENT_SUPABASE_PUBLISHABLE_KEY` par l’environnement de processus, jamais par `.env` distribué.
- [ ] Le backend déployé reçoit `FLUENT_BACKEND_SUPABASE_ISSUER=https://<project-ref>.supabase.co/auth/v1` et `FLUENT_BACKEND_SUPABASE_AUDIENCE=authenticated`; aucune clé JWT symétrique n’est utilisée.

## Préparation locale du smoke

Dans un terminal utilisateur temporaire — jamais dans un fichier du dépôt — définir uniquement les deux valeurs publiques, puis lancer l’application :

```powershell
$env:FLUENT_SUPABASE_URL = 'https://braqfrcbnxthxkbwcpzd.supabase.co'
$env:FLUENT_SUPABASE_PUBLISHABLE_KEY = '<clé publishable publique Supabase>'
dotnet run --project src/Fluent.App/Fluent.App.csproj
```

Ne pas coller de Client Secret Google, de service-role key, de token ou de fichier `.env` dans le terminal, l’application ou le dépôt. Fermer ce terminal à la fin du smoke afin d’effacer les variables de son processus.

## Parcours Desktop

- [ ] Démarrer `Fluent.exe` : sans configuration, constater « Authentification non configurée » et l’absence de navigateur.
- [x] Avec la configuration publique autorisée, cliquer « Se connecter avec Google » : constater l’ouverture du navigateur système, sans WebView.
- [x] Achever Google avec le compte de test : constater le retour loopback vers Fluent, le nom/e-mail et le statut connecté, sans token visible.
- [ ] Annuler une tentative : constater que le navigateur peut rester ouvert mais que Fluent revient à « Connexion annulée » sans session.
- [x] Relancer Fluent : rafraîchissement de session constaté sans token affiché, puis statut hors ligne avec le mode Local constaté par l'utilisateur.
- [x] Cliquer « Se déconnecter » : retour au mode local et aucune session restaurée après relance, constatés par l'utilisateur.
- [x] Après une relance, activation Cloud et consentement Cloud de nouveau désactivés, constatés par l'utilisateur.

Résultat détaillé désensibilisé : `manual-session-closure-result.md`.
- [ ] Vérifier dans Supabase Auth que le compte de test apparaît comme utilisateur authentifié, sans exporter ni copier de jeton.

## Parcours Cloud après déploiement backend autorisé

- [ ] Vérifier que la connexion seule ne transmet aucune dictée.
- [ ] Activer Cloud puis accepter explicitement le consentement : vérifier que le message nomme Gemini et que l’audio reste local.
- [ ] Simuler backend indisponible, JWT invalide, rôle refusé et quota dépassé : vérifier respectivement fallback local et états 503, 401, 403, 429 sans texte/token dans les logs.
- [ ] Vérifier qu’aucune surface ne propose DeepSeek et qu’aucun appel DeepSeek n’est observé.
