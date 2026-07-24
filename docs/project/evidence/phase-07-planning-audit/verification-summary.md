# Vérification reproductible — 2026-07-22

Exécutée après l'audit en lecture seule, sans exécuter l'application, l'OAuth, le backend, un fournisseur Cloud ou une opération externe.

| Commande | Résultat |
| --- | --- |
| `dotnet restore Fluent.sln --disable-parallel` | PASS — projets déjà à jour. |
| `dotnet build Fluent.sln -c Release --no-restore -m:1 --disable-build-servers` | PASS — 0 avertissement, 0 erreur. |
| `dotnet test Fluent.sln -c Release --no-build -m:1 --disable-build-servers` | PASS — 301 réussis, 0 échec, 0 ignoré. |
| `pwsh -NoProfile -File .claude/hooks/tests/Invoke-AllHookTests.ps1` | PASS — 33/33. |
| `git diff --check` | PASS — avertissements LF/CRLF seulement, pas de défaut bloquant. |

Répartition de la suite : Core 11, Windows 6, Audio 6, Speech 9, Rewrite 164, Persistence 19, Integration 32, Backend 54.

Ces résultats vérifient la compilation et les scénarios déterministes. Ils ne remplacent pas les contrôles utilisateurs restants de la phase 07B ni un smoke Cloud déployé.
