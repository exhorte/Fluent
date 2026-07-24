# Matrice fonctionnelle des surfaces Fluent — 2026-07-22

| Surface | Statut réel | Source de vérité | Ce qui fonctionne | Limites / non câblé |
| --- | --- | --- | --- | --- |
| Vue d'ensemble | Réelle, dynamique pour les statuts locaux | `MainWindow.xaml(.cs)`, `DashboardStatusPresentation` | État de dictée, hotkey, cible, profil, dictionnaire, session et autorisation Cloud proviennent de l'application. | Pas de timeline, statistiques d'historique, activité Cloud réelle ni métriques persistées. « Autorisé localement » n’est pas une preuve de disponibilité backend ou fournisseur. |
| Dictionnaire | Réelle et persistante localement | `DictionaryPage`, `PersistentPersonalDictionary`, `SqlitePersonalDictionaryStore` | Chargement, ajout, modification, suppression, recherche et réutilisation après relance sont implémentés. | La base ne contient que `personal_dictionary`; aucune donnée audio, dictée, compte ou historique n'est stockée. Les données utilisateur réelles n'ont pas été inspectées pendant cet audit. |
| Profils locaux | Réelle | `ProfilesPage`, `ProfileSelection`, `RewriteProfiles` | Français professionnel par défaut, Développeur et sélection session-only. Français simplifié est explicitement indisponible. | Pas de persistance des profils ni de moteur de simplification réel. |
| Compte / authentification | Réelle pour le cœur OAuth ; clôture en attente | `SupabaseAuthenticationState`, `ProfilesPage` | Connexion Google via navigateur système/PKCE, callback loopback, affichage du compte et action de déconnexion. Le smoke principal est rapporté PASS. | Restauration après relance, hors-ligne et déconnexion persistante restent à vérifier manuellement. Pas de gestion de compte étendue. |
| Réécriture Cloud | Préparée, indisponible en pratique | `CloudRewriteSettings`, `BackendCloudRewriteClient`, `Fluent.Backend` | Gardes authentification + activation + consentement, sélection de fournisseur de session et fallback local exact sont implémentés. | `BuildCloudBackendOptions()` ne fournit pas de `BaseAddress`; le backend est non déployé. Aucun texte n'est envoyé au Cloud dans cet état. |
| Historique | Non implémenté, honnêtement signalé | Navigation `MainWindow.xaml` | Affichage `À venir`; l'Overview précise qu'aucun historique n'est enregistré. | Pas de page, modèle, repository, base SQLite, rétention, suppression ou recherche d'historique. |
| Paramètres | Non implémenté, honnêtement signalé | Navigation `MainWindow.xaml` | Affichage `À venir`. | Pas de page, ViewModel, store de préférences, migration ou contrôle utilisateur. |
| Capsule / dictée | Réelle | `RecordingCapsuleWindow`, `MainWindow` | Capsule non activante, hotkey, capture mémoire, transcription locale et insertion sécurisée existent. | Vérification multi-application de la phase 01 reste historiquement ouverte ; pas de télémétrie ni historique. |

## Persistance SQLite réellement présente

`FluentDataPath` cible `%LOCALAPPDATA%\Fluent\fluent.db`. Le seul schéma inspecté est la table versionnée `personal_dictionary` avec forme parlée, remplacement et horodatage. L'audit ne lit aucune base réelle. Il n'existe pas de schéma pour l'historique, les préférences, les comptes, les jetons, l'audio ou le contenu des dictées.

## Règle de vérité UI

Une surface ne peut pas afficher une métrique, un état ou une action sans source de données réelle. Les phases 07E à 07G devront introduire source, erreur, vide, chargement, suppression et tests avant de rendre une navigation active.
