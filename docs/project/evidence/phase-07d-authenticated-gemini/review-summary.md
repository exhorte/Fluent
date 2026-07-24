# Revue 07D — configuration et garde UI

## Revue de portée

Les seuls changements produit concernent la configuration publique de l'origine backend, son câblage dans `CloudBackendOptions`, la présentation indisponible et les tests associés. Aucun fichier Backend, fournisseur, OAuth, persistance ou secret n'est modifié.

## Revue sécurité et confidentialité

- L'entrée est l'environnement du processus uniquement ; aucun chargeur `.env` ni lecture de fichier n'est présent.
- La valeur ne peut pas contenir d'userinfo, query, fragment, chemin ou port non standard ; seule l'origine HTTPS normalisée est conservée.
- La valeur n'est pas une URL de fournisseur ni un secret ; les endpoints et clés de fournisseurs restent hors Desktop.
- Les tests n'ouvrent aucune socket et ne démarrent aucun flux OAuth ou fournisseur.

## Revue comportementale

- Sans origine valide, `BaseAddress` reste nulle, le bouton Cloud est désactivé et le pipeline déjà testé reste Local.
- Une origine valide ne relâche pas la session authentifiée, le consentement session-only, le choix explicite de fournisseur ni le fallback exact.
- Le résultat live reste délibérément non prouvé, car aucun backend ni fournisseur n'est autorisé dans cette tranche.
