# Résumé d’implémentation — 07B-R1

## Correctif

- `PkceAuthorization` ne contient plus que le verifier aléatoire et son challenge S256 ; tout état applicatif a été retiré.
- `BuildGoogleAuthorizationUri` cible toujours Supabase et produit cinq paramètres seulement : `provider`, `redirect_to`, `flow_type`, `code_challenge` et `code_challenge_method=s256`.
- Le callback loopback ne conserve plus de `state` ou d’erreur fournisseur brute. Il renvoie uniquement un code de succès ou une catégorie `Rejected` / `Invalid`.
- `AuthenticationStatus.Failed` expose le message générique « Connexion Google impossible. Réessayez. ».
- Chaque tentative possède une annulation locale et un timeout total de 120 secondes. Listener et annulation sont libérés avant la notification UI terminale.
- Erreur, exception et timeout quittent `SigningIn`; l’action de connexion redevient disponible. `CancelSignIn` reste sans effet indésirable lorsqu’il est répété ou appelé après la fin.

## Régressions ajoutées

- PKCE S256 aléatoire sans état applicatif.
- Endpoint Supabase et query exacte, sans navigation directe vers Google.
- `redirect_to` loopback exacte après un seul décodage.
- normalisation de `bad_oauth_state` sans détail fournisseur ;
- absence d’échange de code après erreur ;
- fin de tentative, UI non bloquée et listener disposé ;
- timeout borné ;
- annulation idempotente ;
- seconde tentative possible avec nouveau listener, nouveau port et nouveau challenge ;
- scan source interdisant l’émission de `state`, `StateMatches`, `Console` et `ILogger` dans le code Auth.

## Réparations révélées par la suite complète

La première suite complète a exposé deux défauts déterministes hors du code OAuth Desktop :

- un cas de test du dictionnaire avait été renommé vers `FLUENT`, mais son branchement cherchait encore le préfixe `NYX` ; les données parlée/remplacement sont désormais fournies explicitement par chaque cas de théorie ;
- le validateur JWT effectuait trois cycles metadata/JWKS pour une clé inconnue. Sous les amendements Judge FV-P07-T005 et FV-P07-T006, l’appel redondant sur le gestionnaire abandonné a été retiré et les validations qui se chevauchent partagent un seul rafraîchissement forcé. Un gestionnaire frais n’est publié qu’après un chargement non indisponible.

Ces changements sont couverts par des bornes exactes `metadata=2`, `JWKS=2`, une rotation de clé valide, un échec réseau, une annulation en attente et une reprise.

## État de vérification

- OAuth ciblé : 12 / 12.
- JWT Supabase ciblé : 13 / 13.
- Backend complet : 54 / 54.
- Restauration : succès, projets à jour.
- Build Release : succès, 0 avertissement, 0 erreur.
- Suite complète : 301 / 301.
- `git diff --check` : succès ; seuls des avertissements de conversion LF/CRLF sur le vaste renommage déjà présent ont été affichés.
- Scans Auth : aucun tuple `state`, aucun `StateMatches`, aucun `Console` ou `ILogger`.
- Development Judge final : **ALLOW** pour présenter le diff et demander un retest utilisateur, sans clôture ni déclaration de succès réel.

Le premier smoke réel du 22 juillet 2026 a échoué, mais le retest utilisateur du correctif est **PASS** pour le parcours OAuth principal. Les contrôles manuels de rafraîchissement après relance, mode hors ligne et déconnexion effective restent ouverts.
