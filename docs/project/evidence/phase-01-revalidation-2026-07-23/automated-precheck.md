# Phase 01 revalidation — pré-vérifications automatisables — 2026-07-23

Exécutées par le PROJECT_DIRECTOR (opérations locales/automatisables, sans nouvelle autorisation). Elles ne remplacent pas les scénarios manuels multi-applications (E-011) mais confirment la couche déterministe de la sécurité d'insertion.

## Résultats

- **Build Release** : 0 avertissement / 0 erreur.
- **Exécutable produit** : `src/Fluent.App/bin/Release/net10.0-windows/Fluent.exe` présent (nom d'assembly `Fluent`). Correction : l'ancienne référence `Fluent.App.exe` était erronée.
- **Logique d'insertion sûre** (`Fluent.Core.Tests`, dont `TextInsertionPolicyTests`) : **24/24**. Couvre les décisions d'insertion : cible inchangée → coller ; cible changée → repli presse-papiers ; champ mot de passe → bloqué ; cible non vérifiée/manquante → bloqué (P-005/P-009).
- **Frontière Win32** (`Fluent.Windows.Tests`) : **6/6**. Couvre la taille native de la structure `INPUT` (40 octets x64) et un échec `SendInput` renvoyant un résultat explicite (pas d'exception fatale ; le texte reste au presse-papiers), ainsi que la détection de cible active.
- Rappel suite complète : **399/399** ; gardes de gouvernance **45/45**.

## Ce qui reste strictement manuel (E-011)

Le comportement réel du focus Windows, les fournisseurs UI Automation par application (Bloc-notes, navigateur, VS Code, Windows Terminal, Word), le vrai champ mot de passe, le changement de cible en direct et le microphone matériel. Voir `multi-app-revalidation-checklist.md` pour les 8 scénarios à renseigner PASS/FAIL.
