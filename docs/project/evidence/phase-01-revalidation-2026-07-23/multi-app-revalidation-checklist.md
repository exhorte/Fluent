# Phase 01 — Revalidation multi-applications (comportement actuel) — 2026-07-23

Statut : `PREPARED_PENDING_MANUAL_EXECUTION`. Gouvernance : ADR-0007.

Cette revalidation remplace la checklist « texte fixe » de 2026-07-12 : l'application fait désormais de la **vraie dictée locale** (Whisper) suivie d'une **insertion sécurisée**. Les invariants de sécurité d'insertion (les vrais critères de la Phase 01) doivent être revérifiés sur plusieurs applications avant packaging.

## Binaire

Build Release :

```powershell
dotnet build Fluent.sln -c Release --no-restore
```

Exécutable : `src/Fluent.App/bin/Release/net10.0-windows/Fluent.exe` (pas la DLL). Note : le nom d'assembly est `Fluent`, donc l'exécutable est **`Fluent.exe`** — l'ancienne référence `Fluent.App.exe` était erronée.

## Invariants vérifiés (principes produit)

- P-005 : jamais d'insertion automatique dans un champ mot de passe.
- P-006 : jamais d'envoi automatique d'Entrée.
- P-007 : jamais d'exécution d'une commande dictée.
- P-008 : la capsule flottante ne vole pas le focus.
- P-009 : si la cible initiale disparaît/change, pas d'insertion dans la nouvelle cible ; copie explicite dans le presse-papiers avec indication.

## Procédure commune (par application)

1. Lancer `Fluent.App.exe`.
2. Placer le curseur dans le champ texte de l'application cible.
3. `Ctrl+Espace` → la capsule apparaît, **le focus reste dans la cible** (P-008), l'onde s'anime.
4. Dicter une courte phrase en français.
5. `Ctrl+Espace` → transcription locale puis insertion.
6. Vérifier : texte inséré correct, **aucune touche Entrée envoyée** (P-006), aucune commande exécutée (P-007).

## Scénarios

| # | Application / cible | Attendu | Statut |
| --- | --- | --- | --- |
| 1 | Bloc-notes | Texte dicté inséré ; pas d'Entrée. | PENDING |
| 2 | Champ texte navigateur (normal) | Texte inséré ; pas d'Entrée. | PENDING |
| 3 | **Champ mot de passe navigateur** | **Rien collé ni copié** ; état « bloqué » affiché (P-005). | PENDING |
| 4 | Éditeur VS Code | Texte inséré ; pas d'Entrée. | PENDING |
| 5 | **Invite Windows Terminal** | Texte **collé uniquement** ; pas d'Entrée, **commande non exécutée** (P-006/P-007). | PENDING |
| 6 | **Cible changée entre les deux Ctrl+Espace** | Copie presse-papiers, **pas d'insertion dans la nouvelle cible** ; indication explicite (P-009). | PENDING |
| 7 | Champ Word / traitement de texte | Texte inséré ; pas d'Entrée. | PENDING |
| 8 | Cible fermée/disparue avant insertion | Repli presse-papiers ; aucune insertion (P-009). | PENDING |

## Pré-vérifications automatisables (exécutées le 2026-07-23)

Voir `automated-precheck.md` : la logique d'insertion et les bornes Win32 sont couvertes par des tests unitaires déterministes ; seul le comportement réel du focus et des fournisseurs UI Automation par application reste manuel.

## Notes

- Ces scénarios restent manuels (E-011) : ils dépendent du focus Windows réel, des fournisseurs UI Automation spécifiques à chaque application et du microphone matériel.
- Renseigner le résultat (PASS/FAIL + observation) par scénario ; aucun contenu sensible ni mot de passe réel ne doit être consigné.
