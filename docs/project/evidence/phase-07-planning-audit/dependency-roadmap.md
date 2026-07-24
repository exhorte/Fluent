# Feuille de route et dépendances vérifiées — 2026-07-22

## Convention de numérotation

Le dépôt contient déjà **Phase 07C — renommage technique intégral**, dont l'état documentaire est bloqué/stale. Pour ne pas écraser cet identifiant, la séquence demandée est décalée d'un identifiant :

| Demande produit | Identifiant conservé dans Fluent |
| --- | --- |
| 07C — réécriture Gemini authentifiée | **07D** |
| 07D — tableau de bord dynamique | **07E** |
| 07E — historique | **07F** |
| 07F — paramètres | **07G** |
| 07G — UX/résilience | **07H** |

Cette décision ne modifie pas la phase 07C existante et ne lève pas son blocage.

## État et ordre recommandés

```text
07B fermeture manuelle
  └─ 07D Gemini authentifié (backend déployé et autorisé)
       └─ 07E Dashboard dynamique
            ├─ 07F Historique local
            └─ 07G Paramètres
                 └─ 07H UX / résilience
                      └─ 08A contexte de réécriture
                           └─ 08B dictionnaire / glossaire étendu
                                └─ 08C apprentissage
                                     └─ 09A registre fournisseurs
                                          ├─ 09B activation DeepSeek contrôlée
                                          └─ 09C routage explicable
                                               └─ 10 performances
                                                    └─ 11 packaging
                                                         └─ 12 readiness finale
```

## Phases déjà livrées ou ouvertes

| Phase | Statut factuel | Passage requis |
| --- | --- | --- |
| 01 | Implémentée, vérification multi-applications encore historique | Ne bloque pas la clôture 07B ; devra être revalidée avant packaging. |
| 02 à 05B | Clôturées / acceptées selon leurs preuves | Socle local de dictée, réécriture sûre et dictionnaire persistant. |
| 06A | Clôturée sous autorité utilisateur | Profils locaux session-only. |
| 06B | `IMPLEMENTED_AWAITING_USER_REVIEW` | Cloud optionnel construit hors ligne, backend non déployé. |
| 06C | `IMPLEMENTED_AWAITING_USER_REVIEW` | DeepSeek pré-câblé hors ligne, aucune activation live. |
| 07A | Implémentée ; états documentaires contradictoires | Renommage public présent, à réconcilier avec 07C/Git. |
| 07B | `IMPLEMENTED_AWAITING_USER_REVIEW` | Core OAuth PASS ; contrôles session restants avant clôture. |
| 07C | Bloquée/stale | Renommage physique visible mais Git et documentation à réconcilier dans une tâche séparée. |

## Contrats futurs préparés, non actifs

### 07D — Réécriture Gemini authentifiée

Objectif : rendre le chemin Gemini réellement atteignable seulement via un backend Fluent déployé, une URL backend publique configurée côté Desktop, un JWT Supabase validé, l'activation Cloud volontaire et le consentement de session.

Dépendances : clôture 07B, revue 06B, autorisation utilisateur distincte pour déploiement, configuration serveur, secret Gemini et coût potentiel. Aucune clé ni URL ne doit être mise dans le dépôt ou le Desktop. Le smoke prouve le fallback local exact et les chemins 401/403/429/503.

### 07E — Dashboard dynamique

Objectif : remplacer les cartes statiques par des indicateurs calculés localement depuis des sources explicites, avec états chargement/vide/erreur et aucune statistique inventée.

Dépendances : inventaire des métriques, modèle local minimal et définition de rétention. Cette phase ne crée pas d'historique de texte sans le contrat 07F.

### 07F — Historique fonctionnel

Objectif : historique local, désactivé par défaut ou activé par consentement explicite, avec recherche, suppression unitaire/totale, rétention, migrations SQLite et absence d'audio/secret.

Dépendances : décision de produit et de confidentialité sur contenu, durée, taille et suppression ; contrat de migration sans toucher aux données réelles pendant les tests.

### 07G — Paramètres fonctionnels

Objectif : préférences locales explicites (microphone, hotkey, comportement de capsule, profil, langue et politique d'historique) avec valeurs par défaut sûres, validation, migration et retour aux défauts.

Dépendances : contrat de persistance, décisions de sécurité pour hotkey et comportement d'insertion. Les préférences Cloud sensibles restent hors de ce store ; activation et consentement Cloud demeurent session-only jusqu'à décision contraire.

### 07H — UX et résilience

Objectif : états vides, erreurs réessayables, accessibilité, navigation clavier, gestion réseau/offline, récupération après crash et cohérence des notifications.

Dépendances : pages 07E–07G terminées afin de tester de vrais états ; aucune collecte de télémétrie n'est introduite.

## Long terme préparatoire

| Phase | Objet | Garde-fou principal |
| --- | --- | --- |
| 08A | Contexte de réécriture | Consentement explicite, bornage et conservation locale. |
| 08B | Dictionnaire / glossaire étendu | Gestion des conflits, import/export sûr, suppression. |
| 08C | Apprentissage | Opt-in, explicabilité, jamais d'apprentissage silencieux sur dictées. |
| 09A | Registre fournisseurs | Capacités, disponibilité et séparation stricte secrets Desktop/serveur. |
| 09B | Activation DeepSeek live | Tâche séparée : serveur, coût, configuration, déploiement et smoke autorisés par l'utilisateur. |
| 09C | Routage | Choix explicable et jamais de bascule silencieuse entre fournisseurs. |
| 10 | Performance | Mesures versionnées, budget de latence et tests sur matériel réel. |
| 11 | Packaging | Réconciliation Git/renommage, signature, installation et mise à jour sûre. |
| 12 | Readiness finale | Checklist de confidentialité, sauvegarde, recovery, documentation et acceptation utilisateur. |

## Risques de planification

- Aucune phase Cloud live ne peut démarrer sur la seule présence supposée d'un fichier `.env`; le Desktop l'ignore et l'audit ne le lit pas.
- Le déploiement, le secret Gemini, l'activation DeepSeek et toute dépense exigent une autorité utilisateur distincte.
- Une clôture 07B prématurée risquerait de laisser une session persistante, offline ou logout non prouvés.
- Le renommage 07C non réconcilié doit être stabilisé avant packaging ou publication, sans bloquer les audits et tests read-only actuels.
