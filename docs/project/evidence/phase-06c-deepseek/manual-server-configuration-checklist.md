# Checklist manuelle — DeepSeek côté serveur uniquement

Statut : **non exécutée**. Cette checklist ne peut être suivie qu’après une autorisation explicite de l’utilisateur pour la configuration serveur, le déploiement éventuel et un appel fournisseur potentiellement facturé.

1. Confirmer sur la documentation officielle le compte, la tarification et le modèle voulu. Pour ce client, DeepSeek documente l’API compatible OpenAI, `https://api.deepseek.com`, `POST /chat/completions` et les modèles `deepseek-v4-pro` / `deepseek-v4-flash` : [DeepSeek API Docs](https://api-docs.deepseek.com/).
2. Déployer ou exécuter le backend Fluent dans un environnement isolé du Desktop. Ne placez aucune clé dans le dépôt, dans `.env`, dans le client WPF, dans les arguments de ligne de commande affichables ou dans une URL.
3. Configurer uniquement dans le processus backend les trois variables non vides : `DEEPSEEK_MODEL`, `DEEPSEEK_API_KEY` et `DEEPSEEK_BASE_URL=https://api.deepseek.com`.
4. Vérifier que les logs HTTP du backend restent désactivés pour le client DeepSeek et que les journaux ne contiennent ni clé, ni Bearer token, ni texte dicté.
5. Vérifier d’abord le comportement sans l’une des trois valeurs : la requête DeepSeek doit être indisponible et Fluent doit conserver le résultat local.
6. Après le smoke Supabase/Google autorisé séparément, connecter un compte test dans Fluent, activer Cloud, accorder le consentement de session, choisir DeepSeek V4 Pro, puis dicter un texte de test non sensible.
7. Contrôler que l’UI indique DeepSeek seulement pour cette session, que l’audio reste local et que toute erreur fournisseur retombe sur le texte local exact.
8. Désactiver Cloud ou se déconnecter pour confirmer qu’aucune nouvelle dictée ne part au backend. Conserver les métriques au minimum (fournisseur, durée, repli/cause), jamais le contenu.

Ne lancez pas ces étapes automatiquement et ne copiez jamais ici une clé, un token, un e-mail ou du texte dicté réel.
