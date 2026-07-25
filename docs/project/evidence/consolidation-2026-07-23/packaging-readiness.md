# Packaging readiness (prep phase 11) — 2026-07-23

Gouvernance : ADR-0007. Audit automatable du PROJECT_DIRECTOR (piste consolidation → v1). Aucune décision de distribution imposée : les choix produit sont laissés à l'utilisateur.

## Vérifié (réel)

- `dotnet publish src/Fluent.App -c Release` (framework-dependent) : **0 avertissement, 0 erreur**.
- Sortie : `src/Fluent.App/bin/Release/net10.0-windows/publish/` — **`Fluent.exe`** (nom d'assembly `Fluent`) + `Fluent.dll`, natif Whisper (`Whisper.net.dll`, assets `ggml`), fournisseur SQLite (`SQLitePCLRaw.provider.e_sqlite3.dll`). **107 fichiers, ~93 Mo.**
- Ce paquet est **framework-dependent** : il exige le **runtime .NET 10 Desktop** installé sur la machine cible.
- `Directory.Build.props` : `TreatWarningsAsErrors=true`, `Deterministic=true`, analyse `latest` — build strict, sain pour un artefact reproductible.

## Écarts / décisions nécessaires (produit — utilisateur)

| Sujet | État actuel | Décision / action |
| --- | --- | --- |
| **Version** | Aucune définie ⇒ défaut `1.0.0` | Choisir un schéma (p.ex. `VersionPrefix` 0.1.0 pré-v1) et le fixer dans `Directory.Build.props`. |
| **Modèle de distribution** | Framework-dependent (93 Mo, exige runtime .NET 10) | Choisir : framework-dependent (léger, prérequis runtime) **ou** self-contained (~150–200 Mo, aucun prérequis) — voire single-file. |
| **Icône d'application** | Aucune (`ApplicationIcon` absent) | Fournir une icône. Un dossier `docs/logo/` (non suivi) existe déjà côté utilisateur — candidat pour l'icône/branding. |
| **Modèle Whisper (~80 Mo)** | Téléchargé au premier usage | Décider : conserver le téléchargement à la demande **ou** empaqueter le modèle (taille de l'installeur). |
| **Assets natifs Whisper hors-Windows** | `ggml-metal.metal` (macOS) et variantes AVX/NoAvx présents | Optimisation possible : restreindre les runtimes au RID Windows pour réduire la taille. |
| **Installeur** | Aucun (MSIX / Inno Setup / WiX) | Choisir un format d'installeur pour une distribution v1. |
| **Signature de code** | Aucune | **R3 — utilisateur** : certificat de signature requis (achat/engagement). |

## Ce qui est prêt

- La solution compile en Release strict (0/0) et **publie proprement** en framework-dependent, natifs inclus.
- Nom d'exécutable confirmé : **`Fluent.exe`**.
- Aucun secret, `.env`, clé ni base locale dans la sortie de publication.

## Chemin recommandé vers un artefact v1

1. Fixer une version (`Directory.Build.props`) — après ton choix de schéma.
2. Choisir framework-dependent vs self-contained + single-file — puis ajouter la config de publication.
3. Ajouter l'icône depuis `docs/logo/`.
4. Restreindre les runtimes Whisper au RID Windows (réduction de taille).
5. Choisir et configurer un installeur.
6. Signature de code (**ton autorisation / certificat**), puis smoke d'installation propre.

## Limites honnêtes

Aucune étape ci-dessus n'a été imposée : versioning, modèle de distribution et installeur sont des décisions produit. La signature et la publication exigent une autorité utilisateur distincte (R3). L'audit est en lecture/publication à blanc uniquement ; aucune config de packaging n'a été modifiée.
