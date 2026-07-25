namespace Fluent.App.Localization;

/// <summary>English/French catalog for the interface. Default language: English.</summary>
internal static class LocalizedStrings
{
    public static IReadOnlyDictionary<string, (string En, string Fr)> Catalog { get; } =
        new Dictionary<string, (string En, string Fr)>
        {
            // Navigation
            ["nav.section"] = ("LOCAL SPACE", "ESPACE LOCAL"),
            ["nav.home"] = ("Home", "Accueil"),
            ["nav.history"] = ("History", "Historique"),
            ["nav.dictionary"] = ("Dictionary", "Dictionnaire"),
            ["nav.subscription"] = ("Subscription", "Abonnement"),
            ["nav.settings"] = ("Settings", "Paramètres"),
            ["nav.upgrade"] = ("Upgrade plan", "Passer à Pro"),
            ["chip.localUser"] = ("Local user", "Utilisateur local"),

            // Home (overview)
            ["home.title"] = ("Home", "Accueil"),
            ["home.subtitle"] = (
                "Your local dictation, its status and its protections at a glance.",
                "Votre dictée locale, son état et ses protections en un coup d'œil."),
            ["home.card.state"] = ("STATE", "ÉTAT"),
            ["home.state.desc"] = ("Current dictation", "Dictée courante"),
            ["home.card.shortcut"] = ("SHORTCUT", "RACCOURCI"),
            ["home.shortcut.value"] = ("Ctrl + Space", "Ctrl + Espace"),
            ["home.card.engine"] = ("ENGINE", "MOTEUR"),
            ["home.engine.desc"] = ("Local Whisper · CPU", "Whisper local · CPU"),
            ["home.card.privacy"] = ("PRIVACY", "CONFIDENTIALITÉ"),
            ["home.privacy.value"] = ("In memory", "En mémoire"),
            ["home.privacy.desc"] = ("No audio file", "Aucun fichier audio"),
            ["home.lastActivity"] = ("Last activity", "Dernière activité"),
            ["home.lastResult.default"] = (
                "Place the caret in a text field, then use Ctrl+Space to start and stop dictation.",
                "Placez le curseur dans un champ texte, puis utilisez Ctrl+Espace pour démarrer et arrêter la dictée."),
            ["home.insertion.note"] = (
                "Insertion only if the initial target stays valid; otherwise, explicit copy to the clipboard. Local history is off by default (opt-in).",
                "Insertion seulement si la cible initiale reste valide ; sinon, copie explicite dans le presse-papiers. L'historique local est désactivé par défaut (opt-in)."),
            ["home.target.title"] = ("Secure target", "Cible sécurisée"),
            ["home.target.none"] = ("No locked target", "Aucune cible verrouillée"),
            ["home.target.note"] = (
                "Password field blocked · target re-checked before insertion",
                "Champ secret bloqué · cible revérifiée avant insertion"),
            ["home.foundations.title"] = ("Product foundations", "Fondations du produit"),
            ["home.foundations.subtitle"] = (
                "The dashboard is ready for upcoming features without showing fake data.",
                "Le dashboard est prêt à accueillir les prochaines fonctions sans afficher de données fictives."),

            // History
            ["history.title"] = ("History", "Historique"),
            ["history.subtitle"] = (
                "Local, optional history of your dictations. No audio, nothing sent outside.",
                "Historique local et optionnel de vos dictées. Aucun audio, aucun envoi externe."),
            ["history.recording.title"] = ("History recording", "Enregistrement de l'historique"),
            ["history.recording.desc"] = (
                "Off by default. When enabled, only the final text of your dictations is stored, on this device only. No audio is ever kept and nothing is sent outside. The 500 most recent entries are kept; you can delete one entry or clear everything at any time.",
                "Désactivé par défaut. Lorsqu'il est activé, seul le texte final de vos dictées est enregistré, uniquement sur cet appareil. Aucun audio n'est jamais conservé et rien n'est envoyé à l'extérieur. Les 500 entrées les plus récentes sont conservées ; vous pouvez supprimer une entrée ou tout effacer à tout moment."),
            ["history.stored.title"] = ("Stored dictations", "Dictées enregistrées"),
            ["history.search"] = ("SEARCH", "RECHERCHER"),
            ["history.clearAll"] = ("Clear all", "Tout effacer"),

            // Dictionary
            ["dictionary.title"] = ("Dictionary", "Dictionnaire"),
            ["dictionary.subtitle"] = (
                "Manage the personal corrections applied to transcription.",
                "Gérez les corrections personnelles appliquées à la transcription."),
            ["dictionary.add.title"] = ("Add a correction", "Ajouter une correction"),
            ["dictionary.add.desc"] = (
                "The recognized form will be replaced by the given text during rewriting.",
                "La forme reconnue sera remplacée par le texte indiqué lors de la réécriture."),
            ["dictionary.spokenForm"] = ("RECOGNIZED FORM", "FORME RECONNUE"),
            ["dictionary.replacement"] = ("REPLACEMENT", "REMPLACEMENT"),
            ["dictionary.addUpdate"] = ("Add / update", "Ajouter / mettre à jour"),
            ["dictionary.local.title"] = ("Local corrections", "Corrections locales"),
            ["dictionary.search"] = ("SEARCH", "RECHERCHER"),
            ["dictionary.exchange.title"] = ("Import / Export", "Importer / Exporter"),
            ["dictionary.exchange.desc"] = (
                "Back up your corrections to a local file, or import one. Imported content is validated and overwrites nothing without your choice. No data is sent outside.",
                "Sauvegardez vos corrections dans un fichier local, ou importez-en un. Le contenu importé est validé et n'écrase rien sans votre choix. Aucune donnée n'est envoyée à l'extérieur."),
            ["dictionary.export"] = ("Export…", "Exporter…"),
            ["dictionary.import"] = ("Import…", "Importer…"),
            ["dictionary.overwriteConflicts"] = (
                "Overwrite existing entries on conflict",
                "Écraser les entrées existantes en cas de conflit"),
            ["common.edit"] = ("Edit", "Modifier"),
            ["common.delete"] = ("Delete", "Supprimer"),
            ["common.cancel"] = ("Cancel", "Annuler"),

            // Profiles
            ["profiles.title"] = ("Profiles", "Profils"),
            ["profiles.subtitle"] = (
                "Choose the local transformation applied after the dictionary, before insertion.",
                "Choisissez la transformation locale appliquée après le dictionnaire, avant l'insertion."),
            ["profiles.sessionBadge"] = ("SESSION · NOT SAVED", "SESSION · NON ENREGISTRÉ"),
            ["profiles.info"] = (
                "The selection applies to this session only and is never stored. The profile used by a dictation is the one selected when recording starts; a change during recording applies to the next dictation. No profile invents content.",
                "La sélection vaut pour cette session uniquement et n'est jamais enregistrée. Le profil utilisé par une dictée est celui choisi au démarrage de l'enregistrement ; un changement pendant l'enregistrement s'applique à la dictée suivante. Aucun profil n'invente de contenu."),
            ["profiles.auth.title"] = ("Sign-in", "Connexion"),
            ["profiles.auth.signInGoogle"] = ("Sign in with Google", "Se connecter avec Google"),
            ["profiles.cloud.title"] = ("Cloud rewriting", "Réécriture Cloud"),

            // Subscription
            ["subscription.title"] = ("Subscription", "Abonnement"),
            ["subscription.subtitle"] = (
                "Your plan and options. Fluent stays 100% local and free; Pro will add advanced options.",
                "Votre plan et vos options. Fluent reste 100 % local et gratuit ; Pro ajoutera des options avancées."),
            ["subscription.currentPlan"] = ("CURRENT PLAN", "PLAN ACTUEL"),
            ["subscription.plan.local"] = ("Local · Free", "Local · Gratuit"),
            ["subscription.plan.desc"] = (
                "Local dictation, rewriting, dictionary and history, no account required.",
                "Dictée, réécriture, dictionnaire et historique locaux, sans compte requis."),
            ["subscription.upgrade"] = ("Upgrade to Pro", "Passer à Pro"),
            ["subscription.account"] = ("Account", "Compte"),
            ["subscription.account.local"] = ("Not signed in · local use", "Non connecté · utilisation locale"),
            ["subscription.account.connected"] = ("Signed in · ", "Connecté · "),
            ["subscription.account.connectedNoEmail"] = ("Signed in", "Compte connecté"),
            ["subscription.upgrade.status"] = (
                "Pro is not available yet in this local version. No payment data is requested.",
                "L'abonnement Pro n'est pas encore disponible dans cette version locale. Aucune donnée de paiement n'est demandée."),
            ["subscription.pro.title"] = ("Pro · coming soon", "Pro · à venir"),
            ["subscription.pro.item1"] = (
                "• Extended transcription models and advanced rewriting profiles.",
                "• Modèles de transcription étendus et profils de réécriture avancés."),
            ["subscription.pro.item2"] = (
                "• Extended dictionary/glossary and optional encrypted sync.",
                "• Dictionnaire/glossaire étendu et synchronisation optionnelle chiffrée."),
            ["subscription.pro.item3"] = (
                "• Enriched history and advanced exports.",
                "• Historique enrichi et exports avancés."),
            ["subscription.pro.note"] = (
                "The local mode will always stay available and free. No payment is requested in this version.",
                "Le mode local restera toujours disponible et gratuit. Aucun paiement n'est demandé dans cette version."),

            // Settings
            ["settings.title"] = ("Settings", "Paramètres"),
            ["settings.subtitle"] = (
                "Local, versioned and reversible preferences. No secret or Cloud consent is stored here.",
                "Préférences locales, versionnées et réversibles. Aucun secret ni consentement Cloud n'est enregistré ici."),
            ["settings.defaultProfile"] = ("Default profile", "Profil par défaut"),
            ["settings.defaultProfile.current"] = ("Current: ", "Actuel : "),
            ["settings.defaultProfile.desc"] = (
                "The profile applied to rewriting at startup. This choice is saved locally and restored on the next launch.",
                "Le profil appliqué à la réécriture au démarrage. Ce choix est enregistré localement et restauré au prochain lancement."),
            ["settings.storage.title"] = ("Local storage and privacy", "Stockage local et confidentialité"),
            ["settings.storage.desc1"] = (
                "All data stays on this device, in the local application folder (LocalAppData\\Fluent): dictionary, history and preferences, each in its own SQLite database.",
                "Toutes les données restent sur cet appareil, dans le dossier local de l'application (LocalAppData\\Fluent) : dictionnaire, historique et préférences, chacun dans sa propre base SQLite."),
            ["settings.storage.desc2"] = (
                "No audio file is ever stored. No telemetry. Authentication and Cloud options are session-only and are not persisted.",
                "Aucun fichier audio n'est jamais enregistré. Aucune télémétrie. L'authentification et les options Cloud restent limitées à la session et ne sont pas persistées."),
            ["settings.session.title"] = ("Session and engine", "Session et moteur"),
            ["settings.session.desc"] = (
                "Current session information (previously shown in the header).",
                "Informations de la session en cours (auparavant affichées dans l'en-tête)."),
            ["settings.session.profile"] = ("PROFILE", "PROFIL"),
            ["settings.session.mode"] = ("MODE", "MODE"),
            ["settings.session.engine"] = ("ENGINE", "MOTEUR"),
            ["settings.language.title"] = ("Language", "Langage"),
            ["settings.language.desc"] = (
                "Interface language (default: English).",
                "Langue de l'interface (par défaut : anglais)."),
            ["settings.history.manage"] = ("Manage history", "Gérer l'historique"),
            ["settings.history.prefix"] = ("History · ", "Historique · "),
        };
}
