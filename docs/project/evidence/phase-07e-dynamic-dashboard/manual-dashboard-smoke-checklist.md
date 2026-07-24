# Checklist manuelle — Phase 07E Dashboard

Statut : PASS rapporté explicitement par l’utilisateur le 2026-07-22 ; aucun détail, capture ou comportement supplémentaire n’est inféré.

1. Lancer Fluent sans configurer de backend ni modifier d’option Cloud.
2. Dans **Vue d’ensemble**, vérifier que les badges Profil, Dictionnaire, Compte et Cloud sont lisibles et se replient sans recouvrir la zone « Historique · à venir ».
3. Vérifier que le dictionnaire passe de `Chargement` à son état réel : vide/local ou secours selon l’état déjà constaté par Fluent, sans afficher aucune entrée.
4. Vérifier que le badge Compte montre seulement un état de session ; il ne doit jamais afficher e-mail, jeton ou identifiant.
5. Sans session ou sans origine backend valide, vérifier que le badge Cloud reste `Non configuré` ou `Local (connexion requise)` et qu’aucune activation ni boîte de consentement ne se déclenche.
6. Après une connexion déjà configurée, vérifier que le badge Compte change d’état sans révéler l’identité du compte ; vérifier que `Autorisé localement` ne prétend jamais que le service est disponible.
7. Vérifier qu’aucun texte de dictée, contenu de dictionnaire, historique, compteur d’activité ou nouvelle donnée persistée n’apparaît dans l’Overview.

Résultat utilisateur : **PASS** (rapporté par l’utilisateur dans cette conversation).
