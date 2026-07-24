# Phase 06C — fournisseur DeepSeek V4 Pro optionnel

## Livré hors ligne

- DeepSeek est désormais un fournisseur Cloud disponible dans le domaine, mais Gemini reste le choix de session initial.
- La page Profils permet de sélectionner explicitement Gemini ou DeepSeek V4 Pro pour la session. Cette sélection ne persiste pas, n’active pas Cloud et ne donne jamais le consentement.
- Le Desktop ne connaît ni clé, ni modèle, ni endpoint DeepSeek. Il appelle uniquement le backend existant après toutes les gardes d’authentification, d’activation Cloud et de consentement.
- Le backend ne tente un appel DeepSeek que si `DEEPSEEK_MODEL`, `DEEPSEEK_API_KEY` et l’origine exacte `https://api.deepseek.com` sont présents dans sa configuration de processus. Toute valeur absente ou invalide rend le fournisseur indisponible sans appel sortant.
- Le transport utilise le format OpenAI-compatible `POST /chat/completions`, `Authorization: Bearer` et `stream: false`; le secret n’est jamais placé dans l’URI, le corps, les journaux ou la réponse Desktop.
- Toute erreur, annulation, sortie vide ou sortie rejetée conserve le résultat local exact. Aucun basculement automatique entre Gemini et DeepSeek n’existe.

## Vérification

- Restore : PASS.
- Build Release : 0 avertissement, 0 erreur.
- Suite Release complète : 294 réussis, 0 échec, 0 ignoré.
- Development Judge final : ALLOW pour présentation en `IMPLEMENTED_AWAITING_USER_REVIEW` uniquement ; la phase n’est pas clôturée et ne peut pas être activée en live sans autorisation distincte.
- La documentation officielle DeepSeek confirme la compatibilité OpenAI, l’origine, la route et les identifiants V4 utilisés par la configuration serveur : [DeepSeek API Docs](https://api-docs.deepseek.com/).
- Aucun `.env` n’a été lu ou modifié ; aucun secret, compte, appel DeepSeek réel, déploiement, opération de facturation ou publication Git n’a été effectué.
