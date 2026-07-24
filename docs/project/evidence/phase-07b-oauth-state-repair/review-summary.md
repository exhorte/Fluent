# Revue finale locale — 07B-R1/R2/R3

## Architecture OAuth / Supabase

PASS.

- Le navigateur cible exclusivement l’origine Supabase configurée et `/auth/v1/authorize`, jamais Google directement.
- La query contient cinq paramètres : `provider=google`, `redirect_to` loopback exacte, `flow_type=pkce`, challenge S256 et méthode `s256`.
- Aucun `state` Fluent n’est produit ou attendu. PKCE et le port loopback éphémère conservent la corrélation de la tentative.
- Le callback Google externe Supabase n’est pas modifié.

## Cycle de vie UI et listener

PASS.

- Erreur, rejet, exception, annulation et timeout atteignent un état terminal non authentifié.
- Le listener `127.0.0.1` est disposé et la source de tentative est retirée avant la notification UI terminale.
- `SigningIn` ne subsiste pas ; le bouton de connexion est réutilisable et l’annulation répétée est sans effet indésirable.
- Une destination navigateur étrangère au listener n’est pas interceptée ; le timeout total de 120 secondes termine ce cas honnêtement.

## Sécurité et confidentialité

PASS.

- Les champs d’erreur OAuth sont réduits à `Rejected` ou `Invalid`; aucun détail fournisseur brut n’est conservé dans le callback ou affiché.
- Aucun token, code, verifier ou état brut n’est journalisé. Le code Auth ne référence ni `Console` ni `ILogger`.
- Aucun secret, `.env`, compte, configuration Supabase/Google, donnée locale, déploiement ou Git n’a été lu ou modifié.
- Le validateur JWT conserve l’origine metadata/JWKS épinglée, RS256/ES256, issuer, audience, durée de vie, sujet et rôle. Les rafraîchissements concurrents de clé inconnue sont single-flight et bornés à deux cycles au total.

## Tests et diff

PASS.

- OAuth ciblé 12 / 12 ; JWT ciblé 13 / 13 ; Backend 54 / 54 ; suite complète 301 / 301.
- Build Release : 0 avertissement, 0 erreur.
- `git diff --check` : succès.
- Le dépôt contient déjà un vaste renommage technique non commité ; le diff de cette réparation est donc présenté par fichiers et comportements plutôt que comme un commit Git autonome.

## Limite de validation

Le premier smoke utilisateur du 22 juillet 2026 a échoué. Le retest corrigé est désormais **PASS** pour le parcours OAuth principal ; les tests hors ligne restent complémentaires aux contrôles manuels de session encore ouverts.
