# Matrice source → résumé — Phase 07E

Chaque valeur introduite par 07E est éphémère et calculée au moment de la mise à jour de l’Overview. Aucune valeur n’est écrite dans SQLite, fichier, registre ou préférence.

| Résumé Overview | Source locale existante | États visibles | Exclusions explicites |
| --- | --- | --- | --- |
| Profil | `_profileSelection.Current.DisplayName` | Profil local courant | Aucun profil persisté, aucune identité de compte. |
| Dictionnaire | `_personalDictionary.Count` et `DictionaryPage.StorageMode` | Chargement ; vide/local ; nombre d’entrées locales ; vide/secours ; nombre d’entrées de secours | Ni forme parlée, ni remplacement, ni autre contenu d’entrée. |
| Compte | `_authenticationState.Status` | Non configuré ; déconnecté ; connexion en cours ; connecté ; hors ligne ; expiré ; annulé ; échec | Ni nom, ni e-mail, ni sujet, ni jeton, ni message brut du fournisseur. |
| Cloud | Origine backend validée en mémoire, état de session, activation, consentement et fournisseur de session | Non configuré ; Local/connexion requise ; désactivé ; consentement requis ; autorisé localement | Ni reachability backend, ni santé du service, ni disponibilité Gemini/DeepSeek, ni succès d’appel. |

## Règle de priorité Cloud

1. Sans origine backend valide : `Cloud · Non configuré`.
2. Avec origine mais sans session authentifiée : `Cloud · Local (connexion requise)`.
3. Avec session, mais Cloud désactivé : `Cloud · Désactivé`.
4. Avec activation sans consentement : `Cloud · Consentement requis`.
5. Avec toutes les gardes locales : `Cloud · Autorisé localement` et le fournisseur sélectionné, sans aucune promesse de disponibilité réseau ou de résultat fournisseur.
