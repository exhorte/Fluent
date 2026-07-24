# Phase 07D — origine backend publique Desktop

## Portée livrée hors ligne

- Ajout de `FluentBackendPublicConfiguration`, qui lit uniquement `FLUENT_BACKEND_URL` dans l'environnement du processus Desktop.
- Seule une origine HTTPS racine est acceptée et normalisée. Une URL HTTP, relative, avec userinfo, query, fragment, chemin ou port non standard est refusée.
- L'origine valide est la seule source de `CloudBackendOptions.BaseAddress` dans la composition WPF.
- Sans origine valide, le bouton d'activation Cloud reste indisponible ; le pipeline garde le fournisseur Local et aucun appel réseau n'est lancé.
- Les gardes existantes restent cumulatives : session authentifiée, origine valide, activation explicite, consentement de session et fournisseur sélectionné.

## Hors portée

Aucun backend n'est déployé, aucune variable externe n'est définie, aucun fournisseur n'est appelé et aucune clé, aucun `.env` ou donnée utilisateur n'est lu. Une origine configurée ne constitue pas une activation Gemini live.

## Vérification

- Tests ciblés : 17/17 PASS.
- Build Release : 0 avertissement, 0 erreur.
- Suite complète Release : 318/318 PASS.
- Tests de garde : 33/33 PASS.
- `git diff --check` passe ; seuls des avertissements LF/CRLF préexistants sont émis.
- Le verdict final du Judge reste à obtenir avant présentation.
