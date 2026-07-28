# Phase 07H — UX & résilience · Slice 1 (accessibilité navigation/pages) — 2026-07-23

Gouvernance : ADR-0007. Tâche `FV-P07H-T021`, tier R1. Décidé et exécuté par le PROJECT_DIRECTOR.

## Livré

- **Noms d'accessibilité** (`AutomationProperties.Name` + `HelpText`) sur les 5 entrées de navigation (Vue d'ensemble, Historique, Dictionnaire, Profils, Paramètres) et sur les 5 pages (`Page …`). Les lecteurs d'écran annoncent désormais correctement la navigation et la page active.
- **Ordre clavier** : `TabIndex` 0–4 sur la navigation. Les entrées sont des `Button` (déjà focalisables et activables au clavier par Entrée/Espace) ; l'ordre de tabulation est désormais explicite.
- Modifications strictement additives dans `src/Fluent.App/MainWindow.xaml` ; aucune logique de dictée touchée.

## Vérification réelle

- `dotnet build Fluent.sln -c Release` : **0 avertissement, 0 erreur**.
- Suite complète : **387/387** (0 échec). IntegrationTests 77 → 88 (+11).
- **Test markup d'accessibilité** (`tests/Fluent.IntegrationTests/AccessibilityMarkupTests.cs`) : assertions déterministes que les 5 entrées de navigation et les 5 pages portent un `AutomationProperties.Name`, et que la navigation déclare un ordre de tabulation `TabIndex` 0–4.

## Reste (07H, slices suivants)

- Présentation d'erreurs non techniques + pistes de récupération (remplacer les messages bruts d'exception).
- Polish offline (états et messages).
- Smoke visuel/lecteur d'écran de l'accessibilité (vérification humaine E-011) : facultatif — les libellés sont verrouillés par test.
