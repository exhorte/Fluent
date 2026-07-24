# Phase 07G — Paramètres locaux · Slice 2 (page Paramètres) — 2026-07-23

Gouvernance : ADR-0007. Tâche `FV-P07G-T020`, tier R1. Décidé et exécuté par le PROJECT_DIRECTOR.

## Livré

- **Page Paramètres** (`src/Fluent.App/Views/SettingsPage.xaml` + `.cs`), UserControl aligné sur le système visuel `Nyx.*`. Code-behind visuel uniquement ; la persistance est possédée par l'hôte via événements.
  - **Profil par défaut** : profil actuel affiché + sélecteur des profils disponibles ; définir un profil par défaut le persiste (via le store 07G) et l'applique immédiatement. Synchronisé avec la page Profils (les deux restent cohérentes).
  - **Historique** : résumé de l'état (Activé/Désactivé + nombre) et bouton « Gérer l'historique » qui navigue vers la page Historique. Pas de toggle dupliqué (une seule source de vérité : la page Historique).
  - **Stockage local et confidentialité** : carte factuelle statique (données dans `LocalAppData\Fluent`, bases SQLite séparées dictionnaire/historique/paramètres, aucun audio, aucune télémétrie, Cloud session-only).
- **Navigation** : l'entrée « Paramètres / À venir » de la barre latérale devient un vrai bouton de navigation.
- **Synchronisation** (`MainWindow`) : changement de profil (Profils ou Paramètres) → mise à jour présentation + persistance + rafraîchissement des deux pages ; changements d'état de l'historique → mise à jour du résumé Paramètres.

## Confidentialité

Aucun secret ni consentement Cloud persisté ou affiché. Le consentement/paramètres Cloud restent session-only (P-002, 07B/07D).

## Vérification réelle

- `dotnet build Fluent.sln -c Release` : **0 avertissement, 0 erreur** (WPF `Fluent.App` inclus).
- Suite complète : **376/376** (0 échec). Aucune régression (page UI, non unit-testée ; logique persistée couverte par les tests du store en slice 1).

## Reste (vérification humaine, E-011)

Smoke visuel de la page Paramètres : ouvrir Paramètres, vérifier le profil par défaut affiché, changer le profil par défaut (cohérence avec Profils + persistance au redémarrage), vérifier le résumé Historique et le bouton « Gérer l'historique », vérifier la carte stockage local. Non automatisable dans cette session.
