# Revue adversariale — Phase 06C

| Scénario | Défense | Preuve |
| --- | --- | --- |
| Clé, modèle ou endpoint copiés dans le Desktop | Les scans de source interdisent les trois paramètres et l’origine fournisseur hors backend | `DeepSeekSecurityScanTests` |
| SSRF via `DEEPSEEK_BASE_URL` | Seule l’origine HTTPS exacte sans chemin, query, fragment, userinfo ou port est admise; route statique | `DeepSeekServerProviderTests` |
| Backend configuré partiellement | Le provider est indisponible et le faux transport ne reçoit aucun appel | `DeepSeekServerProviderTests` |
| Secret dans URI ou corps | Le test inspecte l’URI, l’en-tête et le corps; l’autorisation est seulement un en-tête | `DeepSeekServerProviderTests` |
| Sélection qui active Cloud ou le consentement | `CloudRewriteSettings` sépare la sélection de l’activation et du consentement | `CloudRewriteSettingsTests` |
| DeepSeek appelé sans les gardes existantes | Les contextes non authentifiés, non activés ou non consentis restent locaux | `CloudRewriteOrchestratorTests` |
| Panne, annulation, sortie vide ou non sûre | L’orchestrateur retourne le texte local exact; l’annulation demandée est propagée | tests Rewrite et Backend |
| Basculement silencieux Gemini/DeepSeek | Chaque requête transporte le fournisseur explicitement; aucun fallback inter-fournisseur n’existe | `CloudRewriteOrchestratorTests` |

Conclusion : aucun défaut bloquant trouvé dans le périmètre hors ligne. Le risque résiduel concerne uniquement une future opération live autorisée séparément.
