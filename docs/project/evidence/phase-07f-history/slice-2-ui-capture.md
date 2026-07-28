# Phase 07F — Historique local · Slice 2 (UI + capture) — 2026-07-23

Gouvernance : ADR-0007. Tâche `FV-P07F-T019`, tier R1. Décidé et exécuté par le PROJECT_DIRECTOR.

## Livré (slice 2 : capture dans le flux + page Historique WPF)

- **Page Historique** (`src/Fluent.App/Views/HistoryPage.xaml` + `.cs`), UserControl aligné sur le système visuel `Nyx.*` et sur le motif de `DictionaryPage` :
  - Interrupteur **opt-in** (Activer / Désactiver) avec badge d'état (ACTIVÉ / DÉSACTIVÉ) et explication de confidentialité.
  - Liste des dictées (horodatage local, profil, texte), **suppression unitaire** par entrée et **Tout effacer**.
  - Recherche en mémoire, états vides explicites (désactivé / vide / aucun résultat).
  - Code-behind visuel uniquement ; toute la persistance passe par `IDictationHistoryStore` et la politique de capture pure.
- **Navigation** : l'entrée « Historique / À venir » de la barre latérale devient un vrai bouton de navigation ; statut nav = nombre d'entrées quand activé, `OFF` sinon.
- **Capture** (`MainWindow.CompleteDictationAsync`) : après une dictée **réellement délivrée** (collée dans la cible ou copiée dans le presse-papiers ; `InsertTranscript` renvoie désormais ce booléen), `HistoryPage.CaptureAsync` applique la politique opt-in et enregistre le texte final. Les cas **bloqués** (champ mot de passe, cible non vérifiée, cible manquante) **n'enregistrent pas**. La capture ne peut jamais faire échouer la dictée (erreurs avalées côté page).
- **Honnêteté du dashboard** : textes de la Vue d'ensemble corrigés (« Aucun historique n'est enregistré » → « L'historique local est désactivé par défaut (opt-in) » ; badge « Historique · à venir » → « Historique · local opt-in »).

## Confidentialité

- Opt-in **OFF par défaut** ; aucun audio (P-003) ; aucune donnée externe ni télémétrie (P-004) ; aucun secret. Historique local uniquement, supprimable/effaçable.
- Aucune capture sur cible mot de passe ou bloquée.

## Vérification réelle

- `dotnet build Fluent.sln -c Release` : **0 avertissement, 0 erreur** (WPF `Fluent.App` inclus).
- Suite complète : **367/367** (0 échec). Une régression attendue a été corrigée honnêtement : le test `DashboardStatusPresentationTests.Overview_uses_the_pure_mapper_and_exposes_only_status_summaries` vérifiait le libellé « Historique · à venir » ; il vérifie désormais « Historique · local opt-in » (l'intention — Overview sans données inventées — est préservée).

## Reste (vérification humaine, E-011)

Smoke visuel de la page Historique : lancer `Fluent.App.exe`, activer l'historique, dicter, vérifier l'apparition de l'entrée, la suppression et l'effacement, puis le retour à `OFF` sans nouvelle capture. Non automatisable dans cette session.
