# Revue sécurité et confidentialité — Phase 06C

## PASS — frontières de secrets

Les sources Desktop (`Fluent.App/Cloud`, `Fluent.Cloud`, `Fluent.Rewrite`) ne contiennent ni `DEEPSEEK_API_KEY`, ni `DEEPSEEK_MODEL`, ni `DEEPSEEK_BASE_URL`, ni l’endpoint fournisseur. Le transport DeepSeek est exclusivement backend. Aucun chargeur de fichier ou de `.env` n’est ajouté.

## PASS — egress borné et redaction

Le backend accepte seulement l’origine HTTPS exacte `https://api.deepseek.com`, sans chemin, query, fragment, information utilisateur ni port non standard. La route est construite statiquement vers `/chat/completions`; aucun texte ou paramètre client ne peut choisir un endpoint. L’autorisation est un en-tête HTTP, jamais une query string, et le `HttpClient` DeepSeek utilise `RemoveAllLoggers()`.

## PASS — garde et repli

La sélection DeepSeek est explicite et session-only. Elle ne change ni l’authentification, ni l’activation Cloud, ni le consentement. Le backend refuse le fournisseur sans configuration complète; l’orchestrateur conserve le résultat local exact sur indisponibilité, échec, annulation ou sortie invalide. Aucun basculement automatique Gemini/DeepSeek n’est implémenté.

## Limite connue

Le chemin live reste non vérifié volontairement : aucune clé, aucun compte, aucune requête fournisseur, aucun backend déployé ni coût n’ont été engagés. La checklist serveur reste une étape séparée, nécessitant une autorisation explicite.
