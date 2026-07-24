# Phase 07G — Paramètres locaux · Slice 1 (store + profil préféré) — 2026-07-23

Gouvernance : ADR-0007. Tâche `FV-P07G-T020`, tier R1. Décidé et exécuté par le PROJECT_DIRECTOR.

## Livré

- **Domaine Core** (`src/Fluent.Core/Settings/`) : `AppPreferences` (profil préféré, défaut vide), `AppSettingsLimits`, `IAppSettingsStore`.
- **Persistance** (`src/Fluent.Persistence/Settings/SqliteAppSettingsStore.cs`) : store SQLite dans une **base dédiée `fluent-settings.db`**, séparée des bases dictionnaire et historique. Schéma versionné (v1, table mono-ligne `app_settings`), requêtes paramétrées, garde de taille, rejet sans mutation sur version non supportée / données corrompues. **Aucun secret ni consentement Cloud** n'y est stocké.
- **Chemin** : `FluentDataPath.GetDefaultSettingsDatabasePath()` / `GetSettingsDatabasePath(root)`.
- **Wiring** : au démarrage, `MainWindow.RestorePreferredProfileAsync` restaure le profil préféré (valeur absente/inconnue → défaut canonique conservé) ; au changement de profil, `PersistPreferredProfile` l'enregistre (best-effort, n'interrompt jamais la dictée). Le badge du profil actif passe de « ACTIF · SESSION » à « ACTIF · ENREGISTRÉ » (honnête : la sélection persiste désormais).

## Décision produit (réversible, R1)

Le profil de réécriture était réinitialisé au défaut à chaque lancement ; il est désormais mémorisé localement. Réversible : l'utilisateur peut changer de profil à tout moment, et la valeur est purement locale (aucun secret). Le consentement/paramètres Cloud restent **session-only** (P-002, 07B/07D) — non persistés.

## Vérification réelle

- `dotnet build Fluent.sln -c Release` : **0 avertissement, 0 erreur**.
- Suite complète : **376/376** (0 échec) — 20 + 6 + 6 + 9 + 164 + 77 + 40 + 54. Persistance 31 → 40 (+9 tests paramètres). Aucune régression.
- Tests store : défaut vide + idempotence ; profil persisté puis rechargé ; effacement (null) ; blanc → null ; injection SQL neutralisée (table/table-count intacts) ; valeur surdimensionnée rejetée ; version de schéma 2 rejetée sans mutation (octets inchangés, pas de sidecar) ; annulation sans I/O ; base dédiée distincte des autres.

## Reste (slice 2)

Page **Paramètres** WPF (remplace l'état « À venir ») surfaçant les préférences locales (profil par défaut, raccourci vers l'opt-in historique, etc.). Smoke visuel = vérification humaine E-011.
